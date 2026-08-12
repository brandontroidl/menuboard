using System.IO;
using MenuBoard.Services;

namespace MenuBoard.Tests.Services;

[TestClass]
public class AppSettingsServiceTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [TestMethod]
    public void NoFile_ReturnsDefaults()
    {
        var service = new AppSettingsService(_tempDir);

        Assert.AreEqual(MonitorLayout.Auto, service.Settings.MonitorLayout);
        Assert.IsFalse(service.Settings.SwapDisplays);
    }

    [TestMethod]
    public void SaveAndReload_RoundTrips()
    {
        var service = new AppSettingsService(_tempDir);
        service.Settings.MonitorLayout = MonitorLayout.AllMonitors;
        service.Settings.SwapDisplays = true;
        service.Save();

        var reloaded = new AppSettingsService(_tempDir);

        Assert.AreEqual(MonitorLayout.AllMonitors, reloaded.Settings.MonitorLayout);
        Assert.IsTrue(reloaded.Settings.SwapDisplays);
    }

    [TestMethod]
    public void CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "{not valid json!!");

        var service = new AppSettingsService(_tempDir);

        Assert.AreEqual(MonitorLayout.Auto, service.Settings.MonitorLayout);
        Assert.IsFalse(service.Settings.SwapDisplays);
    }
}
