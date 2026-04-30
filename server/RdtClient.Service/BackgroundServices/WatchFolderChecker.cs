using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace RdtClient.Service.BackgroundServices;

public class WatchFolderChecker(ILogger<WatchFolderChecker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private DateTime _prevCheck = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(1000, stoppingToken);
        }

        using var scope = serviceProvider.CreateScope();
        var torrentService = scope.ServiceProvider.GetRequiredService<Torrents>();
            
        logger.LogInformation("WatchFolderChecker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, stoppingToken);

                if (String.IsNullOrWhiteSpace(Settings.Get.Paths.WatchPath))
                {
                    continue;
                }

                var processedStorePath = Path.Combine(Settings.Get.Paths.WatchPath, "processed");
                var errorStorePath = Path.Combine(Settings.Get.Paths.WatchPath, "error");

                if (!String.IsNullOrWhiteSpace(Settings.Get.Paths.WatchProcessedPath))
                {
                    processedStorePath = Settings.Get.Paths.WatchProcessedPath;
                }

                if (!String.IsNullOrWhiteSpace(Settings.Get.Paths.WatchErrorPath))
                {
                    errorStorePath = Settings.Get.Paths.WatchErrorPath;
                }

                var nextCheck = _prevCheck.AddSeconds(Settings.Get.Watch.Interval);

                if (DateTime.UtcNow < nextCheck)
                {
                    continue;
                }

                _prevCheck = DateTime.UtcNow;

                var torrentFiles = Directory.GetFiles(Settings.Get.Paths.WatchPath, "*.*", SearchOption.TopDirectoryOnly);

                foreach (var torrentFile in torrentFiles)
                {
                    var fileInfo = new FileInfo(torrentFile);

                    if (fileInfo.Extension != ".magnet" && fileInfo.Extension != ".torrent")
                    {
                        continue;
                    }

                    if (IsFileLocked(fileInfo))
                    {
                        continue;
                    }

                    try
                    {
                        logger.Log(LogLevel.Debug, "Processing {torrentFile}", torrentFile);

                        var torrent = new Torrent
                        {
                            DownloadClient = Data.Enums.DownloadClient.Internal,
                            Category = Settings.Get.DownloadClient.Default.Category,
                            HostDownloadAction = Settings.Get.DownloadClient.Default.HostDownloadAction,
                            FinishedActionDelay = Settings.Get.DownloadClient.Default.FinishedActionDelay,
                            DownloadAction = Settings.Get.DownloadClient.Default.OnlyDownloadAvailableFiles
                                ? TorrentDownloadAction.DownloadAvailableFiles
                                : TorrentDownloadAction.DownloadAll,
                            FinishedAction = Settings.Get.DownloadClient.Default.FinishedAction,
                            DownloadMinSize = Settings.Get.DownloadClient.Default.MinFileSize,
                            IncludeRegex = Settings.Get.DownloadClient.Default.IncludeRegex,
                            ExcludeRegex = Settings.Get.DownloadClient.Default.ExcludeRegex,
                            TorrentRetryAttempts = Settings.Get.DownloadClient.Default.TorrentRetryAttempts,
                            DownloadRetryAttempts = Settings.Get.DownloadClient.Default.DownloadRetryAttempts,
                            DeleteOnError = Settings.Get.DownloadClient.Default.DeleteOnError,
                            Lifetime = Settings.Get.DownloadClient.Default.TorrentLifetime,
                            Priority = Settings.Get.DownloadClient.Default.Priority > 0 ? Settings.Get.DownloadClient.Default.Priority : null
                        };

                        if (fileInfo.Extension == ".torrent")
                        {
                            var torrentFileContents = await File.ReadAllBytesAsync(torrentFile, stoppingToken);
                            await torrentService.AddFileToDebridQueue(torrentFileContents, torrent);
                        }
                        else if (fileInfo.Extension == ".magnet")
                        {
                            var magnetLink = await File.ReadAllTextAsync(torrentFile, stoppingToken);
                            await torrentService.AddMagnetToDebridQueue(magnetLink, torrent);
                        }

                        if (!Directory.Exists(processedStorePath))
                        {
                            Directory.CreateDirectory(processedStorePath);
                        }
                        
                        var processedPath = Path.Combine(processedStorePath, fileInfo.Name);

                        if (File.Exists(processedPath))
                        {
                            File.Delete(processedPath);

                            logger.Log(LogLevel.Warning,
                                       "File {torrentFileName} replaced in {processedStorePath} - it already existed and new torrent with same filename was added",
                                       fileInfo.Name,
                                       processedStorePath);
                        }

                        File.Move(torrentFile, processedPath);

                        logger.Log(LogLevel.Debug, "Moved {torrentFile} to {processedPath}", torrentFile, processedPath);
                    }
                    catch
                    {
                        if (!Directory.Exists(errorStorePath))
                        {
                            Directory.CreateDirectory(errorStorePath);
                        }

                        var processedPath = Path.Combine(errorStorePath, fileInfo.Name);

                        if (File.Exists(processedPath))
                        {
                            File.Delete(processedPath);

                            logger.Log(LogLevel.Warning,
                                       "File {torrentFileName} replaced in {errorStorePath} - it already existed and new torrent with same filename was added",
                                       fileInfo.Name,
                                       errorStorePath);
                        }
                        File.Move(torrentFile, processedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unexpected error occurred in WatchFolderChecker: {ex.Message}");
            }
        }
    }

    private static Boolean IsFileLocked(FileInfo file)
    {
        try
        {
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
        {
            return true;
        }
        return false;
    }
}