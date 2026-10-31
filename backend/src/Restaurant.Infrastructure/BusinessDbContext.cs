using Microsoft.EntityFrameworkCore;
using Restaurant.Domain;
namespace Restaurant.Infrastructure;

public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options) : DbContext(options)
{
    public DbSet<RestaurantProfile> Restaurants => Set<RestaurantProfile>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MetaWebhookEvent> MetaWebhookEvents => Set<MetaWebhookEvent>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<PaymentEvidence> PaymentEvidences => Set<PaymentEvidence>();
    public DbSet<PaymentReview> PaymentReviews => Set<PaymentReview>();
    public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Restaurant
        model.Entity<RestaurantProfile>(b => {
            b.ToTable("Restaurants"); b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.TimeZone).HasMaxLength(80).IsRequired();
        });

        // Menu
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

        // Meta Webhook
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

        // Customers
        model.Entity<Customer>(b => {
            b.ToTable("Customers"); b.HasKey(x => x.Id);
            b.Property(x => x.WhatsAppId).HasMaxLength(32).IsRequired();
            b.Property(x => x.Name).HasMaxLength(100);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.HasIndex(x => x.WhatsAppId).IsUnique();
        });
        model.Entity<CustomerAddress>(b => {
            b.ToTable("CustomerAddresses"); b.HasKey(x => x.Id);
            b.Property(x => x.Label).HasMaxLength(50).IsRequired();
            b.Property(x => x.Address).HasMaxLength(500).IsRequired();
            b.Property(x => x.Landmark).HasMaxLength(200);
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        // Carts
        model.Entity<Cart>(b => {
            b.ToTable("Carts"); b.HasKey(x => x.Id);
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.CustomerId).IsUnique();
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        model.Entity<CartItem>(b => {
            b.ToTable("CartItems"); b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Instructions).HasMaxLength(500);
            b.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(x => x.LineTotalMinor);
        });

        // Orders
        model.Entity<Order>(b => {
            b.ToTable("Orders"); b.HasKey(x => x.Id);
            b.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.Fulfillment).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.DeliveryAddress).HasMaxLength(500);
            b.Property(x => x.DeliveryLandmark).HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAt);
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Property<uint>("Version").IsRowVersion();
        });
        model.Entity<OrderItem>(b => {
            b.ToTable("OrderItems"); b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.Instructions).HasMaxLength(500);
            b.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
            b.Ignore(x => x.LineTotalMinor);
        });
        model.Entity<OrderStatusHistory>(b => {
            b.ToTable("OrderStatusHistories"); b.HasKey(x => x.Id);
            b.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.Property(x => x.ChangedBy).HasMaxLength(100);
            b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.OrderId);
        });

        // Payments
        model.Entity<PaymentAttempt>(b => {
            b.ToTable("PaymentAttempts"); b.HasKey(x => x.Id);
            b.Property(x => x.Reference).HasMaxLength(100).IsRequired();
            b.Property(x => x.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.ExternalReference).HasMaxLength(200);
            b.Property(x => x.CheckoutUrl).HasMaxLength(500);
            b.Property(x => x.FailureReason).HasMaxLength(500);
            b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.Reference).IsUnique();
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.Status);
        });
        model.Entity<PaymentEvidence>(b => {
            b.ToTable("PaymentEvidences"); b.HasKey(x => x.Id);
            b.Property(x => x.CloudinaryPublicId).HasMaxLength(200).IsRequired();
            b.Property(x => x.CloudinaryAssetId).HasMaxLength(200).IsRequired();
            b.Property(x => x.CloudinaryUrl).HasMaxLength(500).IsRequired();
            b.Property(x => x.FileHash).HasMaxLength(128).IsRequired();
            b.Property(x => x.OcrText).HasMaxLength(4000);
            b.Property(x => x.OcrTransactionRef).HasMaxLength(100);
            b.HasOne<PaymentAttempt>().WithMany().HasForeignKey(x => x.PaymentAttemptId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.FileHash);
        });
        model.Entity<PaymentReview>(b => {
            b.ToTable("PaymentReviews"); b.HasKey(x => x.Id);
            b.Property(x => x.ReviewedBy).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.HasOne<PaymentAttempt>().WithMany().HasForeignKey(x => x.PaymentAttemptId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<PaymentEvidence>().WithMany().HasForeignKey(x => x.EvidenceId).OnDelete(DeleteBehavior.SetNull);
        });

        // Conversations
        model.Entity<ConversationSession>(b => {
            b.ToTable("ConversationSessions"); b.HasKey(x => x.Id);
            b.Property(x => x.State).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.CurrentIntent).HasMaxLength(50);
            b.Property(x => x.HandoffReason).HasMaxLength(500);
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Cart>().WithMany().HasForeignKey(x => x.ActiveCartId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<Order>().WithMany().HasForeignKey(x => x.ActiveOrderId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => new { x.CustomerId, x.State });
        });
        model.Entity<ConversationMessage>(b => {
            b.ToTable("ConversationMessages"); b.HasKey(x => x.Id);
            b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
            b.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Intent).HasMaxLength(50);
            b.Property(x => x.ExternalMessageId).HasMaxLength(200);
            b.HasOne<ConversationSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.ExternalMessageId);
        });
    }
}
