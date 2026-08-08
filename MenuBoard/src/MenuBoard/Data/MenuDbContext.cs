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
