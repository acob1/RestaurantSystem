using Restaurant.Domain;
namespace Restaurant.UnitTests;
public sealed class CatalogTests
{
    [Theory][InlineData(0)][InlineData(-1)][InlineData(100000001)]
    public void PriceMustBePositiveAndBounded(long price) => Assert.Throws<ArgumentOutOfRangeException>(() => new MenuItem(Guid.NewGuid(), "Jollof", "", price));
    [Fact] public void PriceIsStoredInMinorUnits() => Assert.Equal(4550, new MenuItem(Guid.NewGuid(), "Jollof", "", 4550).PriceMinor);
    [Theory][InlineData("")][InlineData("   ")]
    public void CategoryMustHaveName(string name) => Assert.Throws<ArgumentException>(() => new MenuCategory(name));
    [Fact] public void CategoryTrimsName() => Assert.Equal("Mains", new MenuCategory(" Mains ").Name);
}
