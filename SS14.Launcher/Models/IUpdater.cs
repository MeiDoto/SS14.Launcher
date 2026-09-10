using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using SS14.Launcher.Models.ContentManagement;

namespace SS14.Launcher.Models;

/// <summary>
/// Service interface for game engine and content update management.
/// </summary>
public interface IUpdater : INotifyPropertyChanged
{
    Updater.UpdateStatus Status { get; }
    (long downloaded, long total, Updater.ProgressUnit unit)? Progress { get; }
    long? Speed { get; }
    Exception? UpdateException { get; set; }

    Task<ContentLaunchInfo?> RunUpdateForLaunchAsync(
        ServerBuildInformation buildInformation,
        CancellationToken cancel = default);

    Task<ContentLaunchInfo?> InstallContentBundleForLaunchAsync(
        ZipArchive archive,
        byte[] zipHash,
        ContentBundleMetadata metadata,
        CancellationToken cancel = default);
}
