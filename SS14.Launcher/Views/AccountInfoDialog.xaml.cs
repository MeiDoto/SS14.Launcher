using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class AccountInfoDialog : Window
{
    private readonly AccountInfoViewModel _viewModel;

    public AccountInfoDialog()
    {
        InitializeComponent();
        _viewModel = (DataContext as AccountInfoViewModel)!;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _viewModel.Populate();
    }

    private void CloseClicked(object? sender, RoutedEventArgs args)
    {
        Close();
    }
}
