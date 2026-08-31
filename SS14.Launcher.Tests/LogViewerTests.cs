using System;
using System.IO;
using NUnit.Framework;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class LogViewerTests
{
    private string _tempLogDir = "";
    private string _testLogPath = "";

    [SetUp]
    public void SetUp()
    {
        _tempLogDir = Path.Combine(Path.GetTempPath(), "SS14_Launcher_LogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempLogDir);
        _testLogPath = Path.Combine(_tempLogDir, "test.log");

        File.WriteAllLines(_testLogPath, new[]
        {
            "[12:00:00 INF] Launcher starting up",
            "[12:00:01 dbg] Initializing graphics backend",
            "[12:00:02 WRN] Slow network response from hub",
            "[12:00:03 ERR] Failed to connect to socket",
            "[12:00:04 fatal] Unhandled crash exception occurred",
            "[12:00:05 vrb] Verbose message trace",
            "[12:00:06 INF] Game launched successfully"
        });
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempLogDir))
                Directory.Delete(_tempLogDir, true);
        }
        catch { }
    }

    [Test]
    public void TestLogFilterModes()
    {
        var vm = new LogViewerViewModel();
        // Set lines manually for testing
        vm.AvailableFiles.Add("test.log");

        // Test filter mode changes
        vm.SetFilterAll();
        Assert.That(vm.IsFilterAll, Is.True);
        Assert.That(vm.IsFilterErrors, Is.False);

        vm.SetFilterErrors();
        Assert.That(vm.IsFilterErrors, Is.True);
        Assert.That(vm.IsFilterAll, Is.False);

        vm.SetFilterWarnings();
        Assert.That(vm.IsFilterWarnings, Is.True);

        vm.SetFilterInfo();
        Assert.That(vm.IsFilterInfo, Is.True);

        vm.SetFilterDebug();
        Assert.That(vm.IsFilterDebug, Is.True);
    }
}
