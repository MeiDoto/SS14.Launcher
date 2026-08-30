using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SS14.Launcher.Views;

public partial class LogViewerDialog : Window
{
    public LogViewerDialog()
    {
        InitializeComponent();
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
