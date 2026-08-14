using Microsoft.EntityFrameworkCore;
using MenuBoard.Data;
using MenuBoard.Services;

namespace MenuBoard.Tests.Services;

[TestClass]
public class MenuDataServiceTests
{
    private MenuDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MenuDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new MenuDbContext(options);
        context.Database.EnsureCreated();
        context.Seed();
        return context;
    }

    [TestMethod]
    public void AddCategory_CreatesAndRaisesEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);
        bool eventFired = false;
        service.DataChanged += () => eventFired = true;

        var category = service.AddCategory("Burgers", 1);

        Assert.AreEqual("Burgers", category.Name);
        Assert.AreEqual(1, category.ScreenNumber);
        Assert.IsTrue(eventFired);
    }

    [TestMethod]
    public void AddMenuItem_CreatesUnderCategory()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);

        var category = service.AddCategory("Burgers", 1);
        var item = service.AddMenuItem(category.Id, "Classic Burger", 599);

        Assert.AreEqual("Classic Burger", item.Name);
        Assert.AreEqual(599, item.Price);
        Assert.AreEqual(category.Id, item.CategoryId);
    }

    [TestMethod]
    public void GetCategoriesForScreen_ReturnsOnlyMatchingScreen()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);

        service.AddCategory("Burgers", 1);
        service.AddCategory("Drinks", 2);

        var screen1 = service.GetCategoriesForScreen(1);
        Assert.HasCount(1, screen1);
        Assert.AreEqual("Burgers", screen1[0].Name);
    }

    [TestMethod]
    public void GetCategoriesForScreen_ExcludesUnavailableItems()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);

        var cat = service.AddCategory("Food", 1);
        var item1 = service.AddMenuItem(cat.Id, "Visible", 100);
        var item2 = service.AddMenuItem(cat.Id, "Hidden", 200);
        item2.IsAvailable = false;
        service.UpdateMenuItem(item2);

        var categories = service.GetCategoriesForScreen(1);
        Assert.HasCount(1, categories[0].Items);
        Assert.AreEqual("Visible", categories[0].Items.First().Name);
    }

    [TestMethod]
    public void DeleteCategory_RemovesAndRaisesEvent()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);
        bool eventFired = false;

        var cat = service.AddCategory("Temp", 1);
        service.DataChanged += () => eventFired = true;
        service.DeleteCategory(cat.Id);

        Assert.IsEmpty(service.GetCategoriesForScreen(1));
        Assert.IsTrue(eventFired);
    }

    [TestMethod]
    public void GetDisplaySettings_ReturnsSeedData()
    {
        var dbName = Guid.NewGuid().ToString();
        using var context = CreateContext(dbName);
        var service = new MenuDataService(context);

        var settings = service.GetDisplaySettings(1);
        Assert.AreEqual("Hot Food & Meals", settings.HeaderText);
        Assert.AreEqual("#000000", settings.BackgroundColor);
    }
}
