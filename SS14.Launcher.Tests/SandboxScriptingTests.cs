#nullable enable

using System;
using NUnit.Framework;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class SandboxScriptingTests
{
    private DataManager? _dataManager;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // If not registered in Splat, register a dummy DataManager for testing
        if (Locator.Current.GetService<DataManager>() == null)
        {
            _dataManager = new DataManager();
            Locator.CurrentMutable.RegisterConstant(_dataManager);
        }
    }

    [Test]
    public void TestScripting_PresetExecution()
    {
        var vm = new LauncherCustomizerViewModel();

        vm.CustomUserCode = "preset cyberpunk";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomAccentColor, Is.EqualTo("#00F2FE"));

        vm.CustomUserCode = "preset syndicate";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomAccentColor, Is.EqualTo("#E50914"));

        vm.CustomUserCode = "preset classic";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomAccentColor, Is.EqualTo("#ADA24B"));
    }

    [Test]
    public void TestScripting_CustomProperties()
    {
        var vm = new LauncherCustomizerViewModel();

        vm.CustomUserCode = @"
            Accent = #AABBCC
            Button = #112233
            Font = 18
            Opacity = 0.75
            Tabs = Bottom
            VFX = off
        ";
        vm.ExecuteUserCode();

        Assert.That(vm.CustomAccentColor, Is.EqualTo("#AABBCC"));
        Assert.That(vm.CustomButtonColor, Is.EqualTo("#112233"));
        Assert.That(vm.CustomFontSize, Is.EqualTo(18.0f));
        Assert.That(vm.CustomBackgroundOpacity, Is.EqualTo(0.75f));
        Assert.That(vm.CustomTabPlacement, Is.EqualTo("Bottom"));
        Assert.That(vm.EnableClickVfx, Is.False);
    }

    [Test]
    public void TestScripting_Clamping()
    {
        var vm = new LauncherCustomizerViewModel();

        // Font clamping (10 - 24)
        vm.CustomUserCode = "Font = 5";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomFontSize, Is.EqualTo(10.0f));

        vm.CustomUserCode = "Font = 50";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomFontSize, Is.EqualTo(24.0f));

        // Opacity clamping (0.1 - 1.0)
        vm.CustomUserCode = "Opacity = 0.01";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomBackgroundOpacity, Is.EqualTo(0.1f));

        vm.CustomUserCode = "Opacity = 2.5";
        vm.ExecuteUserCode();
        Assert.That(vm.CustomBackgroundOpacity, Is.EqualTo(1.0f));
    }
}
