using System;
using Avalonia.Controls;
using SS14.Launcher.ViewModels.MainWindowTabs;

namespace SS14.Launcher.Views.MainWindowTabs;

public partial class ReplaysTabView : UserControl
{
    private ReplaysTabViewModel? _viewModel;

    public ReplaysTabView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel = DataContext as ReplaysTabViewModel;
        if (_viewModel != null)
        {
            _viewModel.Control = this;
        }

        base.OnDataContextChanged(e);
    }
}
