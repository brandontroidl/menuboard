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

    /// <summary>
    /// Untracked copy of the display settings for read-only consumers.
    /// The display windows must NOT use <see cref="GetDisplaySettings"/>:
    /// EF returns the same tracked instance every call, so a ViewModel
    /// property assignment sees an identical reference and never raises
    /// PropertyChanged - theme changes (background, header, font scale)
    /// would silently stop propagating to the TVs.
    /// </summary>
    public DisplaySettings GetDisplaySettingsSnapshot(int screenNumber)
    {
        return _context.DisplaySettings.AsNoTracking().First(d => d.ScreenNumber == screenNumber);
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
        var entry = _context.Entry(item);
        if (entry.State == EntityState.Detached || entry.State == EntityState.Deleted)
            return;
        _context.SaveChanges();
        DataChanged?.Invoke();
    }

    public void DeleteMenuItem(int menuItemId)
    {
        var item = _context.MenuItems.Find(menuItemId);
        if (item is null)
            return;

        var entry = _context.Entry(item);
        if (entry.State == EntityState.Deleted)
            return;

        _context.MenuItems.Remove(item);
        _context.SaveChanges();
        DataChanged?.Invoke();
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
