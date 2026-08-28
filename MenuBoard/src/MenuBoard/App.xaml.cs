using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
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

        var adminVm = new AdminViewModel(dataService, imageService, _settingsService, new StartupService(), _monitorService);
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

        var settings = _settingsService.Settings;
        var monitors = _monitorService.GetMonitorsSorted();

        // Explicit per-TV picks win (from App Settings). Out-of-range picks
        // (e.g., a monitor was unplugged) fall back to automatic.
        var tv1 = settings.Tv1Monitor >= 0 && settings.Tv1Monitor < monitors.Count
            ? monitors[settings.Tv1Monitor] : null;
        var tv2 = settings.Tv2Monitor >= 0 && settings.Tv2Monitor < monitors.Count
            ? monitors[settings.Tv2Monitor] : null;

        // Automatic pool for any TV without an explicit pick. "Auto" with
        // exactly 2 monitors assumes a store PC hooked up to just the two
        // TVs - use both for menus. With 3+ monitors, keep the primary for
        // the admin editor.
        var useAllMonitors = settings.MonitorLayout == MonitorLayout.AllMonitors
            || (settings.MonitorLayout == MonitorLayout.Auto && monitors.Count == 2);

        var pool = (useAllMonitors ? monitors : monitors.Where(m => !m.IsPrimary)).ToList();
        if (settings.SwapDisplays)
            pool.Reverse();
        pool.RemoveAll(m => m == tv1 || m == tv2);

        if (tv1 is null && pool.Count > 0)
        {
            tv1 = pool[0];
            pool.RemoveAt(0);
        }
        if (tv2 is null && pool.Count > 0)
        {
            tv2 = pool[0];
        }

        AssignMonitor(_display1, tv1, "TV 1 Preview - Hot Food");
        AssignMonitor(_display2, tv2, "TV 2 Preview - Drinks/Snacks");
        MoveAdminOffTvMonitors(monitors, tv1, tv2);
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

    /// <summary>
    /// If a monitor is left over for the admin "console" and the admin
    /// window currently sits on a monitor covered by a fullscreen TV, move
    /// it to the free monitor so the console screen actually shows it.
    /// </summary>
    private void MoveAdminOffTvMonitors(List<MonitorInfo> monitors, MonitorInfo? tv1, MonitorInfo? tv2)
    {
        if (_adminWindow is null)
            return;

        var free = monitors.FirstOrDefault(m => m != tv1 && m != tv2);
        if (free is null)
            return;

        var adminCenter = new Point(
            _adminWindow.Left + _adminWindow.Width / 2,
            _adminWindow.Top + _adminWindow.Height / 2);

        var coveredByTv = (tv1?.Bounds.Contains(adminCenter) ?? false)
            || (tv2?.Bounds.Contains(adminCenter) ?? false);
        if (!coveredByTv)
            return;

        _adminWindow.WindowState = WindowState.Normal;
        _adminWindow.Left = free.Bounds.Left + Math.Max(0, (free.Bounds.Width - _adminWindow.Width) / 2);
        _adminWindow.Top = free.Bounds.Top + Math.Max(0, (free.Bounds.Height - _adminWindow.Height) / 2);
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
