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
