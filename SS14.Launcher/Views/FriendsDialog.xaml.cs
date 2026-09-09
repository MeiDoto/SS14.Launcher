using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class FriendsDialog : Window
{
    public FriendsDialog()
    {
        InitializeComponent();
        SetupViewModel(new FriendsViewModel());
    }

    public FriendsDialog(Action<string>? onConnect)
    {
        InitializeComponent();
        SetupViewModel(new FriendsViewModel(onConnect));
    }

    private void SetupViewModel(FriendsViewModel vm)
    {
        DataContext = vm;
        vm.RequestClose += Close;
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
