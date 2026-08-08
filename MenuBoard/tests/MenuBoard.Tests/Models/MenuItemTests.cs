using Microsoft.EntityFrameworkCore;
using MenuBoard.Data;
using MenuBoard.Models;

namespace MenuBoard.Tests.Models;

[TestClass]
public class MenuItemTests
{
    private MenuDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MenuDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new MenuDbContext(options);
        return context;
    }

    [TestMethod]
    public void Seed_CreatesDisplaySettingsForBothScreens()
    {
        using var context = CreateContext();
        context.Seed();

        var settings = context.DisplaySettings.OrderBy(s => s.ScreenNumber).ToList();
        Assert.AreEqual(2, settings.Count);
        Assert.AreEqual(1, settings[0].ScreenNumber);
        Assert.AreEqual("Hot Food & Meals", settings[0].HeaderText);
        Assert.AreEqual(2, settings[1].ScreenNumber);
        Assert.AreEqual("Drinks & Snacks", settings[1].HeaderText);
    }

    [TestMethod]
    public void Seed_IsIdempotent()
    {
        using var context = CreateContext();
        context.Seed();
        context.Seed();

        Assert.AreEqual(2, context.DisplaySettings.Count());
    }

    [TestMethod]
    public void Category_CascadeDeletesItems()
    {
        using var context = CreateContext();
        var category = new Category { Name = "Burgers", ScreenNumber = 1 };
        category.Items.Add(new MenuItem { Name = "Classic", Price = 599 });
        context.Categories.Add(category);
        context.SaveChanges();

        context.Categories.Remove(category);
        context.SaveChanges();

        Assert.AreEqual(0, context.MenuItems.Count());
    }

    [TestMethod]
    public void MenuItem_PriceStoredAsCents()
    {
        using var context = CreateContext();
        var category = new Category { Name = "Test", ScreenNumber = 1 };
        category.Items.Add(new MenuItem { Name = "Item", Price = 1299 });
        context.Categories.Add(category);
        context.SaveChanges();

        var item = context.MenuItems.First();
        Assert.AreEqual(1299, item.Price);
    }
}
