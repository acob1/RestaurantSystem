using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Infrastructure;

namespace Restaurant.IntegrationTests;

public sealed class MetaWebhookTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Path = "/api/v1/webhooks/whatsapp";
    private const string Secret = "integration-test-app-secret";

    [Fact]
    public async Task ValidChallengeIsEchoedVerbatim()
    {
        var response = await fixture.Factory.CreateClient().GetAsync(Path + "?hub.mode=subscribe&hub.verify_token=integration-test-verify-token&hub.challenge=123456");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("123456", await response.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=123")]
    [InlineData("?hub.mode=invalid&hub.verify_token=integration-test-verify-token&hub.challenge=123")]
    [InlineData("?hub.mode=subscribe&hub.verify_token=integration-test-verify-token")]
    public async Task InvalidChallengeIsRejected(string query) =>
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Factory.CreateClient().GetAsync(Path + query)).StatusCode);

    [Fact]
    public async Task SignedMessageIsSavedOnceAcrossRepeatedDeliveries()
    {
        var id = "wamid." + Guid.NewGuid().ToString("N");
        var body = Message(id, "Hello");
        var first = await Send(body, Sign(body));
        var second = await Send(body, Sign(body));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("\"accepted\":1", await first.Content.ReadAsStringAsync());
        Assert.Contains("\"duplicates\":1", await second.Content.ReadAsStringAsync());
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        var saved = await db.MetaWebhookEvents.SingleAsync(x => x.MessageId == id);
        Assert.Equal("Hello", saved.Text);
        Assert.Equal("Pending", saved.State);
    }

    [Fact]
    public async Task ChangedBodyOrMissingSignatureIsRejectedBeforeDatabaseWrite()
    {
        var id = "wamid." + Guid.NewGuid().ToString("N");
        var body = Message(id, "Hello");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(body, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(body.Replace("Hello", "Menu"), Sign(body))).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<BusinessDbContext>().MetaWebhookEvents.AnyAsync(x => x.MessageId == id));
    }

    [Fact]
    public async Task StatusAndNonTextEventsAreAcknowledgedWithoutProcessing()
    {
        var body = JsonSerializer.Serialize(new { @object = "whatsapp_business_account", entry = new[] { new { changes = new[] { new { field = "messages", value = new { statuses = new[] { new { id = "wamid.status", status = "delivered" } } } } } } } });
        var response = await Send(body, Sign(body));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"accepted\":0", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WrongTestNumberOrRecipientIsNotStored()
    {
        var id = "wamid." + Guid.NewGuid().ToString("N");
        foreach (var body in new[] { Message(id, "Hello").Replace("123456789", "999999999"), Message(id, "Hello").Replace("233201234567", "233209999999") })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(body, Sign(body))).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<BusinessDbContext>().MetaWebhookEvents.AnyAsync(x => x.MessageId == id));
    }

    private async Task<HttpResponseMessage> Send(string body, string? signature)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Path) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (signature is not null) request.Headers.TryAddWithoutValidation("X-Hub-Signature-256", signature);
        return await fixture.Factory.CreateClient().SendAsync(request);
    }

    private static string Sign(string body) => "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    private static string Message(string id, string text) => JsonSerializer.Serialize(new
    {
        @object = "whatsapp_business_account",
        entry = new[] { new { changes = new[] { new { field = "messages", value = new {
            metadata = new { phone_number_id = "123456789" },
            messages = new[] { new { id, from = "233201234567", type = "text", text = new { body = text } } }
        } } } } }
    });
}
