using Microsoft.EntityFrameworkCore;
using Restaurant.Domain;
namespace Restaurant.Infrastructure;

public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options) : DbContext(options)
{
    public DbSet<RestaurantProfile> Restaurants => Set<RestaurantProfile>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MetaWebhookEvent> MetaWebhookEvents => Set<MetaWebhookEvent>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<RestaurantProfile>(b => {
            b.ToTable("Restaurants"); b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.TimeZone).HasMaxLength(80).IsRequired();
        });
        model.Entity<MenuCategory>(b => {
            b.ToTable("MenuCategories"); b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });
        model.Entity<MenuItem>(b => {
            b.ToTable("MenuItems", t => t.HasCheckConstraint("CK_MenuItems_PriceMinor", "\"PriceMinor\" > 0 AND \"PriceMinor\" <= 100000000"));
            b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.HasOne<MenuCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
            b.Property<uint>("Version").IsRowVersion();
        });
        model.Entity<MetaWebhookEvent>(b => {
            b.ToTable("MetaWebhookEvents"); b.HasKey(x => x.Id);
            b.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
            b.Property(x => x.PhoneNumberId).HasMaxLength(80).IsRequired();
            b.Property(x => x.SenderWaId).HasMaxLength(32).IsRequired();
            b.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            b.Property(x => x.State).HasMaxLength(24).IsRequired();
            b.HasIndex(x => x.MessageId).IsUnique();
            b.HasIndex(x => new { x.State, x.ReceivedAt });
        });
    }
}
