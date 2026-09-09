using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.ViewModels;

public sealed class SmartReplayCleanerViewModel : ViewModelBase
{
    private readonly List<ReplayMetadataEntry> _allEntries;
    private readonly Action _onCompleted;
    private readonly LocalizationManager _loc = LocalizationManager.Instance;

    private int _selectedAgeIndex = 2; // Default: Older than 30 days
    public int SelectedAgeIndex
    {
        get => _selectedAgeIndex;
        set
        {
            if (SetProperty(ref _selectedAgeIndex, value))
            {
                UpdateCandidates();
            }
        }
    }

    public string[] AgeOptions => [
        _loc.GetString("replay-cleaner-age-all"),
        _loc.GetString("replay-cleaner-age-14"),
        _loc.GetString("replay-cleaner-age-30"),
        _loc.GetString("replay-cleaner-age-60"),
        _loc.GetString("replay-cleaner-age-90")
    ];

    private bool _deleteCorrupted = true;
    public bool DeleteCorrupted
    {
        get => _deleteCorrupted;
        set
        {
            if (SetProperty(ref _deleteCorrupted, value))
            {
                UpdateCandidates();
            }
        }
    }

    private bool _excludeFavorites = true;
    public bool ExcludeFavorites
    {
        get => _excludeFavorites;
        set
        {
            if (SetProperty(ref _excludeFavorites, value))
            {
                UpdateCandidates();
            }
        }
    }

    private int _selectedQuotaIndex = 0; // Default: No quota
    public int SelectedQuotaIndex
    {
        get => _selectedQuotaIndex;
        set
        {
            if (SetProperty(ref _selectedQuotaIndex, value))
            {
                UpdateCandidates();
            }
        }
    }

    public string[] QuotaOptions => [
        _loc.GetString("replay-cleaner-quota-none"),
        _loc.GetString("replay-cleaner-quota-1gb"),
        _loc.GetString("replay-cleaner-quota-2gb"),
        _loc.GetString("replay-cleaner-quota-5gb"),
        _loc.GetString("replay-cleaner-quota-10gb")
    ];

    private int _candidatesCount;
    public int CandidatesCount
    {
        get => _candidatesCount;
        private set
        {
            if (SetProperty(ref _candidatesCount, value))
            {
                OnPropertyChanged(nameof(CanClean));
            }
        }
    }

    private string _freedBytesFormatted = "0 B";
    public string FreedBytesFormatted
    {
        get => _freedBytesFormatted;
        private set => SetProperty(ref _freedBytesFormatted, value);
    }

    public bool CanClean => CandidatesCount > 0;

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private List<ReplayMetadataEntry> _currentCandidates = new();

    public SmartReplayCleanerViewModel(IEnumerable<ReplayMetadataEntry> entries, Action onCompleted)
    {
        _allEntries = entries.ToList();
        _onCompleted = onCompleted;

        _loc.LanguageSwitched += () =>
        {
            OnPropertyChanged(nameof(AgeOptions));
            OnPropertyChanged(nameof(QuotaOptions));
        };

        UpdateCandidates();
    }

    private void UpdateCandidates()
    {
        int? olderThanDays = SelectedAgeIndex switch
        {
            1 => 14,
            2 => 30,
            3 => 60,
            4 => 90,
            _ => null
        };

        long? quotaBytes = SelectedQuotaIndex switch
        {
            1 => 1024L * 1024 * 1024,
            2 => 2L * 1024 * 1024 * 1024,
            3 => 5L * 1024 * 1024 * 1024,
            4 => 10L * 1024 * 1024 * 1024,
            _ => null
        };

        var options = new CleanerOptions
        {
            OlderThanDays = olderThanDays,
            DeleteCorrupted = DeleteCorrupted,
            ExcludeFavorites = ExcludeFavorites,
            MaxTotalQuotaBytes = quotaBytes
        };

        _currentCandidates = SmartReplayCleaner.Scan(_allEntries, options);
        CandidatesCount = _currentCandidates.Count;
        var freed = _currentCandidates.Sum(c => c.FileSize);
        FreedBytesFormatted = StorageAnalyzer.FormatBytes(freed);
    }

    public bool ExecuteClean()
    {
        if (_currentCandidates.Count == 0)
            return false;

        var result = SmartReplayCleaner.Execute(_currentCandidates, ReplayMetadataCache.Instance);
        _onCompleted();
        return true;
    }
}
