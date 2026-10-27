using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Restaurant.Infrastructure;

namespace Restaurant.API;

// Development bridge only. A claim is never automatically retried after an uncertain delivery;
// Meta's send endpoint is not assumed to be idempotent.
public sealed class MetaTestDispatchWorker(IServiceScopeFactory scopes, IHttpClientFactory clients,
    IConfiguration config, IHostEnvironment environment, ILogger<MetaTestDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue<bool>("Meta:TestMode")) return;
        var url = config["Automation:MetaBridgeUrl"];
        var token = config["Automation:MetaBridgeToken"];
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var destination) ||
            (destination.Scheme != Uri.UriSchemeHttps && !environment.IsDevelopment() && !environment.IsEnvironment("Testing")))
            throw new InvalidOperationException("Meta bridge URL must be an absolute HTTPS URL outside local testing.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchBatch(destination, token, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Meta test dispatch scan failed ({Type}).", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task DispatchBatch(Uri destination, string token, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        var pending = await db.MetaWebhookEvents.AsNoTracking().Where(x => x.State == "Pending")
            .OrderBy(x => x.ReceivedAt).Take(10).ToListAsync(cancellationToken);
        foreach (var item in pending)
        {
            var claimed = await db.MetaWebhookEvents.Where(x => x.Id == item.Id && x.State == "Pending")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Claimed"), cancellationToken);
            if (claimed != 1) continue;
            var outcome = "Unknown";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, destination) {
                    Content = JsonContent.Create(new { messageId = item.MessageId, phoneNumberId = item.PhoneNumberId,
                        senderWaId = item.SenderWaId, text = item.Text })
                };
                request.Headers.TryAddWithoutValidation("X-Restaurant-Meta-Bridge-Token", token);
                using var client = clients.CreateClient("meta-test-bridge");
                using var response = await client.SendAsync(request, cancellationToken);
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);
                var result = document.RootElement;
                outcome = response.IsSuccessStatusCode && result.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True &&
                    result.TryGetProperty("messageId", out var id) && id.GetString() == item.MessageId ? "MetaAccepted" : "Failed";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogWarning("Meta test delivery outcome is uncertain for event {EventId} ({Type}).", item.Id, ex.GetType().Name); }
            await db.MetaWebhookEvents.Where(x => x.Id == item.Id && x.State == "Claimed")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, outcome), cancellationToken);
        }
    }
}
