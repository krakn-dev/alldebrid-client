using AdbClient.Data.Models.Data;
using AdbClient.Service.Helpers;
using SharpCompress.Archives;

namespace AdbClient.Service.Services;

public class UnpackClient(Download download, string destinationPath)
{
    public bool Finished { get; private set; }

    public string? Error { get; private set; }

    public int Progress { get; private set; }

    private readonly Torrent _torrent = download.Torrent ?? throw new Exception("Torrent is null");

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void Start()
    {
        Progress = 0;

        try
        {
            var filePath = DownloadHelper.GetDownloadPath(destinationPath, _torrent, download) ?? throw new Exception("Invalid download path");

            Task.Run(async delegate
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await Unpack(filePath, _cancellationTokenSource.Token);
                }
            });
        }
        catch (Exception ex)
        {
            Error = $"An unexpected error occurred preparing download {download.Link} for torrent {_torrent.RdName}: {ex.Message}";
            Finished = true;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    private async Task Unpack(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var extractPath = destinationPath;
            string? extractPathTemp = null;

            var archiveEntries = await GetArchiveFiles(filePath);
            extractPath = ResolveExtractionPath(destinationPath, _torrent, archiveEntries);

            if (archiveEntries.Any(m => m.Contains(".r00")))
            {
                extractPathTemp = Path.Combine(extractPath, Guid.NewGuid().ToString());

                if (!Directory.Exists(extractPathTemp))
                {
                    Directory.CreateDirectory(extractPathTemp);
                }
            }

            if (extractPathTemp != null)
            {
                await Extract(filePath, extractPathTemp, cancellationToken);

                await FileHelper.Delete(filePath);

                var rarFiles = Directory.GetFiles(extractPathTemp, "*.r00", SearchOption.TopDirectoryOnly);

                foreach (var rarFile in rarFiles)
                {
                    var mainRarFile = Path.ChangeExtension(rarFile, ".rar");

                    if (File.Exists(mainRarFile))
                    {
                        await Extract(mainRarFile, extractPath, cancellationToken);
                    }

                    await FileHelper.DeleteDirectory(extractPathTemp);
                }
            }
            else
            {
                await Extract(filePath, extractPath, cancellationToken);

                await FileHelper.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Error = $"An unexpected error occurred unpacking {download.Link} for torrent {_torrent.RdName}: {ex.Message}";
        }
        finally
        {
            Finished = true;
        }
    }

    private static async Task<IList<string>> GetArchiveFiles(string filePath)
    {
        await using Stream stream = File.OpenRead(filePath);

        using var archive = ArchiveFactory.OpenArchive(stream);

        var entries = archive.Entries
                             .Where(entry => !entry.IsDirectory)
                             .Select(m => m.Key!)
                             .ToList();

        return entries;
    }

    private async Task Extract(string filePath, string extractPath, CancellationToken cancellationToken)
    {
        var parts = ArchiveFactory.GetFileParts(filePath);
        var files = parts.Select(part => new FileInfo(part)).ToList();
        using var archive = ArchiveFactory.OpenArchive(files);
        var entries = archive.Entries.ToList();

        for (var index = 0; index < entries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await entries[index].WriteToDirectoryAsync(extractPath, cancellationToken: cancellationToken);
            Progress = (int)Math.Round((index + 1d) / entries.Count * 100);
        }
    }

    internal static string ResolveExtractionPath(
        string destinationPath,
        Torrent torrent,
        IEnumerable<string> archiveEntries)
    {
        var torrentDirectory = DownloadHelper.GetTorrentDirectoryName(torrent);
        var entrySegments = archiveEntries
                           .Where(entry => !string.IsNullOrWhiteSpace(entry))
                           .Select(entry => entry.Split(
                               ['/', '\\'],
                               StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                           .ToList();
        var archiveContainsOnlyTorrentRoot = entrySegments.Count > 0 && entrySegments.All(segments =>
            segments.Length > 1 &&
            string.Equals(
                FileHelper.RemoveInvalidFileNameChars(segments[0]),
                torrentDirectory,
                StringComparison.OrdinalIgnoreCase));
        var normalizedDestination = FileSystemPath.Normalize(destinationPath);
        var extractPath = archiveContainsOnlyTorrentRoot
            ? normalizedDestination
            : FileSystemPath.Normalize(Path.Combine(normalizedDestination, torrentDirectory));

        if (!FileSystemPath.IsSameOrDescendant(extractPath, normalizedDestination))
        {
            throw new InvalidDataException("Archive extraction path is outside the configured download directory.");
        }

        return extractPath;
    }
}
