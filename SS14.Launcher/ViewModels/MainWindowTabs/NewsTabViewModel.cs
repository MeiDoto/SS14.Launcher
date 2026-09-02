using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public partial class NewsTabViewModel : MainWindowTabViewModel
{
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();

    public ObservableCollection<NewsEntryViewModel> AllNewsEntries { get; } = [];
    public ObservableCollection<NewsEntryViewModel> FilteredNewsEntries { get; } = [];

    private bool _startedPullingNews;

    [ObservableProperty]
    private bool _newsPulled;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "";

    private string _searchString = "";
    public string SearchString
    {
        get => _searchString;
        set
        {
            if (SetProperty(ref _searchString, value))
            {
                ApplyFilter();
            }
        }
    }

    public override string Name
    {
        get
        {
            var custom = _cfg.GetCVar(CVars.CustomNewsTabName);
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;
            return LocalizationManager.Instance.GetString("tab-news-title");
        }
    }

    public NewsTabViewModel()
    {
        _cfg.GetCVarEntry(CVars.CustomNewsTabName).PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
        };
    }

    public override void Selected()
    {
        base.Selected();

        if (!_startedPullingNews)
        {
            _ = PullNews();
        }
    }

    public async Task RefreshNews()
    {
        _startedPullingNews = false;
        await PullNews();
    }

    private static string NewsCacheFile => System.IO.Path.Combine(LauncherPaths.DirLocalData, "news_cache.json");

    private sealed record CachedNewsItem(string Title, string Link, DateTime? Date, string? Description);

    private void LoadCachedNews()
    {
        try
        {
            if (System.IO.File.Exists(NewsCacheFile))
            {
                var json = System.IO.File.ReadAllText(NewsCacheFile);
                var items = System.Text.Json.JsonSerializer.Deserialize<List<CachedNewsItem>>(json);
                if (items != null && items.Count > 0)
                {
                    AllNewsEntries.Clear();
                    foreach (var i in items)
                    {
                        if (Uri.TryCreate(i.Link, UriKind.Absolute, out var uri))
                        {
                            AllNewsEntries.Add(new NewsEntryViewModel(i.Title, uri, i.Date, i.Description));
                        }
                    }
                    ApplyFilter();
                    NewsPulled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load news cache");
        }
    }

    private void SaveCachedNews()
    {
        try
        {
            var cache = AllNewsEntries.Select(n => new CachedNewsItem(n.Headline, n.Link.ToString(), n.Date, n.Description)).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(cache);
            System.IO.File.WriteAllText(NewsCacheFile, json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to save news cache");
        }
    }

    private async Task PullNews()
    {
        if (_isBusy)
            return;

        if (AllNewsEntries.Count == 0)
        {
            LoadCachedNews();
        }

        _startedPullingNews = true;
        IsBusy = true;
        StatusMessage = LocalizationManager.Instance.GetString("tab-news-pulling-news");

        try
        {
            var http = Locator.Current.GetService<System.Net.Http.HttpClient>() ?? new System.Net.Http.HttpClient();
            using var cts = new System.Threading.CancellationTokenSource(10000);
            var feedString = await http.GetStringAsync(ConfigConstants.NewsFeedUrl, cts.Token);
            var feed = CodeHollow.FeedReader.FeedReader.ReadFromString(feedString);

            AllNewsEntries.Clear();
            foreach (var i in feed.Items)
            {
                AllNewsEntries.Add(new NewsEntryViewModel(i.Title, new Uri(i.Link), i.PublishingDate, i.Description));
            }

            ApplyFilter();
            SaveCachedNews();
            NewsPulled = true;
            StatusMessage = "";
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to pull news feed");
            if (AllNewsEntries.Count > 0)
            {
                StatusMessage = "(Offline cache)";
            }
            else
            {
                StatusMessage = $"{e.Message}";
            }
            NewsPulled = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredNewsEntries.Clear();

        var query = SearchString.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? AllNewsEntries.AsEnumerable()
            : AllNewsEntries.Where(n => n.Headline.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                       (n.Description != null && n.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));

        foreach (var item in matches)
        {
            FilteredNewsEntries.Add(item);
        }
    }
}
