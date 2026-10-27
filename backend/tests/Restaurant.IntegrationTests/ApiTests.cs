using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application;
using Restaurant.Domain;
using Restaurant.Infrastructure;
using Testcontainers.PostgreSql;
namespace Restaurant.IntegrationTests;

// Without TEST_DATABASE_CONNECTION, Docker creates and destroys an isolated PostgreSQL database.
// An explicit database is accepted only when its name ends in _test. It is never dropped.
public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION");
        if (connection is null) {
            container = new PostgreSqlBuilder().WithImage("postgres:17.6-alpine").WithDatabase("restaurant_test").Build();
            await container.StartAsync(); connection = container.GetConnectionString();
        } else if (!new Npgsql.NpgsqlConnectionStringBuilder(connection).Database!.EndsWith("_test", StringComparison.Ordinal))
            throw new InvalidOperationException("Integration databases must end in _test.");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => {
            b.UseEnvironment("Testing");
            // Host settings are visible during minimal-host startup, before DI registration.
            b.UseSetting("ConnectionStrings:DefaultConnection", connection);
            b.UseSetting("Meta:TestMode", "true");
            b.UseSetting("Meta:VerifyToken", "integration-test-verify-token");
            b.UseSetting("Meta:AppSecret", "integration-test-app-secret");
            b.UseSetting("Meta:PhoneNumberId", "123456789");
            b.UseSetting("Meta:AllowedTestRecipient", "233201234567");
        });
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        await db.Database.MigrateAsync();
        if (!await db.Restaurants.AnyAsync()) { db.Restaurants.Add(new RestaurantProfile()); await db.SaveChangesAsync(); }
    }
    public async Task DisposeAsync() { await Factory.DisposeAsync(); if (container is not null) await container.DisposeAsync(); }
}
public sealed class ApiTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Theory][InlineData("/health/live")][InlineData("/health/ready")]
    public async Task HealthIsAvailable(string path) => Assert.Equal(HttpStatusCode.OK, (await fixture.Factory.CreateClient().GetAsync(path)).StatusCode);
    [Fact] public async Task StatusCannotAdvertiseLivePayments() {
        var status = await fixture.Factory.CreateClient().GetFromJsonAsync<SystemStatusDto>("/api/v1/system/status");
        Assert.Equal("Test", status!.PaymentMode);
    }
    [Fact] public async Task InvalidPaginationReturnsProblem() => Assert.Equal(HttpStatusCode.BadRequest,
        (await fixture.Factory.CreateClient().GetAsync("/api/v1/menu/items?pageSize=101")).StatusCode);
    [Fact] public async Task RestaurantComesFromDatabase() {
        var profile = await fixture.Factory.CreateClient().GetFromJsonAsync<RestaurantDto>("/api/v1/restaurant");
        Assert.Equal("GHS", profile!.Currency);
    }
    [Fact] public async Task MigrationCreatesRelationalConstraints() {
        using var scope = fixture.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        db.MenuItems.Add(new MenuItem(Guid.NewGuid(), "Unknown category", "", 100));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact] public async Task ModelMatchesMigrations() {
        using var scope = fixture.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
