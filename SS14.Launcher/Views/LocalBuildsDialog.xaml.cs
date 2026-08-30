using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class LocalBuildsDialog : Window
{
    public LocalBuildsDialog()
    {
        InitializeComponent();
    }

    private void BrowseClicked(object? sender, RoutedEventArgs e)
    {
        ((LocalBuildsViewModel)DataContext!).BrowseFile(this);
    }

    private void LaunchClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LocalBuildItemViewModel item })
        {
            ((LocalBuildsViewModel)DataContext!).LaunchBuild(item, this);
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
