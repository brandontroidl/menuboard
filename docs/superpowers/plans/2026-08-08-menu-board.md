# Menu Board Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a WPF application that drives two TVs as convenience store menu boards with a built-in admin editor.

**Architecture:** Single .NET 8 WPF app with three windows - an admin editor on the primary monitor and two borderless fullscreen display windows on secondary/tertiary monitors. SQLite stores menu data, EF Core handles persistence, CommunityToolkit.Mvvm provides MVVM bindings. Admin raises in-process events to refresh displays instantly.

**Tech Stack:** .NET 8, WPF, SQLite, Microsoft.EntityFrameworkCore.Sqlite, CommunityToolkit.Mvvm

## Global Constraints

- .NET 8 (net8.0-windows TFM)
- Prices stored as integer cents, never floats
- Images stored as files in `Images/` next to the executable, database holds relative paths
- All data changes save immediately (no explicit save button)
- Display windows: `WindowStyle=None, WindowState=Maximized, Topmost=True`

## File Map

```
MenuBoard/
  MenuBoard.sln
  src/MenuBoard/
    MenuBoard.csproj
    App.xaml
    App.xaml.cs
    Models/
      Category.cs
      MenuItem.cs
      DisplaySettings.cs
    Data/
      MenuDbContext.cs
    Services/
      MonitorService.cs
      ImageService.cs
      MenuDataService.cs          -- central data access + change event
    ViewModels/
      DisplayViewModel.cs
      AdminViewModel.cs
    Views/
      DisplayWindow.xaml
      AdminWindow.xaml
    Converters/
      CentsToDollarsConverter.cs
      ImagePathConverter.cs
  tests/MenuBoard.Tests/
    MenuBoard.Tests.csproj
    Models/
      MenuItemTests.cs
    Services/
      MenuDataServiceTests.cs
    Converters/
      CentsToDollarsConverterTests.cs
```

---

### Task 1: Project scaffold and data layer

Create the solution, project, models, DbContext, and seed logic. This is the foundation everything else builds on.

**Files:**
- Create: `MenuBoard/MenuBoard.sln`
- Create: `MenuBoard/src/MenuBoard/MenuBoard.csproj`
- Create: `MenuBoard/src/MenuBoard/Models/Category.cs`
- Create: `MenuBoard/src/MenuBoard/Models/MenuItem.cs`
- Create: `MenuBoard/src/MenuBoard/Models/DisplaySettings.cs`
- Create: `MenuBoard/src/MenuBoard/Data/MenuDbContext.cs`
- Create: `MenuBoard/tests/MenuBoard.Tests/MenuBoard.Tests.csproj`
- Create: `MenuBoard/tests/MenuBoard.Tests/Models/MenuItemTests.cs`

**Interfaces:**
- Consumes: nothing (first task)
- Produces:
  - `Category` class: `int Id`, `string Name`, `int DisplayOrder`, `int ScreenNumber`, `ICollection<MenuItem> Items`
  - `MenuItem` class: `int Id`, `int CategoryId`, `Category Category`, `string Name`, `int Price`, `string? Description`, `string? ImagePath`, `int DisplayOrder`, `bool IsAvailable`
  - `DisplaySettings` class: `int Id`, `int ScreenNumber`, `string BackgroundColor`, `string HeaderText`, `double FontScale`
  - `MenuDbContext` : DbContext with `DbSet<Category> Categories`, `DbSet<MenuItem> MenuItems`, `DbSet<DisplaySettings> DisplaySettings`, `void Seed()` method

- [ ] **Step 1: Create solution and project**

```bash
mkdir -p MenuBoard/src/MenuBoard
mkdir -p MenuBoard/tests/MenuBoard.Tests
cd MenuBoard
dotnet new sln -n MenuBoard
dotnet new wpf -n MenuBoard -o src/MenuBoard --framework net8.0
dotnet new mstest -n MenuBoard.Tests -o tests/MenuBoard.Tests --framework net8.0
dotnet sln add src/MenuBoard/MenuBoard.csproj
dotnet sln add tests/MenuBoard.Tests/MenuBoard.Tests.csproj
dotnet add tests/MenuBoard.Tests reference src/MenuBoard
```

- [ ] **Step 2: Add NuGet packages**

```bash
cd MenuBoard
dotnet add src/MenuBoard package Microsoft.EntityFrameworkCore.Sqlite --version 8.*
dotnet add src/MenuBoard package Microsoft.EntityFrameworkCore.Design --version 8.*
dotnet add src/MenuBoard package CommunityToolkit.Mvvm --version 8.*
dotnet add tests/MenuBoard.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.*
```

- [ ] **Step 3: Write model classes**

`src/MenuBoard/Models/Category.cs`:
```csharp
namespace MenuBoard.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int ScreenNumber { get; set; }
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
```

`src/MenuBoard/Models/MenuItem.cs`:
```csharp
namespace MenuBoard.Models;

public class MenuItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
}
```

`src/MenuBoard/Models/DisplaySettings.cs`:
```csharp
namespace MenuBoard.Models;

public class DisplaySettings
{
    public int Id { get; set; }
    public int ScreenNumber { get; set; }
    public string BackgroundColor { get; set; } = "#000000";
    public string HeaderText { get; set; } = string.Empty;
    public double FontScale { get; set; } = 1.0;
}
```

- [ ] **Step 4: Write MenuDbContext**

`src/MenuBoard/Data/MenuDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using MenuBoard.Models;

namespace MenuBoard.Data;

public class MenuDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<DisplaySettings> DisplaySettings => Set<DisplaySettings>();

    public MenuDbContext(DbContextOptions<MenuDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Category)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DisplaySettings>()
            .HasIndex(d => d.ScreenNumber)
            .IsUnique();
    }

    public void Seed()
    {
        if (!DisplaySettings.Any())
        {
            DisplaySettings.Add(new DisplaySettings
            {
                ScreenNumber = 1,
                BackgroundColor = "#000000",
                HeaderText = "Hot Food & Meals",
                FontScale = 1.0
            });
            DisplaySettings.Add(new DisplaySettings
            {
                ScreenNumber = 2,
                BackgroundColor = "#000000",
                HeaderText = "Drinks & Snacks",
                FontScale = 1.0
            });
            SaveChanges();
        }
    }
}
```

- [ ] **Step 5: Write failing test for model relationships and seed**

`tests/MenuBoard.Tests/Models/MenuItemTests.cs`:
```csharp
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
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
cd MenuBoard
dotnet test tests/MenuBoard.Tests --verbosity normal
```
Expected: all 4 tests pass.

- [ ] **Step 7: Commit**

```bash
cd MenuBoard
git init
git add -A
git commit -m "feat: project scaffold with models, dbcontext, and seed logic"
```

---

### Task 2: MenuDataService and converters

Central data access service that wraps all CRUD operations and raises a `DataChanged` event. Plus the value converters for price and image display.

**Files:**
- Create: `MenuBoard/src/MenuBoard/Services/MenuDataService.cs`
- Create: `MenuBoard/src/MenuBoard/Services/ImageService.cs`
- Create: `MenuBoard/src/MenuBoard/Services/MonitorService.cs`
- Create: `MenuBoard/src/MenuBoard/Converters/CentsToDollarsConverter.cs`
- Create: `MenuBoard/src/MenuBoard/Converters/ImagePathConverter.cs`
- Create: `MenuBoard/tests/MenuBoard.Tests/Services/MenuDataServiceTests.cs`
- Create: `MenuBoard/tests/MenuBoard.Tests/Converters/CentsToDollarsConverterTests.cs`

**Interfaces:**
- Consumes: `MenuDbContext`, `Category`, `MenuItem`, `DisplaySettings` from Task 1
- Produces:
  - `MenuDataService`:
    - `event Action DataChanged`
    - `List<Category> GetCategoriesForScreen(int screenNumber)` (includes Items, ordered, only IsAvailable items)
    - `DisplaySettings GetDisplaySettings(int screenNumber)`
    - `Category AddCategory(string name, int screenNumber)`
    - `void UpdateCategory(Category category)`
    - `void DeleteCategory(int categoryId)`
    - `void ReorderCategory(int categoryId, int newOrder)`
    - `MenuItem AddMenuItem(int categoryId, string name, int price)`
    - `void UpdateMenuItem(MenuItem item)`
    - `void DeleteMenuItem(int menuItemId)`
    - `void ReorderMenuItem(int menuItemId, int newOrder)`
    - `void UpdateDisplaySettings(DisplaySettings settings)`
  - `ImageService`:
    - `string CopyImageToStore(string sourcePath)` - copies file to Images/, returns relative path
    - `string GetFullPath(string relativePath)` - resolves relative to app base
  - `MonitorService`:
    - `List<MonitorInfo> GetMonitors()` where `MonitorInfo` has `Rect Bounds`, `bool IsPrimary`
  - `CentsToDollarsConverter` : IValueConverter (int cents -> string "$X.XX")
  - `ImagePathConverter` : IValueConverter (string? relativePath -> BitmapImage or null)

- [ ] **Step 1: Write failing tests for CentsToDollarsConverter**

`tests/MenuBoard.Tests/Converters/CentsToDollarsConverterTests.cs`:
```csharp
using System.Globalization;
using MenuBoard.Converters;

namespace MenuBoard.Tests.Converters;

[TestClass]
public class CentsToDollarsConverterTests
{
    private readonly CentsToDollarsConverter _converter = new();

    [TestMethod]
    public void Convert_599_ReturnsDollarString()
    {
        var result = _converter.Convert(599, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$5.99", result);
    }

    [TestMethod]
    public void Convert_0_ReturnsZero()
    {
        var result = _converter.Convert(0, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$0.00", result);
    }

    [TestMethod]
    public void Convert_1000_ReturnsTenDollars()
    {
        var result = _converter.Convert(1000, typeof(string), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual("$10.00", result);
    }

    [TestMethod]
    public void ConvertBack_DollarString_ReturnsCents()
    {
        var result = _converter.ConvertBack("$5.99", typeof(int), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual(599, result);
    }

    [TestMethod]
    public void ConvertBack_PlainNumber_ReturnsCents()
    {
        var result = _converter.ConvertBack("5.99", typeof(int), null!, CultureInfo.InvariantCulture);
        Assert.AreEqual(599, result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd MenuBoard
dotnet test tests/MenuBoard.Tests --filter "FullyQualifiedName~CentsToDollarsConverterTests" --verbosity normal
```
Expected: FAIL - class not found.

- [ ] **Step 3: Implement CentsToDollarsConverter**

`src/MenuBoard/Converters/CentsToDollarsConverter.cs`:
```csharp
using System.Globalization;
using System.Windows.Data;

namespace MenuBoard.Converters;

public class CentsToDollarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cents)
            return $"${cents / 100.0:F2}";
        return "$0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            s = s.TrimStart('$');
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var dollars))
                return (int)(dollars * 100);
        }
        return 0;
    }
}
```

- [ ] **Step 4: Run converter tests to verify they pass**

```bash
cd MenuBoard
dotnet test tests/MenuBoard.Tests --filter "FullyQualifiedName~CentsToDollarsConverterTests" --verbosity normal
```
Expected: all 5 pass.

- [ ] **Step 5: Write failing tests for MenuDataService**

`tests/MenuBoard.Tests/Services/MenuDataServiceTests.cs`:
```csharp
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
        Assert.AreEqual(1, screen1.Count);
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
        Assert.AreEqual(1, categories[0].Items.Count);
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

        Assert.AreEqual(0, service.GetCategoriesForScreen(1).Count);
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
```

- [ ] **Step 6: Run tests to verify they fail**

```bash
cd MenuBoard
dotnet test tests/MenuBoard.Tests --filter "FullyQualifiedName~MenuDataServiceTests" --verbosity normal
```
Expected: FAIL - class not found.

- [ ] **Step 7: Implement MenuDataService**

`src/MenuBoard/Services/MenuDataService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using MenuBoard.Data;
using MenuBoard.Models;

namespace MenuBoard.Services;

public class MenuDataService
{
    private readonly MenuDbContext _context;

    public event Action? DataChanged;

    public MenuDataService(MenuDbContext context)
    {
        _context = context;
    }

    public List<Category> GetCategoriesForScreen(int screenNumber)
    {
        return _context.Categories
            .Where(c => c.ScreenNumber == screenNumber)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.Items.Where(i => i.IsAvailable).OrderBy(i => i.DisplayOrder))
            .AsNoTracking()
            .ToList();
    }

    public List<Category> GetAllCategoriesForScreen(int screenNumber)
    {
        return _context.Categories
            .Where(c => c.ScreenNumber == screenNumber)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.Items.OrderBy(i => i.DisplayOrder))
            .ToList();
    }

    public DisplaySettings GetDisplaySettings(int screenNumber)
    {
        return _context.DisplaySettings.First(d => d.ScreenNumber == screenNumber);
    }

    public Category AddCategory(string name, int screenNumber)
    {
        var maxOrder = _context.Categories
            .Where(c => c.ScreenNumber == screenNumber)
            .Select(c => (int?)c.DisplayOrder)
            .Max() ?? -1;

        var category = new Category
        {
            Name = name,
            ScreenNumber = screenNumber,
            DisplayOrder = maxOrder + 1
        };
        _context.Categories.Add(category);
        _context.SaveChanges();
        DataChanged?.Invoke();
        return category;
    }

    public void UpdateCategory(Category category)
    {
        _context.Categories.Update(category);
        _context.SaveChanges();
        DataChanged?.Invoke();
    }

    public void DeleteCategory(int categoryId)
    {
        var category = _context.Categories.Find(categoryId);
        if (category is not null)
        {
            _context.Categories.Remove(category);
            _context.SaveChanges();
            DataChanged?.Invoke();
        }
    }

    public void ReorderCategory(int categoryId, int newOrder)
    {
        var category = _context.Categories.Find(categoryId);
        if (category is not null)
        {
            category.DisplayOrder = newOrder;
            _context.SaveChanges();
            DataChanged?.Invoke();
        }
    }

    public MenuItem AddMenuItem(int categoryId, string name, int price)
    {
        var maxOrder = _context.MenuItems
            .Where(i => i.CategoryId == categoryId)
            .Select(i => (int?)i.DisplayOrder)
            .Max() ?? -1;

        var item = new MenuItem
        {
            CategoryId = categoryId,
            Name = name,
            Price = price,
            DisplayOrder = maxOrder + 1
        };
        _context.MenuItems.Add(item);
        _context.SaveChanges();
        DataChanged?.Invoke();
        return item;
    }

    public void UpdateMenuItem(MenuItem item)
    {
        _context.MenuItems.Update(item);
        _context.SaveChanges();
        DataChanged?.Invoke();
    }

    public void DeleteMenuItem(int menuItemId)
    {
        var item = _context.MenuItems.Find(menuItemId);
        if (item is not null)
        {
            _context.MenuItems.Remove(item);
            _context.SaveChanges();
            DataChanged?.Invoke();
        }
    }

    public void ReorderMenuItem(int menuItemId, int newOrder)
    {
        var item = _context.MenuItems.Find(menuItemId);
        if (item is not null)
        {
            item.DisplayOrder = newOrder;
            _context.SaveChanges();
            DataChanged?.Invoke();
        }
    }

    public void UpdateDisplaySettings(DisplaySettings settings)
    {
        _context.DisplaySettings.Update(settings);
        _context.SaveChanges();
        DataChanged?.Invoke();
    }
}
```

- [ ] **Step 8: Implement ImageService**

`src/MenuBoard/Services/ImageService.cs`:
```csharp
using System.IO;

namespace MenuBoard.Services;

public class ImageService
{
    private readonly string _imageDirectory;

    public ImageService()
    {
        _imageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        Directory.CreateDirectory(_imageDirectory);
    }

    public string CopyImageToStore(string sourcePath)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_imageDirectory, fileName);
        File.Copy(sourcePath, destPath);
        return fileName;
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_imageDirectory, relativePath);
    }
}
```

- [ ] **Step 9: Implement MonitorService**

`src/MenuBoard/Services/MonitorService.cs`:
```csharp
using System.Windows;

namespace MenuBoard.Services;

public class MonitorInfo
{
    public Rect Bounds { get; init; }
    public bool IsPrimary { get; init; }
}

public class MonitorService
{
    public List<MonitorInfo> GetMonitors()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        return screens.Select(s => new MonitorInfo
        {
            Bounds = new Rect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
            IsPrimary = s.Primary
        }).ToList();
    }
}
```

- [ ] **Step 10: Implement ImagePathConverter**

`src/MenuBoard/Converters/ImagePathConverter.cs`:
```csharp
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MenuBoard.Converters;

public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string relativePath && !string.IsNullOrEmpty(relativePath))
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", relativePath);
            if (File.Exists(fullPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

- [ ] **Step 11: Add WinForms reference to csproj for Screen enumeration**

Add to `src/MenuBoard/MenuBoard.csproj` inside `<PropertyGroup>`:
```xml
<UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 12: Run all tests**

```bash
cd MenuBoard
dotnet test --verbosity normal
```
Expected: all tests pass (4 model tests + 5 converter tests + 6 service tests).

- [ ] **Step 13: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "feat: data service, image service, monitor service, and converters"
```

---

### Task 3: Display window

The borderless fullscreen window that renders the menu for one screen. Reused for both TVs - each instance gets a different screen number.

**Files:**
- Create: `MenuBoard/src/MenuBoard/ViewModels/DisplayViewModel.cs`
- Create: `MenuBoard/src/MenuBoard/Views/DisplayWindow.xaml`
- Create: `MenuBoard/src/MenuBoard/Views/DisplayWindow.xaml.cs`

**Interfaces:**
- Consumes: `MenuDataService.GetCategoriesForScreen()`, `MenuDataService.GetDisplaySettings()`, `MenuDataService.DataChanged` event, `CentsToDollarsConverter`, `ImagePathConverter`
- Produces:
  - `DisplayViewModel`: `int ScreenNumber`, `ObservableCollection<Category> Categories`, `DisplaySettings Settings`, `void Refresh()`, constructor takes `MenuDataService` and `int screenNumber`
  - `DisplayWindow`: constructor takes `DisplayViewModel`

- [ ] **Step 1: Implement DisplayViewModel**

`src/MenuBoard/ViewModels/DisplayViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MenuBoard.Models;
using MenuBoard.Services;

namespace MenuBoard.ViewModels;

public partial class DisplayViewModel : ObservableObject
{
    private readonly MenuDataService _dataService;
    private readonly int _screenNumber;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private DisplaySettings _settings = new();

    public DisplayViewModel(MenuDataService dataService, int screenNumber)
    {
        _dataService = dataService;
        _screenNumber = screenNumber;
        _dataService.DataChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        var cats = _dataService.GetCategoriesForScreen(_screenNumber);
        Categories = new ObservableCollection<Category>(cats);
        Settings = _dataService.GetDisplaySettings(_screenNumber);
    }
}
```

- [ ] **Step 2: Create DisplayWindow XAML**

`src/MenuBoard/Views/DisplayWindow.xaml`:
```xml
<Window x:Class="MenuBoard.Views.DisplayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:converters="clr-namespace:MenuBoard.Converters"
        WindowStyle="None"
        WindowState="Maximized"
        Topmost="True"
        ShowInTaskbar="False">
    <Window.Resources>
        <converters:CentsToDollarsConverter x:Key="CentsToDollars"/>
        <converters:ImagePathConverter x:Key="ImagePath"/>
    </Window.Resources>

    <Grid x:Name="RootGrid">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Padding="20,15" Background="#1a1a1a">
            <TextBlock Text="{Binding Settings.HeaderText}"
                       FontSize="48"
                       FontWeight="Bold"
                       Foreground="White"
                       HorizontalAlignment="Center"/>
        </Border>

        <!-- Menu Content -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Hidden">
            <ItemsControl ItemsSource="{Binding Categories}" Margin="40,20">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Margin="0,0,0,30">
                            <!-- Category Name -->
                            <TextBlock Text="{Binding Name}"
                                       FontSize="36"
                                       FontWeight="Bold"
                                       Foreground="#FFD700"
                                       Margin="0,0,0,10"
                                       TextTransform="Uppercase"/>
                            <Rectangle Height="2" Fill="#FFD700" Margin="0,0,0,10"/>

                            <!-- Items -->
                            <ItemsControl ItemsSource="{Binding Items}">
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Grid Margin="10,6">
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width="Auto"/>
                                                <ColumnDefinition Width="*"/>
                                                <ColumnDefinition Width="Auto"/>
                                            </Grid.ColumnDefinitions>

                                            <!-- Optional Image -->
                                            <Image Grid.Column="0"
                                                   Source="{Binding ImagePath, Converter={StaticResource ImagePath}}"
                                                   Width="60" Height="60"
                                                   Stretch="UniformToFill"
                                                   Margin="0,0,15,0"
                                                   Visibility="{Binding ImagePath, Converter={StaticResource ImagePath}, ConverterParameter=visibility}"/>

                                            <!-- Item Name -->
                                            <TextBlock Grid.Column="1"
                                                       Text="{Binding Name}"
                                                       FontSize="28"
                                                       Foreground="White"
                                                       VerticalAlignment="Center"/>

                                            <!-- Price -->
                                            <TextBlock Grid.Column="2"
                                                       Text="{Binding Price, Converter={StaticResource CentsToDollars}}"
                                                       FontSize="28"
                                                       Foreground="White"
                                                       VerticalAlignment="Center"
                                                       FontWeight="SemiBold"/>
                                        </Grid>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </Grid>
</Window>
```

- [ ] **Step 3: Create DisplayWindow code-behind**

`src/MenuBoard/Views/DisplayWindow.xaml.cs`:
```csharp
using System.Windows;
using System.Windows.Media;
using MenuBoard.ViewModels;

namespace MenuBoard.Views;

public partial class DisplayWindow : Window
{
    public DisplayWindow(DisplayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DisplayViewModel.Settings))
                ApplySettings();
        };
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (DataContext is DisplayViewModel vm)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(vm.Settings.BackgroundColor);
                RootGrid.Background = new SolidColorBrush(color);
            }
            catch
            {
                RootGrid.Background = Brushes.Black;
            }
        }
    }
}
```

- [ ] **Step 4: Build to verify compilation**

```bash
cd MenuBoard
dotnet build src/MenuBoard
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "feat: display window with list-style menu layout"
```

---

### Task 4: Admin window

The editor interface where the store operator manages categories, items, images, and display settings.

**Files:**
- Create: `MenuBoard/src/MenuBoard/ViewModels/AdminViewModel.cs`
- Create: `MenuBoard/src/MenuBoard/Views/AdminWindow.xaml`
- Create: `MenuBoard/src/MenuBoard/Views/AdminWindow.xaml.cs`

**Interfaces:**
- Consumes: `MenuDataService` (all CRUD methods), `ImageService.CopyImageToStore()`
- Produces:
  - `AdminViewModel`: `int SelectedScreen` (1 or 2), `ObservableCollection<Category> Categories`, `Category? SelectedCategory`, commands for all CRUD operations
  - `AdminWindow`: the full admin UI

- [ ] **Step 1: Implement AdminViewModel**

`src/MenuBoard/ViewModels/AdminViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MenuBoard.Models;
using MenuBoard.Services;

namespace MenuBoard.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly MenuDataService _dataService;
    private readonly ImageService _imageService;

    [ObservableProperty]
    private int _selectedScreen = 1;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private DisplaySettings _currentDisplaySettings = new();

    public AdminViewModel(MenuDataService dataService, ImageService imageService)
    {
        _dataService = dataService;
        _imageService = imageService;
        LoadCategories();
        LoadDisplaySettings();
    }

    partial void OnSelectedScreenChanged(int value)
    {
        LoadCategories();
        LoadDisplaySettings();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        OnPropertyChanged(nameof(SelectedCategory));
    }

    public void LoadCategories()
    {
        var cats = _dataService.GetAllCategoriesForScreen(SelectedScreen);
        Categories = new ObservableCollection<Category>(cats);
        SelectedCategory = Categories.FirstOrDefault();
    }

    private void LoadDisplaySettings()
    {
        CurrentDisplaySettings = _dataService.GetDisplaySettings(SelectedScreen);
    }

    [RelayCommand]
    private void AddCategory()
    {
        var category = _dataService.AddCategory("New Category", SelectedScreen);
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == category.Id);
    }

    [RelayCommand]
    private void DeleteCategory()
    {
        if (SelectedCategory is null) return;
        _dataService.DeleteCategory(SelectedCategory.Id);
        LoadCategories();
    }

    [RelayCommand]
    private void MoveCategoryUp()
    {
        if (SelectedCategory is null) return;
        var index = Categories.IndexOf(SelectedCategory);
        if (index <= 0) return;

        var prev = Categories[index - 1];
        var currentOrder = SelectedCategory.DisplayOrder;
        _dataService.ReorderCategory(SelectedCategory.Id, prev.DisplayOrder);
        _dataService.ReorderCategory(prev.Id, currentOrder);
        var selectedId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }

    [RelayCommand]
    private void MoveCategoryDown()
    {
        if (SelectedCategory is null) return;
        var index = Categories.IndexOf(SelectedCategory);
        if (index >= Categories.Count - 1) return;

        var next = Categories[index + 1];
        var currentOrder = SelectedCategory.DisplayOrder;
        _dataService.ReorderCategory(SelectedCategory.Id, next.DisplayOrder);
        _dataService.ReorderCategory(next.Id, currentOrder);
        var selectedId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }

    [RelayCommand]
    private void AddMenuItem()
    {
        if (SelectedCategory is null) return;
        _dataService.AddMenuItem(SelectedCategory.Id, "New Item", 0);
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == SelectedCategory.Id);
    }

    public void SaveMenuItem(MenuItem item)
    {
        _dataService.UpdateMenuItem(item);
    }

    public void DeleteMenuItem(int menuItemId)
    {
        _dataService.DeleteMenuItem(menuItemId);
        if (SelectedCategory is not null)
        {
            var catId = SelectedCategory.Id;
            LoadCategories();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
        }
    }

    public void MoveMenuItemUp(MenuItem item)
    {
        if (SelectedCategory is null) return;
        var items = SelectedCategory.Items.OrderBy(i => i.DisplayOrder).ToList();
        var index = items.IndexOf(item);
        if (index <= 0) return;

        var prev = items[index - 1];
        var currentOrder = item.DisplayOrder;
        _dataService.ReorderMenuItem(item.Id, prev.DisplayOrder);
        _dataService.ReorderMenuItem(prev.Id, currentOrder);
        var catId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
    }

    public void MoveMenuItemDown(MenuItem item)
    {
        if (SelectedCategory is null) return;
        var items = SelectedCategory.Items.OrderBy(i => i.DisplayOrder).ToList();
        var index = items.IndexOf(item);
        if (index >= items.Count - 1) return;

        var next = items[index + 1];
        var currentOrder = item.DisplayOrder;
        _dataService.ReorderMenuItem(item.Id, next.DisplayOrder);
        _dataService.ReorderMenuItem(next.Id, currentOrder);
        var catId = SelectedCategory.Id;
        LoadCategories();
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == catId);
    }

    public string? BrowseAndCopyImage(string sourcePath)
    {
        return _imageService.CopyImageToStore(sourcePath);
    }

    public void SaveCategoryName(Category category)
    {
        _dataService.UpdateCategory(category);
    }

    [RelayCommand]
    private void SaveDisplaySettings()
    {
        _dataService.UpdateDisplaySettings(CurrentDisplaySettings);
    }
}
```

- [ ] **Step 2: Create AdminWindow XAML**

`src/MenuBoard/Views/AdminWindow.xaml`:
```xml
<Window x:Class="MenuBoard.Views.AdminWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:converters="clr-namespace:MenuBoard.Converters"
        Title="Menu Board Admin"
        Width="1100" Height="700"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    <Window.Resources>
        <converters:CentsToDollarsConverter x:Key="CentsToDollars"/>
    </Window.Resources>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="260"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Left Sidebar -->
        <Border Grid.Column="0" Background="#2D2D2D" Padding="10">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top" Text="Menu Board Admin"
                           Foreground="White" FontSize="18" FontWeight="Bold"
                           Margin="5,5,5,15"/>

                <!-- Screen Toggle -->
                <StackPanel DockPanel.Dock="Top" Margin="0,0,0,15">
                    <RadioButton x:Name="Screen1Radio" Content="TV 1 - Hot Food"
                                 Foreground="White" FontSize="14"
                                 IsChecked="True"
                                 Checked="Screen1Radio_Checked"
                                 Margin="5,3" GroupName="Screen"/>
                    <RadioButton x:Name="Screen2Radio" Content="TV 2 - Drinks/Snacks"
                                 Foreground="White" FontSize="14"
                                 Checked="Screen2Radio_Checked"
                                 Margin="5,3" GroupName="Screen"/>
                </StackPanel>

                <Separator DockPanel.Dock="Top" Margin="0,0,0,10"/>

                <!-- Category Controls -->
                <StackPanel DockPanel.Dock="Bottom" Margin="0,5,0,0">
                    <Button Content="+ Add Category" Command="{Binding AddCategoryCommand}"
                            Margin="5,3" Padding="8,5" FontSize="13"/>
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,5">
                        <Button Content="Up" Command="{Binding MoveCategoryUpCommand}"
                                Width="50" Margin="3" Padding="5,3"/>
                        <Button Content="Down" Command="{Binding MoveCategoryDownCommand}"
                                Width="50" Margin="3" Padding="5,3"/>
                        <Button Content="Delete" Command="{Binding DeleteCategoryCommand}"
                                Width="60" Margin="3" Padding="5,3" Foreground="Red"/>
                    </StackPanel>
                </StackPanel>

                <!-- Category List -->
                <ListBox ItemsSource="{Binding Categories}"
                         SelectedItem="{Binding SelectedCategory}"
                         Background="Transparent"
                         Foreground="White"
                         FontSize="14"
                         BorderThickness="0"
                         Margin="5">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding Name}" Margin="5,4"/>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </DockPanel>
        </Border>

        <!-- Right Content Area -->
        <ScrollViewer Grid.Column="1" Padding="20">
            <StackPanel>
                <!-- Display Settings -->
                <Expander Header="Display Settings" IsExpanded="False" Margin="0,0,0,20">
                    <StackPanel Margin="10">
                        <StackPanel Orientation="Horizontal" Margin="0,5">
                            <TextBlock Text="Header Text:" Width="120" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding CurrentDisplaySettings.HeaderText, UpdateSourceTrigger=PropertyChanged}"
                                     Width="300" Padding="5,3"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Margin="0,5">
                            <TextBlock Text="Background Color:" Width="120" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding CurrentDisplaySettings.BackgroundColor, UpdateSourceTrigger=PropertyChanged}"
                                     Width="100" Padding="5,3"/>
                        </StackPanel>
                        <StackPanel Orientation="Horizontal" Margin="0,5">
                            <TextBlock Text="Font Scale:" Width="120" VerticalAlignment="Center"/>
                            <Slider Value="{Binding CurrentDisplaySettings.FontScale}"
                                    Minimum="0.5" Maximum="2.0" Width="200"
                                    TickFrequency="0.1" IsSnapToTickEnabled="True"/>
                            <TextBlock Text="{Binding CurrentDisplaySettings.FontScale, StringFormat={}{0:F1}x}"
                                       Margin="10,0,0,0" VerticalAlignment="Center"/>
                        </StackPanel>
                        <Button Content="Apply Settings" Command="{Binding SaveDisplaySettingsCommand}"
                                Width="120" Margin="0,10,0,0" HorizontalAlignment="Left" Padding="8,5"/>
                    </StackPanel>
                </Expander>

                <!-- Selected Category Content -->
                <StackPanel Visibility="{Binding SelectedCategory, Converter={StaticResource NullToCollapsed}, FallbackValue=Collapsed}">
                    <!-- Category Name -->
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,15">
                        <TextBlock Text="Category:" FontSize="16" FontWeight="Bold"
                                   VerticalAlignment="Center" Margin="0,0,10,0"/>
                        <TextBox x:Name="CategoryNameBox"
                                 Text="{Binding SelectedCategory.Name, UpdateSourceTrigger=PropertyChanged}"
                                 FontSize="16" Width="300" Padding="5,3"
                                 LostFocus="CategoryNameBox_LostFocus"/>
                    </StackPanel>

                    <!-- Add Item Button -->
                    <Button Content="+ Add Item" Command="{Binding AddMenuItemCommand}"
                            HorizontalAlignment="Left" Padding="10,5" Margin="0,0,0,15"/>

                    <!-- Items List -->
                    <ItemsControl ItemsSource="{Binding SelectedCategory.Items}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border BorderBrush="#DDD" BorderThickness="0,0,0,1"
                                        Padding="5,8" Margin="0,2">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="100"/>
                                            <ColumnDefinition Width="200"/>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>

                                        <TextBox Grid.Column="0" Text="{Binding Name, UpdateSourceTrigger=LostFocus}"
                                                 Padding="5,3" Margin="0,0,5,0"
                                                 LostFocus="ItemField_LostFocus" Tag="{Binding}"/>
                                        <TextBox Grid.Column="1" Text="{Binding Price, Converter={StaticResource CentsToDollars}, UpdateSourceTrigger=LostFocus}"
                                                 Padding="5,3" Margin="0,0,5,0"
                                                 LostFocus="ItemField_LostFocus" Tag="{Binding}"/>
                                        <TextBox Grid.Column="2" Text="{Binding Description, UpdateSourceTrigger=LostFocus}"
                                                 Padding="5,3" Margin="0,0,5,0"
                                                 LostFocus="ItemField_LostFocus" Tag="{Binding}"/>
                                        <Button Grid.Column="3" Content="Image"
                                                Click="BrowseImage_Click" Tag="{Binding}"
                                                Padding="5,3" Margin="0,0,5,0"/>
                                        <CheckBox Grid.Column="4" IsChecked="{Binding IsAvailable}"
                                                  Content="Available" VerticalAlignment="Center"
                                                  Margin="0,0,10,0"
                                                  Click="AvailableToggle_Click" Tag="{Binding}"/>
                                        <StackPanel Grid.Column="5" Orientation="Horizontal">
                                            <Button Content="^" Click="MoveItemUp_Click" Tag="{Binding}"
                                                    Width="25" Margin="2,0" Padding="2"/>
                                            <Button Content="v" Click="MoveItemDown_Click" Tag="{Binding}"
                                                    Width="25" Margin="2,0" Padding="2"/>
                                            <Button Content="X" Click="DeleteItem_Click" Tag="{Binding}"
                                                    Width="25" Margin="2,0" Padding="2" Foreground="Red"/>
                                        </StackPanel>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</Window>
```

- [ ] **Step 3: Create AdminWindow code-behind**

`src/MenuBoard/Views/AdminWindow.xaml.cs`:
```csharp
using System.Windows;
using System.Windows.Controls;
using MenuBoard.Models;
using MenuBoard.ViewModels;
using Microsoft.Win32;

namespace MenuBoard.Views;

public partial class AdminWindow : Window
{
    private AdminViewModel ViewModel => (AdminViewModel)DataContext;

    public AdminWindow(AdminViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Screen1Radio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SelectedScreen = 1;
    }

    private void Screen2Radio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
            vm.SelectedScreen = 2;
    }

    private void CategoryNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedCategory is not null)
            ViewModel.SaveCategoryName(ViewModel.SelectedCategory);
    }

    private void ItemField_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.SaveMenuItem(item);
    }

    private void AvailableToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.SaveMenuItem(item);
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select Item Image"
            };
            if (dialog.ShowDialog() == true)
            {
                var relativePath = ViewModel.BrowseAndCopyImage(dialog.FileName);
                if (relativePath is not null)
                {
                    item.ImagePath = relativePath;
                    ViewModel.SaveMenuItem(item);
                }
            }
        }
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.DeleteMenuItem(item.Id);
    }

    private void MoveItemUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.MoveMenuItemUp(item);
    }

    private void MoveItemDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is MenuItem item)
            ViewModel.MoveMenuItemDown(item);
    }
}
```

- [ ] **Step 4: Add NullToCollapsedConverter**

This converter is needed by the admin XAML to hide the item editor when no category is selected.

`src/MenuBoard/Converters/NullToCollapsedConverter.cs`:
```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MenuBoard.Converters;

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

Register it in `AdminWindow.xaml` resources (add alongside the existing CentsToDollars converter):
```xml
<converters:NullToCollapsedConverter x:Key="NullToCollapsed"/>
```

- [ ] **Step 5: Build to verify compilation**

```bash
cd MenuBoard
dotnet build src/MenuBoard
```
Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "feat: admin window with category and item management"
```

---

### Task 5: App startup and monitor wiring

Wire everything together in `App.xaml.cs` - create the database, instantiate services, open admin on primary monitor, open display windows on secondary/tertiary monitors.

**Files:**
- Modify: `MenuBoard/src/MenuBoard/App.xaml`
- Modify: `MenuBoard/src/MenuBoard/App.xaml.cs`

**Interfaces:**
- Consumes: `MenuDbContext`, `MenuDataService`, `ImageService`, `MonitorService`, `AdminViewModel`, `DisplayViewModel`, `AdminWindow`, `DisplayWindow`
- Produces: complete running application

- [ ] **Step 1: Update App.xaml to remove default StartupUri**

`src/MenuBoard/App.xaml`:
```xml
<Application x:Class="MenuBoard.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources/>
</Application>
```

- [ ] **Step 2: Implement App.xaml.cs startup**

`src/MenuBoard/App.xaml.cs`:
```csharp
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MenuBoard.Data;
using MenuBoard.Services;
using MenuBoard.ViewModels;
using MenuBoard.Views;

namespace MenuBoard;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "menuboard.db");
        var options = new DbContextOptionsBuilder<MenuDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var context = new MenuDbContext(options);
        context.Database.EnsureCreated();
        context.Seed();

        var dataService = new MenuDataService(context);
        var imageService = new ImageService();
        var monitorService = new MonitorService();
        var monitors = monitorService.GetMonitors();

        var adminVm = new AdminViewModel(dataService, imageService);
        var adminWindow = new AdminWindow(adminVm);
        adminWindow.Show();

        var nonPrimary = monitors.Where(m => !m.IsPrimary).ToList();

        if (nonPrimary.Count >= 1)
        {
            var displayVm1 = new DisplayViewModel(dataService, 1);
            var display1 = new DisplayWindow(displayVm1);
            var bounds1 = nonPrimary[0].Bounds;
            display1.Left = bounds1.Left;
            display1.Top = bounds1.Top;
            display1.Width = bounds1.Width;
            display1.Height = bounds1.Height;
            display1.Show();
        }

        if (nonPrimary.Count >= 2)
        {
            var displayVm2 = new DisplayViewModel(dataService, 2);
            var display2 = new DisplayWindow(displayVm2);
            var bounds2 = nonPrimary[1].Bounds;
            display2.Left = bounds2.Left;
            display2.Top = bounds2.Top;
            display2.Width = bounds2.Width;
            display2.Height = bounds2.Height;
            display2.Show();
        }

        if (nonPrimary.Count < 2)
        {
            var missing = 2 - nonPrimary.Count;
            MessageBox.Show(
                $"{missing} display monitor(s) not detected. Display windows will open when monitors are connected.",
                "Menu Board",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
```

- [ ] **Step 3: Build the full solution**

```bash
cd MenuBoard
dotnet build
```
Expected: build succeeds.

- [ ] **Step 4: Run all tests**

```bash
cd MenuBoard
dotnet test --verbosity normal
```
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "feat: app startup with monitor detection and window placement"
```

---

### Task 6: Visual polish and dot leaders

Refine the display window to match the spec's list-style layout with dot leaders between item names and prices, proper font scaling from display settings, and high-contrast readability.

**Files:**
- Modify: `MenuBoard/src/MenuBoard/Views/DisplayWindow.xaml`
- Modify: `MenuBoard/src/MenuBoard/Views/DisplayWindow.xaml.cs`

**Interfaces:**
- Consumes: `DisplayViewModel.Settings.FontScale`
- Produces: polished display that matches the spec's visual layout

- [ ] **Step 1: Update DisplayWindow XAML with dot leaders and font scaling**

Replace the menu item DataTemplate's Grid with a version that uses a dotted line between name and price:

```xml
<DataTemplate>
    <Grid Margin="10,6">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Optional Image -->
        <Border Grid.Column="0" Width="60" Height="60" Margin="0,0,15,0"
                Visibility="{Binding ImagePath, TargetNullValue=Collapsed, FallbackValue=Visible}">
            <Image Source="{Binding ImagePath, Converter={StaticResource ImagePath}}"
                   Stretch="UniformToFill"/>
        </Border>

        <!-- Item Name -->
        <TextBlock Grid.Column="1"
                   Text="{Binding Name}"
                   FontSize="28"
                   Foreground="White"
                   VerticalAlignment="Center"/>

        <!-- Dot Leader -->
        <TextBlock Grid.Column="2"
                   VerticalAlignment="Center"
                   Foreground="#666"
                   FontSize="20"
                   Margin="8,0"
                   Text="  . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . ."
                   TextTrimming="CharacterEllipsis"/>

        <!-- Price -->
        <TextBlock Grid.Column="3"
                   Text="{Binding Price, Converter={StaticResource CentsToDollars}}"
                   FontSize="28"
                   Foreground="White"
                   VerticalAlignment="Center"
                   FontWeight="SemiBold"/>
    </Grid>
</DataTemplate>
```

- [ ] **Step 2: Add font scale support to DisplayWindow code-behind**

Add to `ApplySettings()` in `DisplayWindow.xaml.cs`:
```csharp
private void ApplySettings()
{
    if (DataContext is DisplayViewModel vm)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(vm.Settings.BackgroundColor);
            RootGrid.Background = new SolidColorBrush(color);
        }
        catch
        {
            RootGrid.Background = Brushes.Black;
        }

        RootGrid.LayoutTransform = new ScaleTransform(vm.Settings.FontScale, vm.Settings.FontScale);
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
cd MenuBoard
dotnet build
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "feat: dot leaders and font scaling on display windows"
```

---

### Task 7: End-to-end verification

Run the full test suite, build a release, and verify the app launches correctly.

**Files:** none new

**Interfaces:**
- Consumes: entire application
- Produces: verified working build

- [ ] **Step 1: Run full test suite**

```bash
cd MenuBoard
dotnet test --verbosity normal
```
Expected: all tests pass.

- [ ] **Step 2: Build release**

```bash
cd MenuBoard
dotnet publish src/MenuBoard -c Release -o publish
```
Expected: successful publish to `publish/` directory.

- [ ] **Step 3: Verify Images directory is created**

```bash
ls MenuBoard/publish/
```
Expected: `MenuBoard.exe`, `menuboard.db` (created on first run), `Images/` directory.

- [ ] **Step 4: Commit**

```bash
cd MenuBoard
git add -A
git commit -m "chore: verify full build and test suite"
```
