using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Restaurant.Application;
namespace Restaurant.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, bool development)
    {
        var connection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required. See .env.example.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (!development && parsed.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException("Hosted databases must use SSL Mode=VerifyFull.");
        parsed.MaxPoolSize = 10; parsed.Timeout = 10; parsed.CommandTimeout = 15;
        services.AddDbContext<BusinessDbContext>(o => o.UseNpgsql(parsed.ConnectionString));
        services.AddScoped<ICatalogQueries, CatalogQueries>();
        return services;
    }
}
