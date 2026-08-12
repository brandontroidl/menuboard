using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using Microsoft.EntityFrameworkCore;
using MenuBoard.Data;
using MenuBoard.Services;
using MenuBoard.ViewModels;
using MenuBoard.Views;

namespace MenuBoard;

public partial class App : Application
{
    private AdminWindow? _adminWindow;
    private DisplayWindow? _display1;
    private DisplayWindow? _display2;
    private MonitorService _monitorService = null!;
    private AppSettingsService _settingsService = null!;

    public static new App Current => (App)Application.Current;

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
        _settingsService = new AppSettingsService();
        _monitorService = new MonitorService();

        var adminVm = new AdminViewModel(dataService, imageService, _settingsService, new StartupService());
        adminVm.MonitorSettingsChanged += ApplyMonitorLayout;
        _adminWindow = new AdminWindow(adminVm);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = _adminWindow;
        _adminWindow.Show();

        _display1 = new DisplayWindow(new DisplayViewModel(dataService, 1));
        _display2 = new DisplayWindow(new DisplayViewModel(dataService, 2));

        ApplyMonitorLayout();

        _display1.Show();
        _display2.Show();
    }

    /// <summary>
    /// Positions the two TV display windows according to the configured
    /// monitor layout. Safe to call again at runtime when settings change.
    /// </summary>
    public void ApplyMonitorLayout()
    {
        if (_display1 is null || _display2 is null)
            return;

        var monitors = _monitorService.GetMonitors();
        var layout = _settingsService.Settings.MonitorLayout;

        // "Auto" with exactly 2 monitors assumes a store PC hooked up to just
        // the two TVs - use both for menus. With 3+ monitors, keep the
        // primary for the admin editor.
        var useAllMonitors = layout == MonitorLayout.AllMonitors
            || (layout == MonitorLayout.Auto && monitors.Count == 2);

        var targets = (useAllMonitors ? monitors : monitors.Where(m => !m.IsPrimary))
            .OrderBy(m => m.Bounds.Left)
            .ThenBy(m => m.Bounds.Top)
            .ToList();

        if (_settingsService.Settings.SwapDisplays)
            targets.Reverse();

        AssignMonitor(_display1, targets.ElementAtOrDefault(0), "TV 1 Preview - Hot Food");
        AssignMonitor(_display2, targets.ElementAtOrDefault(1), "TV 2 Preview - Drinks/Snacks");
    }

    /// <summary>
    /// Restores and focuses the admin window. Used by the display windows so
    /// the operator can get back to editing when fullscreen menus cover
    /// every monitor (Esc or double-click on a display).
    /// </summary>
    public void ActivateAdmin()
    {
        if (_adminWindow is null)
            return;

        if (_adminWindow.WindowState == WindowState.Minimized)
            _adminWindow.WindowState = WindowState.Normal;

        _adminWindow.Show();
        _adminWindow.Activate();
        // Nudge above the borderless fullscreen displays, then release so
        // dialogs behave normally.
        _adminWindow.Topmost = true;
        _adminWindow.Topmost = false;
        _adminWindow.Focus();
    }

    private static void AssignMonitor(DisplayWindow window, MonitorInfo? monitor, string previewTitle)
    {
        if (monitor is not null)
        {
            // Borderless fullscreen on the assigned monitor.
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;
            window.WindowState = WindowState.Normal;
            window.Left = monitor.Bounds.Left;
            window.Top = monitor.Bounds.Top;
            window.Width = monitor.Bounds.Width;
            window.Height = monitor.Bounds.Height;
        }
        else
        {
            // Not enough monitors - open as a movable preview window instead.
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.Topmost = false;
            window.ShowInTaskbar = true;
            window.ResizeMode = ResizeMode.CanResize;
            window.Title = previewTitle;
            window.WindowState = WindowState.Normal;
            window.Width = 960;
            window.Height = 540;
        }
    }
}
