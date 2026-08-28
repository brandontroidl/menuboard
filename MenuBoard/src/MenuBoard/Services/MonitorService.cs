using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace MenuBoard.Services;

public class MonitorInfo
{
    public Rect Bounds { get; init; }
    public bool IsPrimary { get; init; }
}

public class MonitorService
{
    /// <summary>
    /// Monitors in a stable, user-predictable order: left-to-right, then
    /// top-to-bottom. "Monitor 1" in the admin UI is always the leftmost.
    /// </summary>
    public List<MonitorInfo> GetMonitorsSorted()
    {
        return GetMonitors()
            .OrderBy(m => m.Bounds.Left)
            .ThenBy(m => m.Bounds.Top)
            .ToList();
    }

    public List<MonitorInfo> GetMonitors()
    {
        var dpiScale = GetDpiScale();
        var screens = System.Windows.Forms.Screen.AllScreens;
        return screens.Select(s => new MonitorInfo
        {
            Bounds = new Rect(
                s.Bounds.X / dpiScale,
                s.Bounds.Y / dpiScale,
                s.Bounds.Width / dpiScale,
                s.Bounds.Height / dpiScale),
            IsPrimary = s.Primary
        }).ToList();
    }

    private static double GetDpiScale()
    {
        var source = PresentationSource.FromVisual(Application.Current.MainWindow);
        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformToDevice.M11;
        return 1.0;
    }
}
