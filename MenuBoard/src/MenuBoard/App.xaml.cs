using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
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

        var adminVm = new AdminViewModel(dataService, imageService);
        var adminWindow = new AdminWindow(adminVm);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = adminWindow;
        adminWindow.Show();

        var monitorService = new MonitorService();
        var monitors = monitorService.GetMonitors();
        var nonPrimary = monitors.Where(m => !m.IsPrimary).ToList();

        var displayVm1 = new DisplayViewModel(dataService, 1);
        var display1 = new DisplayWindow(displayVm1);

        var displayVm2 = new DisplayViewModel(dataService, 2);
        var display2 = new DisplayWindow(displayVm2);

        if (nonPrimary.Count >= 1)
        {
            PlaceOnMonitor(display1, nonPrimary[0].Bounds);
        }
        else
        {
            OpenAsPreview(display1, "TV 1 Preview - Hot Food");
        }

        if (nonPrimary.Count >= 2)
        {
            PlaceOnMonitor(display2, nonPrimary[1].Bounds);
        }
        else
        {
            OpenAsPreview(display2, "TV 2 Preview - Drinks/Snacks");
        }

        display1.Show();
        display2.Show();
    }

    private static void PlaceOnMonitor(DisplayWindow window, Rect bounds)
    {
        window.Left = bounds.Left;
        window.Top = bounds.Top;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
    }

    private static void OpenAsPreview(DisplayWindow window, string title)
    {
        window.WindowStyle = WindowStyle.SingleBorderWindow;
        window.Topmost = false;
        window.ShowInTaskbar = true;
        window.ResizeMode = ResizeMode.CanResize;
        window.Title = title;
        window.Width = 960;
        window.Height = 540;
    }
}
