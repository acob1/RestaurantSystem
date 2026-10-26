namespace Restaurant.Domain;

public sealed class RestaurantProfile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "Accra Kitchen";
    public string Currency { get; private set; } = "GHS";
    public string TimeZone { get; private set; } = "Africa/Accra";
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class MenuCategory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    private MenuCategory() { }
    public MenuCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new ArgumentException("Category name must contain 1–100 characters.", nameof(name));
        Name = name.Trim();
    }
}

public sealed class MenuItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public long PriceMinor { get; private set; }
    public bool Available { get; private set; } = true;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    private MenuItem() { }
    public MenuItem(Guid categoryId, string name, string description, long priceMinor)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new ArgumentException("Item name must contain 1–160 characters.");
        if (description.Length > 2000) throw new ArgumentException("Description is too long.");
        if (priceMinor <= 0 || priceMinor > 100_000_000) throw new ArgumentOutOfRangeException(nameof(priceMinor));
        CategoryId = categoryId; Name = name.Trim(); Description = description; PriceMinor = priceMinor;
    }
}
