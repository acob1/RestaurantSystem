namespace Restaurant.Application;

public sealed record RestaurantDto(Guid Id, string Name, string Currency, string TimeZone);
public sealed record CategoryDto(Guid Id, string Name);
public sealed record MenuItemDto(Guid Id, Guid CategoryId, string Name, string Description, long PriceMinor, bool Available);
public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int Total);
public sealed record SystemStatusDto(string Phase, string PaymentMode, string Currency, string[] AvailableFeatures, string[] PlannedFeatures);
public interface ICatalogQueries
{
    Task<RestaurantDto?> Restaurant(CancellationToken ct);
    Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct);
    Task<Page<MenuItemDto>> Items(int page, int pageSize, CancellationToken ct);
}
