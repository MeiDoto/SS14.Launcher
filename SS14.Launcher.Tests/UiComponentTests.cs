using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using NUnit.Framework;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility.Network;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class UiComponentTests
{
    private string _tempDir = "";

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ss14_ui_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    [Test]
    public void TestDiagnosticsStepViewModelStatusAndBadges()
    {
        var step = new DiagnosticsStep("Network Ping");
        var vm = new DiagnosticsStepViewModel(step);

        Assert.That(vm.Name, Is.EqualTo("Network Ping"));
        Assert.That(vm.StatusBadge, Is.EqualTo("⚪"));
        Assert.That(vm.StatusColor, Is.EqualTo("#888888"));
        Assert.That(vm.ElapsedFormatted, Is.EqualTo(""));

        // Running
        step.Status = DiagnosticsStepStatus.Running;
        step.ElapsedMilliseconds = 15;
        step.Details = "Pinging host...";
        vm.UpdateFromModel();

        Assert.That(vm.Status, Is.EqualTo(DiagnosticsStepStatus.Running));
        Assert.That(vm.StatusBadge, Is.EqualTo("⏳"));
        Assert.That(vm.StatusColor, Is.EqualTo("#2196F3"));
        Assert.That(vm.ElapsedFormatted, Is.EqualTo("15 ms"));
        Assert.That(vm.Details, Is.EqualTo("Pinging host..."));

        // Success
        step.Status = DiagnosticsStepStatus.Success;
        vm.UpdateFromModel();
        Assert.That(vm.StatusBadge, Is.EqualTo("✅"));
        Assert.That(vm.StatusColor, Is.EqualTo("#4CAF50"));

        // Warning
        step.Status = DiagnosticsStepStatus.Warning;
        vm.UpdateFromModel();
        Assert.That(vm.StatusBadge, Is.EqualTo("⚠️"));
        Assert.That(vm.StatusColor, Is.EqualTo("#FFC107"));

        // Failed
        step.Status = DiagnosticsStepStatus.Failed;
        vm.UpdateFromModel();
        Assert.That(vm.StatusBadge, Is.EqualTo("❌"));
        Assert.That(vm.StatusColor, Is.EqualTo("#F44336"));
    }

    [Test]
    public void TestNetworkDiagnosticsViewModelTargetAndState()
    {
        var vm = new NetworkDiagnosticsViewModel("ss14s://custom.server.org:1212");

        Assert.That(vm.TargetAddress, Is.EqualTo("ss14s://custom.server.org:1212"));
        Assert.That(vm.IsRunning, Is.False);
        Assert.That(vm.CanRun, Is.True);
        Assert.That(vm.HasReport, Is.False);

        // Blank target disables CanRun
        vm.TargetAddress = "";
        Assert.That(vm.CanRun, Is.False);

        vm.TargetAddress = "   ";
        Assert.That(vm.CanRun, Is.False);

        vm.TargetAddress = "ss14://game.example.com";
        Assert.That(vm.CanRun, Is.True);

        // Cancel test should not throw
        Assert.DoesNotThrow(() => vm.Cancel());
    }

    [Test]
    public void TestReplayDetailsViewModelFileInfoAndPlayAction()
    {
        var dummyZipPath = Path.Combine(_tempDir, "test_replay_round_42.zip");

        // Create a synthetic replay zip
        using (var zipStream = new FileStream(dummyZipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            var metaEntry = archive.CreateEntry("replay_final.txt");
            using var writer = new StreamWriter(metaEntry.Open());
            writer.WriteLine("server: Test Station 14");
            writer.WriteLine("map: Delta");
            writer.WriteLine("gamemode: Secret");
            writer.WriteLine("round_id: 12345");
            writer.WriteLine("duration: 01:25:30");
        }

        bool played = false;
        var vm = new ReplayDetailsViewModel(dummyZipPath, () => played = true);

        Assert.That(vm.FilePath, Is.EqualTo(dummyZipPath));
        Assert.That(vm.FileName, Is.EqualTo("test_replay_round_42.zip"));
        Assert.That(vm.Title, Is.EqualTo("test_replay_round_42"));
        Assert.That(vm.FileSizeFormatted, Does.Contain("B"));

        vm.Play();
        Assert.That(played, Is.True);
    }

    [Test]
    public void TestEngineStorageItemFormatting()
    {
        var date = new DateTime(2026, 9, 8, 15, 30, 0, DateTimeKind.Utc);
        var item = new EngineStorageItem("10.0.1", 104857600L, date);

        Assert.That(item.Version, Is.EqualTo("10.0.1"));
        Assert.That(item.SizeBytes, Is.EqualTo(104857600L));
        Assert.That(item.FormattedSize, Is.EqualTo("100.0 MB"));
        Assert.That(item.FormattedDate, Is.EqualTo(date.ToString("yyyy-MM-dd HH:mm")));
    }

    [Test]
    public void TestStorageManagerViewModelSelectionState()
    {
        var vm = new StorageManagerViewModel();

        Assert.That(vm.HasSelectedEngine, Is.False);
        Assert.That(vm.SelectedEngine, Is.Null);

        var testEngine = new EngineStorageItem("10.0.2", 52428800L, DateTime.UtcNow);
        vm.SelectedEngine = testEngine;

        Assert.That(vm.HasSelectedEngine, Is.True);
        Assert.That(vm.SelectedEngine?.Version, Is.EqualTo("10.0.2"));

        vm.SelectedEngine = null;
        Assert.That(vm.HasSelectedEngine, Is.False);
    }
}
