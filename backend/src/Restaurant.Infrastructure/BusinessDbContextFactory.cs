using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Restaurant.Infrastructure;

// Design-time model generation does not need a live database or the API host.
public sealed class BusinessDbContextFactory : IDesignTimeDbContextFactory<BusinessDbContext>
{
    public BusinessDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<BusinessDbContext>()
            .UseNpgsql("Host=localhost;Database=restaurant_design_time;Username=unused;Password=unused")
            .Options);
}
