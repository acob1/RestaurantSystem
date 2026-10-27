using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Restaurant.Infrastructure;

namespace Restaurant.API;

public static class MetaWebhookEndpoints
{
    private const int MaxBodyBytes = 256 * 1024;

    public static void MapMetaWebhook(this RouteGroupBuilder api)
    {
        api.MapGet("/webhooks/whatsapp", Verify);
        api.MapPost("/webhooks/whatsapp", Receive);
    }

    private static IResult Verify(HttpRequest request, IConfiguration config)
    {
        if (!config.GetValue<bool>("Meta:TestMode")) return Results.NotFound();
        var expected = config["Meta:VerifyToken"];
        var supplied = request.Query["hub.verify_token"].ToString();
        var challenge = request.Query["hub.challenge"].ToString();
        if (request.Query["hub.mode"] != "subscribe" ||
            !FixedTimeEquals(expected, supplied) ||
            challenge.Length is < 1 or > 256 || challenge.Any(char.IsControl))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        return Results.Text(challenge, "text/plain", Encoding.UTF8, StatusCodes.Status200OK);
    }

    private static async Task<IResult> Receive(HttpRequest request, IConfiguration config,
        BusinessDbContext db, CancellationToken cancellationToken)
    {
        if (!config.GetValue<bool>("Meta:TestMode")) return Results.NotFound();
        var appSecret = config["Meta:AppSecret"];
        var expectedPhone = config["Meta:PhoneNumberId"];
        var allowedSender = config["Meta:AllowedTestRecipient"];
        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(expectedPhone) ||
            string.IsNullOrWhiteSpace(allowedSender)) return Results.StatusCode(503);
        if (request.ContentLength > MaxBodyBytes) return Results.StatusCode(413);

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + count > MaxBodyBytes) return Results.StatusCode(413);
            buffer.Write(chunk, 0, count);
        }
        var raw = buffer.ToArray();
        if (!ValidSignature(appSecret, request.Headers["X-Hub-Signature-256"].ToString(), raw))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        List<IncomingText> messages;
        try { messages = ParseTexts(raw, expectedPhone, allowedSender); }
        catch (JsonException) { return Results.BadRequest(new { error = "invalid_meta_payload" }); }
        catch (InvalidOperationException) { return Results.BadRequest(new { error = "invalid_meta_payload" }); }
        if (messages.Count > 10) return Results.StatusCode(413);

        var accepted = 0;
        foreach (var item in messages)
        {
            var entry = new MetaWebhookEvent(item.Id, item.PhoneNumberId, item.From, item.Text);
            // The unique message ID is the durable deduplication boundary, including concurrent deliveries.
            accepted += await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "MetaWebhookEvents" ("Id", "MessageId", "PhoneNumberId", "SenderWaId", "Text", "ReceivedAt", "State")
                VALUES ({entry.Id}, {entry.MessageId}, {entry.PhoneNumberId}, {entry.SenderWaId}, {entry.Text}, {entry.ReceivedAt}, {entry.State})
                ON CONFLICT ("MessageId") DO NOTHING
                """, cancellationToken);
        }
        return Results.Json(new { accepted, duplicates = messages.Count - accepted }, statusCode: 200);
    }

    private static bool FixedTimeEquals(string? expected, string supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)) return false;
        var first = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var second = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(first, second);
    }

    private static bool ValidSignature(string secret, string header, byte[] body)
    {
        if (!header.StartsWith("sha256=", StringComparison.Ordinal) || header.Length != 71) return false;
        byte[] supplied;
        try { supplied = Convert.FromHexString(header.AsSpan(7)); }
        catch (FormatException) { return false; }
        var calculated = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return CryptographicOperations.FixedTimeEquals(calculated, supplied);
    }

    private static List<IncomingText> ParseTexts(byte[] raw, string expectedPhone, string allowedSender)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("object", out var kind) || kind.GetString() != "whatsapp_business_account" ||
            !root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            throw new JsonException();
        var result = new List<IncomingText>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                throw new JsonException();
            foreach (var change in changes.EnumerateArray())
            {
                if (change.ValueKind != JsonValueKind.Object || !change.TryGetProperty("field", out var field) || field.GetString() != "messages" ||
                    !change.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object)
                    continue;
                if (!value.TryGetProperty("messages", out var messages)) continue; // Delivery status notification.
                if (messages.ValueKind != JsonValueKind.Array || !value.TryGetProperty("metadata", out var metadata) ||
                    metadata.ValueKind != JsonValueKind.Object || !metadata.TryGetProperty("phone_number_id", out var phone))
                    throw new JsonException();
                var phoneId = phone.GetString();
                if (phoneId != expectedPhone) throw new JsonException();
                foreach (var message in messages.EnumerateArray())
                {
                    if (message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("type", out var type)) throw new JsonException();
                    if (type.GetString() != "text") continue; // Media and unsupported events have no AI side effect.
                    if (!message.TryGetProperty("id", out var idElement) || !message.TryGetProperty("from", out var fromElement) ||
                        !message.TryGetProperty("text", out var textObj) || textObj.ValueKind != JsonValueKind.Object ||
                        !textObj.TryGetProperty("body", out var bodyElement)) throw new JsonException();
                    var id = idElement.GetString(); var from = fromElement.GetString(); var text = bodyElement.GetString();
                    if (id is null || !id.StartsWith("wamid.", StringComparison.Ordinal) || id.Length > 200 ||
                        from != allowedSender || text is null || text.Length is < 1 or > 1000 || string.IsNullOrWhiteSpace(text))
                        throw new JsonException();
                    result.Add(new IncomingText(id, phoneId!, from, text));
                }
            }
        }
        return result;
    }

    private sealed record IncomingText(string Id, string PhoneNumberId, string From, string Text);
}
