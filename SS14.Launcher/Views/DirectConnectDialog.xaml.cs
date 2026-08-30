using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SS14.Launcher.Views;

public partial class DirectConnectDialog : Window
{
    private static string? _lastDirectConnectAddress;

    public DirectConnectDialog()
    {
        InitializeComponent();

        if (!string.IsNullOrEmpty(_lastDirectConnectAddress))
        {
            AddressBox.Text = _lastDirectConnectAddress;
        }

        AddressBox.TextChanged += (_, _) =>
        {
            var valid = IsAddressValid(AddressBox.Text);
            InvalidLabel.IsVisible = !valid;
            SubmitButton.IsEnabled = valid;
        };

        AddressBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Key.Enter && IsAddressValid(AddressBox.Text))
            {
                TrySubmit(sender, e);
            }
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        AddressBox.Focus();
        if (!string.IsNullOrEmpty(AddressBox.Text))
        {
            AddressBox.SelectAll();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
        }

        base.OnKeyDown(e);
    }

    private async void PasteClicked(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is { } clipboard)
        {
            var text = await clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                AddressBox.Text = text.Trim();
            }
        }
    }

    private void TrySubmit(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (!IsAddressValid(AddressBox.Text))
        {
            return;
        }

        var trimmed = AddressBox.Text.Trim();
        _lastDirectConnectAddress = trimmed;
        Close(trimmed);
    }

    internal static bool IsAddressValid([NotNullWhen(true)] string? address)
    {
        return !string.IsNullOrWhiteSpace(address) && UriHelper.TryParseSs14Uri(address, out _);
    }
}
