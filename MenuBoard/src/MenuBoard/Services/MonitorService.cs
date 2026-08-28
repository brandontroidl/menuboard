using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace MenuBoard.Services;

public class MonitorInfo
{
    public Rect Bounds { get; init; }
    public bool IsPrimary { get; init; }

    /// <summary>True for a built-in laptop panel (internal/eDP/LVDS connector).</summary>
    public bool IsInternal { get; init; }
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
        var internalDeviceNames = GetInternalDisplayDeviceNames();
        var screens = System.Windows.Forms.Screen.AllScreens;
        return screens.Select(s => new MonitorInfo
        {
            Bounds = new Rect(
                s.Bounds.X / dpiScale,
                s.Bounds.Y / dpiScale,
                s.Bounds.Width / dpiScale,
                s.Bounds.Height / dpiScale),
            IsPrimary = s.Primary,
            IsInternal = internalDeviceNames.Contains(s.DeviceName)
        }).ToList();
    }

    private static double GetDpiScale()
    {
        try
        {
            // This can run before any window exists (the admin ViewModel
            // asks for the monitor list while it is being constructed).
            // MainWindow is null then, and PresentationSource.FromVisual
            // throws on null - so guard everything and fall back to 1.0.
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow is null)
                return 1.0;

            var source = PresentationSource.FromVisual(mainWindow);
            if (source?.CompositionTarget != null)
                return source.CompositionTarget.TransformToDevice.M11;
        }
        catch
        {
            // Any DPI probing failure must never take the app down.
        }
        return 1.0;
    }

    // --- Built-in display detection --------------------------------------
    // A splitter/MST hub can shuffle which output Windows calls "primary",
    // so "admin goes to the primary monitor" is not reliable. Instead, ask
    // the display configuration API which active paths use an internal
    // connector (laptop panel: INTERNAL, embedded DisplayPort, LVDS, UDI
    // embedded) and map them to their GDI device names (\\.\DISPLAY1 ...),
    // which is what WinForms Screen.DeviceName reports.

    private const uint QDC_ONLY_ACTIVE_PATHS = 2;
    private const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;

    private static readonly uint[] InternalOutputTechnologies =
    {
        6,          // LVDS
        11,         // DISPLAYPORT_EMBEDDED (eDP)
        13,         // UDI_EMBEDDED
        0x80000000  // INTERNAL
    };

    private static HashSet<string> GetInternalDisplayDeviceNames()
    {
        var result = new HashSet<string>();
        try
        {
            if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount) != 0)
                return result;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
                return result;

            for (var i = 0; i < pathCount; i++)
            {
                if (!InternalOutputTechnologies.Contains(paths[i].targetInfo.outputTechnology))
                    continue;

                var request = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        adapterId = paths[i].sourceInfo.adapterId,
                        id = paths[i].sourceInfo.id
                    }
                };
                if (DisplayConfigGetDeviceInfo(ref request) == 0 && !string.IsNullOrEmpty(request.viewGdiDeviceName))
                    result.Add(request.viewGdiDeviceName);
            }
        }
        catch
        {
            // Detection is best-effort; callers fall back to the primary
            // monitor when no internal panel is identified.
        }
        return result;
    }

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public uint refreshRateNumerator;
        public uint refreshRateDenominator;
        public uint scanLineOrdering;
        public uint targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    // Union in the native API; contents unused here - only the size matters
    // so QueryDisplayConfig can fill its array.
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }
}
