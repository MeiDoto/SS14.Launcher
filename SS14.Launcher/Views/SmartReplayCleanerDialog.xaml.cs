using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.ViewModels;

namespace SS14.Launcher.Views;

public partial class SmartReplayCleanerDialog : Window
{
    public SmartReplayCleanerDialog()
    {
        InitializeComponent();
    }

    public SmartReplayCleanerDialog(IEnumerable<ReplayMetadataEntry> entries, Action onCompleted) : this()
    {
        DataContext = new SmartReplayCleanerViewModel(entries, onCompleted);
    }

    private void ExecuteCleanClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SmartReplayCleanerViewModel vm)
        {
            if (vm.ExecuteClean())
            {
                Close();
            }
        }
    }

    private void CancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
