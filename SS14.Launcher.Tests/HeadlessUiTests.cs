#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Simple;
using NUnit.Framework;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Views;
using SS14.Launcher.Views.MainWindowTabs;

[assembly: AvaloniaTestApplication(typeof(SS14.Launcher.Tests.HeadlessAppBuilder))]

namespace SS14.Launcher.Tests;

public static class HeadlessAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestHeadlessApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class TestHeadlessApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new SimpleTheme());

        var baseUri = new Uri("avares://SS14.Launcher/");
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeResources.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/Theme.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeAngleBox.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeButton.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeServerList.axaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeCheckBox.axaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeOverlayBox.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeDungSpinner.xaml") });
        Styles.Add(new StyleInclude(baseUri) { Source = new Uri("avares://SS14.Launcher/Theme/ThemeRandomMessage.xaml") });

        // Setup mock/test DI services for LocExtension and views
        var dataManager = new DataManager();
        var locManager = new LocalizationManager(dataManager);
        locManager.Initialize();
        Locator.CurrentMutable.RegisterConstant<IDataManager>(dataManager);
        Locator.CurrentMutable.RegisterConstant(dataManager);
        Locator.CurrentMutable.RegisterConstant(locManager);
    }
}

[TestFixture]
public sealed class HeadlessUiTests
{
    [AvaloniaTest]
    public void TestAngleBox_MeasureAndArrange_Success()
    {
        var box = new AngleBox
        {
            Width = 200,
            Height = 100,
            CornerSize = 10,
            SideStyle = AngleBoxSideStyle.OpenRight
        };

        box.Measure(new Size(200, 100));
        box.Arrange(new Rect(0, 0, 200, 100));

        Assert.That(box.Bounds.Width, Is.EqualTo(200));
        Assert.That(box.Bounds.Height, Is.EqualTo(100));
    }

    [AvaloniaTest]
    public void TestAngleBox_AllSideStyles_MeasureWithoutError()
    {
        AngleBoxSideStyle[] styles =
        [
            AngleBoxSideStyle.Full,
            AngleBoxSideStyle.OpenLeft,
            AngleBoxSideStyle.OpenRight,
            AngleBoxSideStyle.OpenBoth
        ];

        foreach (var style in styles)
        {
            var box = new AngleBox
            {
                Width = 150,
                Height = 60,
                CornerSize = 8,
                SideStyle = style
            };

            box.Measure(new Size(150, 60));
            box.Arrange(new Rect(0, 0, 150, 60));

            Assert.That(box.Bounds.Width, Is.EqualTo(150));
            Assert.That(box.Bounds.Height, Is.EqualTo(60));
        }
    }

    [AvaloniaTest]
    public void TestThemeResources_BrushesAndColors_ResolveCorrectly()
    {
        var app = Application.Current;
        Assert.That(app, Is.Not.Null);

        Assert.That(app!.TryFindResource("ThemeBackgroundOverlayBrush", out var overlayBrush), Is.True);
        Assert.That(overlayBrush, Is.Not.Null);

        Assert.That(app.TryFindResource("ThemeServerListBackgroundBrush", out var serverListBg), Is.True);
        Assert.That(serverListBg, Is.Not.Null);

        Assert.That(app.TryFindResource("ThemeServerListRowAltBrush", out var serverListRowAlt), Is.True);
        Assert.That(serverListRowAlt, Is.Not.Null);

        Assert.That(app.TryFindResource("ThemeNanoGoldBrush", out var nanoGold), Is.True);
        Assert.That(nanoGold, Is.Not.Null);
    }

    [AvaloniaTest]
    public void TestServerFilterView_InstantiatesAndLoadsComponents()
    {
        var view = new ServerFilterView();
        Assert.That(view, Is.Not.Null);

        view.Measure(new Size(300, 40));
        view.Arrange(new Rect(0, 0, 300, 40));

        Assert.That(view.Bounds.Width, Is.EqualTo(300));
        Assert.That(view.Bounds.Height, Is.EqualTo(40));
    }

    [AvaloniaTest]
    public void TestServerFilterCounterView_InstantiatesAndLoadsComponents()
    {
        var view = new ServerFilterCounterView();
        Assert.That(view, Is.Not.Null);

        view.Measure(new Size(300, 30));
        view.Arrange(new Rect(0, 0, 300, 30));

        Assert.That(view.Bounds.Width, Is.EqualTo(300));
        Assert.That(view.Bounds.Height, Is.EqualTo(30));
    }

    [AvaloniaTest]
    public void TestServerListFiltersView_InstantiatesAndLoadsComponents()
    {
        var view = new ServerListFiltersView();
        Assert.That(view, Is.Not.Null);

        view.Measure(new Size(400, 300));
        view.Arrange(new Rect(0, 0, 400, 300));

        Assert.That(view.Bounds.Width, Is.EqualTo(400));
        Assert.That(view.Bounds.Height, Is.EqualTo(300));
    }

    [AvaloniaTest]
    public void TestDungSpinner_InstantiatesAndMeasures()
    {
        var spinner = new DungSpinner
        {
            Width = 48,
            Height = 48
        };

        spinner.Measure(new Size(48, 48));
        spinner.Arrange(new Rect(0, 0, 48, 48));

        Assert.That(spinner.Bounds.Width, Is.EqualTo(48));
        Assert.That(spinner.Bounds.Height, Is.EqualTo(48));
    }

    [AvaloniaTest]
    public void TestRandomMessage_InstantiatesAndMeasures()
    {
        var msg = new RandomMessage
        {
            Width = 250,
            Height = 30
        };

        msg.Measure(new Size(250, 30));
        msg.Arrange(new Rect(0, 0, 250, 30));

        Assert.That(msg.Bounds.Width, Is.EqualTo(250));
        Assert.That(msg.Bounds.Height, Is.EqualTo(30));
    }

    [AvaloniaTest]
    public void TestConnectingOverlay_InstantiatesAndMeasures()
    {
        var overlay = new ConnectingOverlay();
        Assert.That(overlay, Is.Not.Null);

        overlay.Measure(new Size(400, 200));
        overlay.Arrange(new Rect(0, 0, 400, 200));

        Assert.That(overlay.Bounds.Width, Is.EqualTo(400));
        Assert.That(overlay.Bounds.Height, Is.EqualTo(200));
    }

    [AvaloniaTest]
    public void TestLauncherUpdatePromptOverlayView_InstantiatesAndMeasures()
    {
        var prompt = new LauncherUpdatePromptOverlayView();
        Assert.That(prompt, Is.Not.Null);
        Assert.That(prompt.Content, Is.Not.Null);
    }
}
