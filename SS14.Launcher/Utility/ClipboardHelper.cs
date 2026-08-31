using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SS14.Launcher.Localization;

namespace SS14.Launcher.Utility;

/// <summary>
/// Provides unified, safe cross-platform clipboard access with automatic UI button feedback timers.
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// Copies text to the active window clipboard and temporarily updates a UI property with feedback text.
    /// </summary>
    /// <param name="text">The string to copy to the clipboard.</param>
    /// <param name="setTextAction">Action to set the button/label text (e.g. `s => CopyButtonText = s`).</param>
    /// <param name="customFeedback">Optional feedback string. Defaults to localized 'account-info-copied'.</param>
    /// <param name="timeoutSeconds">Duration in seconds to display the feedback before reverting to empty.</param>
    public static async Task CopyWithFeedbackAsync(
        string text,
        Action<string> setTextAction,
        string? customFeedback = null,
        int timeoutSeconds = 2)
    {
        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                if (window?.Clipboard != null)
                {
                    await window.Clipboard.SetTextAsync(text);
                    var feedback = customFeedback ?? LocalizationManager.Instance.GetString("account-info-copied");
                    setTextAction(feedback);

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(timeoutSeconds) };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        setTextAction("");
                    };
                    timer.Start();
                }
            }
        }
        catch (Exception)
        {
            // Clipboard access can fail if locked by another process
        }
    }

    /// <summary>
    /// Reads text from the clipboard of the active window.
    /// </summary>
    public static async Task<string?> GetTextAsync()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                if (window?.Clipboard != null)
                {
                    return await window.Clipboard.GetTextAsync();
                }
            }
        }
        catch (Exception)
        {
            // Ignore clipboard access errors
        }

        return null;
    }
}
