using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Restaurant.API;
using Restaurant.Infrastructure;

namespace Restaurant.IntegrationTests;

public sealed class MetaDispatchTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task SignedDuplicateIsDispatchedOnlyOnce()
    {
        var id = "wamid." + Guid.NewGuid().ToString("N");
        var body = JsonSerializer.Serialize(new {
            @object = "whatsapp_business_account",
            entry = new[] { new { changes = new[] { new { field = "messages", value = new {
                metadata = new { phone_number_id = "123456789" },
                messages = new[] { new { id, from = "233201234567", type = "text", text = new { body = "Hello" } } }
            } } } } }
        });
        using var client = fixture.Factory.CreateClient();
        async Task<HttpResponseMessage> Send()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/whatsapp") {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("X-Hub-Signature-256",
                "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("integration-test-app-secret"),
                    Encoding.UTF8.GetBytes(body))).ToLowerInvariant());
            return await client.SendAsync(request);
        }

        Assert.Equal(HttpStatusCode.OK, (await Send()).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send()).StatusCode);
        var fake = new BridgeHandler(id);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Meta:TestMode"] = "true", ["Automation:MetaBridgeUrl"] = "http://localhost/bridge",
            ["Automation:MetaBridgeToken"] = "test-bridge-token"
        }).Build();
        var worker = new MetaTestDispatchWorker(fixture.Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            new FakeHttpClientFactory(fake), config, fixture.Factory.Services.GetRequiredService<IHostEnvironment>(),
            fixture.Factory.Services.GetRequiredService<ILogger<MetaTestDispatchWorker>>());
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var accepted = false;
            for (var i = 0; i < 100; i++)
            {
                using var scope = fixture.Factory.Services.CreateScope();
                var state = await scope.ServiceProvider.GetRequiredService<BusinessDbContext>().MetaWebhookEvents
                    .Where(x => x.MessageId == id).Select(x => x.State).SingleAsync();
                if (state == "MetaAccepted") { accepted = true; break; }
                await Task.Delay(50);
            }
            Assert.True(accepted, "The durable test inbox should dispatch to the bridge.");
            Assert.Equal(1, fake.Count);
            Assert.Equal(HttpStatusCode.OK, (await Send()).StatusCode);
            await Task.Delay(100);
            Assert.Equal(1, fake.Count);
        }
        finally { await worker.StopAsync(CancellationToken.None); worker.Dispose(); }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class BridgeHandler(string expectedId) : HttpMessageHandler
    {
        private int count;
        public int Count => Volatile.Read(ref count);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref count);
            Assert.Equal("test-bridge-token", request.Headers.GetValues("X-Restaurant-Meta-Bridge-Token").Single());
            var body = await request.Content!.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken);
            Assert.Equal(expectedId, body!["messageId"].ToString());
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent.Create(new { ok = true, messageId = expectedId })
            };
        }
    }
}
