using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Serilog;
using SS14.Launcher.Models;

namespace SS14.Launcher.Views;

public sealed partial class ServerInfoLinkControl : UserControl
{
    private static readonly HashSet<string> ValidIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord",
        "wiki",
        "web",
        "github",
        "forum",
        "telegram",
    };

    public ServerInfoLinkControl()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ServerInfoLink link)
            return;

        Helpers.SafeOpenServerUri(link.Url);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not ServerInfoLink link)
            return;

        if (string.IsNullOrWhiteSpace(link.Icon))
            return;

        var iconName = link.Icon.Trim().ToLowerInvariant();
        var resourceKey = iconName switch
        {
            "gitlab" => "InfoIcon-github",
            "vk" or "vkontakte" => "InfoIcon-forum",
            "matrix" => "InfoIcon-discord",
            _ when ValidIcons.Contains(iconName) => $"InfoIcon-{iconName}",
            _ => "InfoIcon-web"
        };

        if (this.TryFindResource(resourceKey, out var res) && res is IImage img)
        {
            IconLabel.Icon = img;
        }
        else if (this.TryFindResource("InfoIcon-web", out var fallbackRes) && fallbackRes is IImage fallbackImg)
        {
            IconLabel.Icon = fallbackImg;
        }
    }
}
