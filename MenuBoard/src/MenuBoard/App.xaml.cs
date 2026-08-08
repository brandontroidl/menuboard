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
