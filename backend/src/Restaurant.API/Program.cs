using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Restaurant.Application;
using Restaurant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
if (builder.Configuration.GetValue<bool>("Payments:AllowLivePayments") ||
    !string.Equals(builder.Configuration["Paystack:Mode"] ?? "Test", "Test", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("This milestone only supports test payments.");
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"));
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().WithMethods("GET")));
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("read", p => { p.PermitLimit = 120; p.Window = TimeSpan.FromMinutes(1); p.QueueLimit = 0; });
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"]);
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next(ctx);
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = x => x.Tags.Contains("ready") });
if (app.Environment.IsDevelopment()) app.MapOpenApi();
var api = app.MapGroup("/api/v1").RequireRateLimiting("read");
api.MapGet("/system/status", () => new SystemStatusDto("Phase 1", "Test", "GHS",
    ["API connectivity", "Database readiness", "Restaurant profile", "Read-only menu"],
    ["Staff authentication", "WhatsApp", "Orders", "Payments", "Kitchen", "Reports"]));
api.MapGet("/restaurant", async Task<IResult> (ICatalogQueries queries, CancellationToken ct) =>
    await queries.Restaurant(ct) is { } restaurant ? Results.Ok(restaurant) : Results.NotFound());
api.MapGet("/menu/categories", async (ICatalogQueries queries, CancellationToken ct) => Results.Ok(await queries.Categories(ct)));
api.MapGet("/menu/items", async Task<IResult> (ICatalogQueries queries, int? page, int? pageSize, CancellationToken ct) => {
    if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["pagination"] = ["page must be 1–100000; pageSize must be 1–100."] });
    return Results.Ok(await queries.Items(page ?? 1, pageSize ?? 20, ct));
});
if (args.Contains("--migrate")) {
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
    await db.Database.MigrateAsync();
    if (args.Contains("--seed")) {
        if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Sample seeds are development-only.");
        if (!await db.Restaurants.AnyAsync()) db.Restaurants.Add(new Restaurant.Domain.RestaurantProfile());
        if (!await db.MenuCategories.AnyAsync()) {
            var mains = new Restaurant.Domain.MenuCategory("Main dishes");
            var drinks = new Restaurant.Domain.MenuCategory("Drinks");
            db.MenuCategories.AddRange(mains, drinks);
            db.MenuItems.AddRange(
                new Restaurant.Domain.MenuItem(mains.Id, "Jollof rice with chicken", "Spiced jollof rice, grilled chicken, and fresh slaw.", 5500),
                new Restaurant.Domain.MenuItem(mains.Id, "Waakye bowl", "Rice and beans with egg, spaghetti, and shito.", 4500),
                new Restaurant.Domain.MenuItem(mains.Id, "Banku with tilapia", "Two balls of banku, grilled tilapia, and pepper sauce.", 8500),
                new Restaurant.Domain.MenuItem(drinks.Id, "Sobolo", "Chilled hibiscus drink with ginger and pineapple.", 1500));
        }
        await db.SaveChangesAsync();
    }
    return;
}
await app.RunAsync();

public partial class Program { }
public sealed class DatabaseHealthCheck(BusinessDbContext db, ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try {
            // A query detects missing migrations as well as network failures.
            await db.Restaurants.AsNoTracking().AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        } catch (Exception ex) {
            logger.LogWarning("Database readiness failed ({ExceptionType}).", ex.GetType().Name);
            return HealthCheckResult.Unhealthy("Database unavailable or migrations missing.");
        }
    }
}
