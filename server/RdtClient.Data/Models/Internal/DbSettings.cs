using RdtClient.Data.Enums;
using System.ComponentModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace RdtClient.Data.Models.Internal;

public class DbSettings
{
    [DisplayName("General")]
    [Description("")]
    public DbSettingsGeneral General { get; set; } = new();

    [DisplayName("Download")]
    [Description("")]
    public DbSettingsDownloadClient DownloadClient { get; set; } = new();

    [DisplayName("Paths")]
    [Description("")]
    public DbSettingsPaths Paths { get; set; } = new();

    [DisplayName("AllDebrid")]
    [Description("")]
    public DbSettingsProvider Provider { get; set; } = new();

    [DisplayName("Watch")]
    [Description("The following settings only apply when a torrent gets through the watch folder.")]
    public DbSettingsWatch Watch { get; set; } = new();
}

public class DbSettingsGeneral
{
    [DisplayName("Log level")]
    [Description("Recommended level is Warning, set to Debug to get the most info.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Error;

    [DisplayName("Maximum parallel downloads")]
    [Description("Maximum amount of torrents that get downloaded to your host at the same time.")]
    public int DownloadLimit { get; set; } = 2;

    [DisplayName("Maximum unpack processes")]
    [Description("Maximum amount of downloads that get unpacked on your host at the same time. Set to 0 to disable unpacking.")]
    public int UnpackLimit { get; set; } = 1;

    [DisplayName("Categories")]
    [Description("Define available categories, separated by commas.")]
    public string? Categories { get; set; } = null;

    [DisplayName("Run external program on torrent completion")]
    [Description("Path to the executable to run when the torrent and all downloads are finished. No arguments should be passed here.")]
    public string? RunOnTorrentCompleteFileName { get; set; } = null;

    [DisplayName("External program arguments")]
    [Description(@"When the executable above is executed, use these parameters.
Supports the following parameters:
%N: Torrent name
%L: Category
%F: Content path (same as root path for multifile torrent)
%R: Root path (first torrent subdirectory path)
%D: Save path
%C: Number of files
%Z: Torrent size (bytes)
%I: Info hash")]
    public string? RunOnTorrentCompleteArguments { get; set; } = null;

    [DisplayName("Authentication Type")]
    [Description("How to authenticate with the client. WARNING: when set to None anyone with access to the URL can use the client without any credentials.")]
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.None;

    [DisplayName("Tracker enrichment list")]
    [Description("Optional. Specify the URL of a tracker list file to be appended to magnet links and torrent files.")]
    public string? TrackerEnrichmentList { get; set; } = null;

    [DisplayName("Tracker enrichment cache expiration")]
    [Description("The time in minutes to cache the tracker list. Set to 0 to disable caching.")]
    public int TrackerEnrichmentCacheExpiration { get; set; } = 60;

    [DisplayName("Banned Trackers")]
    [Description("Torrents that come from these trackers will not be allowed, this is a failsafe if you are accidentally downloading from private trackers. Will compare by keyword. Define multiple trackers by separating them with a comma.")]
    public string? BannedTrackers { get; set; } = null;

    [DisplayName("Disable update notifications")]
    [Description("Ignore update notifications. You will still be notified if the version you are running has a security vulnerability.")]
    public bool DisableUpdateNotifications { get; set; } = false;
}

public class DbSettingsDownloadClient
{
    [DisplayName("Download speed (in MB/s) (only used for the Internal Downloader)")]
    [Description("Maximum download speed in Megabytes per second. When set to 0 unlimited speed is used.")]
    public int MaxSpeed { get; set; } = 0;

    [DisplayName("Parallel connections per download (only used for the Internal Downloader)")]
    [Description("Maximum amount of parallel threads that are used to download a single file to your host. If set to 0 no parallel downloading will be done.")]
    public int ParallelCount { get; set; } = 8;

    [DisplayName("Chunk Count")]
    [Description("Split the downloaded file in this amount of chunks.")]
    public int ChunkCount { get; set; } = 0;

    [DisplayName("Buffer Size")]
    [Description("Buffersize in bytes for the internal downloader, used to read data and write it to disk.")]
    public int BufferSize { get; set; } = 4 * 1024 * 1024;

    [DisplayName("Log level")]
    [Description("Only set when trying to debug a download client, can generate a lot of logs.")]
    public DownloadClientLogLevel LogLevel { get; set; } = DownloadClientLogLevel.None;

    [DisplayName("Automatically import and process torrents added to provider")]
    [Description("When selected, import downloads that are not added through RealDebridClient but have been directly added to your debrid provider.")]
    public bool AutoImport { get; set; } = false;

    [DisplayName("Automatically delete downloads removed from provider")]
    [Description("When selected, cancel and delete downloads that have been removed from your debrid provider.")]
    public bool AutoDelete { get; set; } = false;

    [DisplayName("Max parallel downloads")]
    [Description("Limits the number of torrents that will be sent for downloading on the debrid provider at the same time. If set to 0, all downloads will be sent immediately without queuing.")]
    public int MaxParallelDownloads { get; set; } = 0;

    [DisplayName("Defaults")]
    public DbSettingsDefaultsWithCategory Default { get; set; } = new();
}

public class DbSettingsPaths
{
    [DisplayName("Download path")]
    [Description(@"Path to download files to (e.g. C:\Downloads).")]
    public string DownloadPath { get; set; } = @"C:\Downloads";

    [DisplayName("Mapped path")]
    [Description(@"Path where files are downloaded to on your host (e.g. D:\Downloads). This path is used for *arr to find your downloads.")]
    public string MappedPath { get; set; } = @"C:\Downloads";

    [DisplayName("Copy added torrent files")]
    [Description("When a torrent file or magnet is added, create a copy in this directory.")]
    public string? CopyAddedTorrents { get; set; } = null;

    [DisplayName("Watch Path")]
    [Description("Watch this path for .torrent or .magnet files. When a file is found it will be automatically imported.")]
    public string? WatchPath { get; set; } = null;

    [DisplayName("Watch Error Path")]
    [Description(@"When an error occurs the torrent file is moved to this directory. When unset it will be moved to \error in the watch path.")]
    public string? WatchErrorPath { get; set; } = null;

    [DisplayName("Watch Processed Path")]
    [Description(@"When a torrent file is added successfully it will be moved to this directory. When unset it will be moved to \processed in the watch path.")]
    public string? WatchProcessedPath { get; set; } = null;
}

public class DbSettingsProvider
{
    [DisplayName("API Key")]
    [Description(@"You can find your AllDebrid API key here:
<a href=""https://alldebrid.com/apikeys/"" target=""_blank"" rel=""noopener"">https://alldebrid.com/apikeys/</a>")]
    public string ApiKey { get; set; } = "";

    [DisplayName("Connection Timeout")]
    [Description("Timeout in seconds to make a connection to the provider. Increase if you experience timeouts in the logs.")]
    public int Timeout { get; set; } = 10;

    [DisplayName("Check Interval")]
    [Description("The interval to check the torrents info on the providers API. Minumum is 3 seconds. When there are no active downloads this limit is increased * 3.")]
    public int CheckInterval { get; set; } = 10;
}

public class DbSettingsWatch
{
    [DisplayName("Check Interval")]
    [Description("Time in seconds to check the folder for new files.")]
    public int Interval { get; set; } = 60;
}

public class DbSettingsDefaultsWithCategory : DbSettingsDefaults
{
    [DisplayName("Post Torrent Download Action")]
    [Description("When a torrent is finished downloading on your debrid provider, perform this action. Use this setting if you only want to add files to your debrid provider but not download them to the host.")]
    public TorrentHostDownloadAction HostDownloadAction { get; set; }

    [DisplayName("Category")]
    [Description("When a torrent is imported assign it this category.")]
    public string? Category { get; set; } = null;

    [DisplayName("Post Download Action")]
    [Description("When all files are downloaded from the provider to the host, perform this action. Does not apply when using the symlink downloader.")]
    public TorrentFinishedAction FinishedAction { get; set; } = TorrentFinishedAction.RemoveAllTorrents;

    [DisplayName("Finished Action Delay")]
    [Description("When all files are downloaded from the provider to the host, wait this many minutes before performing the action above.")]
    public int FinishedActionDelay { get; set; } = 0;
}

public class DbSettingsDefaults
{
    [DisplayName("Only download available files on debrid provider")]
    [Description("When selected, it will only download files in the torrent that have been download by AllDebrid. You can use this in combination with the Min File size setting above.")]
    public bool OnlyDownloadAvailableFiles { get; set; } = true;

    [DisplayName("Minimum file size to download")]
    [Description("Files that are smaller than this setting are skipped and not downloaded. When set to 0 all files are downloaded. When downloading from Radarr or Sonarr it's recommended to keep this setting at atleast a few MB to avoid the debrid provider having to re-download the torrent.")]
    public int MinFileSize { get; set; } = 0;

    [DisplayName("Include files")]
    [Description("Select only the files that are matching this regular expression. Only use this setting OR the Exclude files setting, not both.")]
    public string? IncludeRegex { get; set; }

    [DisplayName("Exclude files")]
    [Description("Ignore files that are matching this regular expression. Only use this setting OR the Include files setting, not both.")]
    public string? ExcludeRegex { get; set; }

    [DisplayName("Automatic retry torrent")]
    [Description("When a single download has failed multiple times (see setting above) or when the torrent itself received an error it will retry the full torrent this many times before marking it failed.")]
    public int TorrentRetryAttempts { get; set; } = 1;

    [DisplayName("Automatic retry downloads")]
    [Description("When a single download fails it will retry it this many times before marking it as failed.")]
    public int DownloadRetryAttempts { get; set; } = 3;

    [DisplayName("Delete download when in error")]
    [Description("When a download has been in error for this many minutes, delete it from the provider and the client. 0 to disable.")]
    public int DeleteOnError { get; set; } = 0;

    [DisplayName("Torrent maximum lifetime")]
    [Description("The maximum lifetime of a torrent in minutes. When this time has passed, mark the torrent as error. If the torrent is completed and has downloads, the lifetime setting will not apply. 0 to disable.")]
    public int TorrentLifetime { get; set; } = 0;

    [DisplayName("Priority")]
    [Description("Set the priority of a torrent, 1 = highest, 0 = disabled.")]
    public int Priority { get; set; } = 0;
}
