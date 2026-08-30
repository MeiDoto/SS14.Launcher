using System;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public class NewsEntryViewModel : ViewModelBase
{
    public NewsEntryViewModel(string headline, Uri link, DateTime? date = null, string? description = null)
    {
        Headline = headline;
        Link = link;
        Date = date;
        Description = description;
    }

    public string Headline { get; }
    public Uri Link { get; }
    public DateTime? Date { get; }
    public string? Description { get; }

    public string DateFormatted => Date?.ToString("dd.MM.yyyy") ?? "";
    public bool HasDate => Date.HasValue;

    public void Open()
    {
        Helpers.OpenUri(Link);
    }
}