using Microsoft.EntityFrameworkCore;
using Restaurant.Application;
namespace Restaurant.Infrastructure;
public sealed class CatalogQueries(BusinessDbContext db) : ICatalogQueries
{
    public Task<RestaurantDto?> Restaurant(CancellationToken ct) => db.Restaurants.AsNoTracking()
        .Select(x => new RestaurantDto(x.Id, x.Name, x.Currency, x.TimeZone)).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) => await db.MenuCategories.AsNoTracking()
        .OrderBy(x => x.Name).Select(x => new CategoryDto(x.Id, x.Name)).ToListAsync(ct);
    public async Task<Page<MenuItemDto>> Items(int page, int pageSize, CancellationToken ct)
    {
        var total = await db.MenuItems.CountAsync(ct);
        var items = await db.MenuItems.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new MenuItemDto(x.Id, x.CategoryId, x.Name, x.Description, x.PriceMinor, x.Available)).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }
}
