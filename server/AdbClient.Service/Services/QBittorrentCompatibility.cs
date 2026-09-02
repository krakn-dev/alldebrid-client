using System.IO.Abstractions;
using AdbClient.Data.Enums;
using AdbClient.Data.Models.Data;
using AdbClient.Service.Helpers;
using AdbClient.Service.Models.QBittorrent;
using Microsoft.Extensions.Logging;

namespace AdbClient.Service.Services;

public sealed class QBittorrentCompatibility(
    ILogger<QBittorrentCompatibility> logger,
    Authentication authentication,
    Settings settings,
    Torrents torrents,
    IHttpClientFactory httpClientFactory,
    IFileSystem fileSystem) : IQBittorrentCompatibility
{
    private const long UnknownEta = 8_640_000;
    private const string LogposeCategory = "logpose";
    private const string LogposeRetainedCategory = "logpose-retained";

    public async Task<bool> Login(string userName, string password)
    {
        var result = await authentication.Login(userName, password);
        return result.Succeeded;
    }

    public async Task CreateCategory(string category)
    {
        category = category.Trim();

        if (category.Length == 0)
        {
            throw new ArgumentException("Category cannot be empty.", nameof(category));
        }

        var categories = (Settings.Get.General.Categories ?? string.Empty)
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();

        if (!categories.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            categories.Add(category);
            await settings.Update("General:Categories", string.Join(',', categories));
        }
    }

    public async Task Add(string urls, string? category, CancellationToken cancellationToken = default)
    {
        var torrentUrls = urls.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (torrentUrls.Length == 0)
        {
            throw new ArgumentException("At least one torrent URL is required.", nameof(urls));
        }

        foreach (var torrentUrl in torrentUrls)
        {
            var normalizedTorrentUrl = NormalizeTorrentUrl(torrentUrl);
            var torrent = CreateTorrent(category);

            if (normalizedTorrentUrl.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase))
            {
                await torrents.AddMagnetToDebridQueue(normalizedTorrentUrl, torrent);
                continue;
            }

            if (!Uri.TryCreate(normalizedTorrentUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"Unsupported torrent URL: {normalizedTorrentUrl}", nameof(urls));
            }

            logger.LogDebug("Downloading torrent metadata from {TorrentUrl}", uri);

            var client = httpClientFactory.CreateClient();
            var fileBytes = await client.GetByteArrayAsync(uri, cancellationToken);
            await torrents.AddFileToDebridQueue(fileBytes, torrent);
        }
    }

    private static string NormalizeTorrentUrl(string torrentUrl)
    {
        if (!Uri.TryCreate(torrentUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.IdnHost, "nyaa.si", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal) ||
            string.IsNullOrEmpty(uri.Query))
        {
            return torrentUrl;
        }

        const string queryPrefix = "?q=";
        var isValidInfoHashSearch = uri.Scheme == Uri.UriSchemeHttps &&
                                    uri.IsDefaultPort &&
                                    string.IsNullOrEmpty(uri.UserInfo) &&
                                    string.IsNullOrEmpty(uri.Fragment) &&
                                    uri.Query.StartsWith(queryPrefix, StringComparison.Ordinal) &&
                                    uri.Query.Length == queryPrefix.Length + 40;

        if (!isValidInfoHashSearch)
        {
            throw new ArgumentException($"Unsupported Nyaa search URL: {torrentUrl}", nameof(torrentUrl));
        }

        var infoHash = uri.Query[queryPrefix.Length..];

        if (infoHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException($"Unsupported Nyaa search URL: {torrentUrl}", nameof(torrentUrl));
        }

        return $"magnet:?xt=urn:btih:{infoHash.ToLowerInvariant()}";
    }

    public async Task<IReadOnlyList<QBittorrentTorrentInfo>> GetTorrents(string? category)
    {
        var allTorrents = await torrents.Get();
        var filteredTorrents = allTorrents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filteredTorrents = filteredTorrents.Where(torrent =>
                string.Equals(torrent.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        return filteredTorrents.Select(MapTorrent).ToList();
    }

    public async Task Delete(string hashes, bool deleteFiles)
    {
        var torrentHashes = hashes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var hash in torrentHashes)
        {
            var torrent = await torrents.GetByHash(hash);

            if (torrent == null)
            {
                continue;
            }

            var retainLogposeJob = !deleteFiles && IsLogposeManagedCategory(torrent.Category);
            var cleanupPlan = deleteFiles
                ? null
                : CreateEmptyDirectoryCleanupPlan(
                    torrent,
                    string.Equals(torrent.Category, LogposeRetainedCategory, StringComparison.OrdinalIgnoreCase)
                        ? LogposeCategory
                        : null);

            if (retainLogposeJob)
            {
                // Logpose uses deleteFiles=false after a successful import. Move the job out
                // of its active category so Logpose can finish, while leaving ADC and the
                // provider record under the user's configured retention policy.
                if (!string.Equals(torrent.Category, LogposeRetainedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    await torrents.UpdateCategory(hash, LogposeRetainedCategory);
                }

                if (cleanupPlan != null)
                {
                    CleanupEmptyJobDirectories(cleanupPlan);
                }

                continue;
            }

            await torrents.Delete(torrent.TorrentId, true, true, deleteFiles);

            if (cleanupPlan != null)
            {
                CleanupEmptyJobDirectories(cleanupPlan);
            }
        }
    }

    private static bool IsLogposeManagedCategory(string? category)
    {
        return string.Equals(category, LogposeCategory, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(category, LogposeRetainedCategory, StringComparison.OrdinalIgnoreCase);
    }

    private EmptyDirectoryCleanupPlan? CreateEmptyDirectoryCleanupPlan(
        Torrent torrent,
        string? categoryOverride = null)
    {
        if (string.IsNullOrWhiteSpace(Settings.Get.Paths.DownloadPath) ||
            string.IsNullOrWhiteSpace(torrent.RdName))
        {
            return null;
        }

        try
        {
            var downloadRoot = NormalizeFullPath(Settings.Get.Paths.DownloadPath);
            var cleanupCategory = categoryOverride ?? torrent.Category;
            var categoryRoot = string.IsNullOrWhiteSpace(cleanupCategory)
                ? downloadRoot
                : NormalizeFullPath(fileSystem.Path.Combine(downloadRoot, cleanupCategory));

            if (!IsSameOrDescendant(categoryRoot, downloadRoot))
            {
                logger.LogWarning(
                    "Skipping qBittorrent directory cleanup because category path {CategoryRoot} is outside download root {DownloadRoot}",
                    categoryRoot,
                    downloadRoot);
                return null;
            }

            var jobRoot = NormalizeFullPath(fileSystem.Path.Combine(
                categoryRoot,
                DownloadHelper.RemoveInvalidPathChars(torrent.RdName)));

            if (!IsStrictDescendant(jobRoot, categoryRoot))
            {
                logger.LogWarning(
                    "Skipping qBittorrent directory cleanup because job path {JobRoot} is outside category root {CategoryRoot}",
                    jobRoot,
                    categoryRoot);
                return null;
            }

            var candidates = new HashSet<string>(PathComparer)
            {
                jobRoot
            };

            foreach (var download in torrent.Downloads)
            {
                var relativePath = DownloadHelper.GetDownloadPath(torrent, download);

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                var filePath = NormalizeFullPath(fileSystem.Path.Combine(categoryRoot, relativePath));
                var directory = fileSystem.Path.GetDirectoryName(filePath);

                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                directory = NormalizeFullPath(directory);

                if (!IsSameOrDescendant(directory, jobRoot))
                {
                    logger.LogWarning(
                        "Skipping qBittorrent directory cleanup because download path {DownloadPath} is outside job root {JobRoot}",
                        directory,
                        jobRoot);
                    return null;
                }

                candidates.Add(directory);
            }

            return new(
                downloadRoot,
                categoryRoot,
                candidates.OrderByDescending(path => path.Length).ToList());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to safely resolve qBittorrent cleanup paths for {TorrentHash}", torrent.Hash);
            return null;
        }
    }

    private void CleanupEmptyJobDirectories(EmptyDirectoryCleanupPlan plan)
    {
        foreach (var candidate in plan.CandidateDirectories)
        {
            var current = candidate;

            while (IsStrictDescendant(current, plan.CategoryRoot))
            {
                if (!IsSameOrDescendant(current, plan.DownloadRoot) ||
                    HasReparsePoint(current, plan.DownloadRoot))
                {
                    logger.LogWarning("Skipping unsafe qBittorrent directory cleanup path {CleanupPath}", current);
                    break;
                }

                if (fileSystem.Directory.Exists(current))
                {
                    try
                    {
                        if (fileSystem.Directory.EnumerateFileSystemEntries(current).Any())
                        {
                            break;
                        }

                        fileSystem.Directory.Delete(current, false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Unable to remove empty qBittorrent job directory {CleanupPath}", current);
                        break;
                    }
                }

                var parent = fileSystem.Path.GetDirectoryName(current);

                if (string.IsNullOrWhiteSpace(parent) || PathsEqual(parent, current))
                {
                    break;
                }

                current = NormalizeFullPath(parent);
            }
        }
    }

    private bool HasReparsePoint(string path, string boundary)
    {
        var current = path;

        while (IsSameOrDescendant(current, boundary))
        {
            if (fileSystem.Directory.Exists(current))
            {
                try
                {
                    if (fileSystem.File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unable to inspect qBittorrent cleanup path {CleanupPath}", current);
                    return true;
                }
            }

            if (PathsEqual(current, boundary))
            {
                break;
            }

            var parent = fileSystem.Path.GetDirectoryName(current);

            if (string.IsNullOrWhiteSpace(parent) || PathsEqual(parent, current))
            {
                break;
            }

            current = NormalizeFullPath(parent);
        }

        return false;
    }

    private static string NormalizeFullPath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool IsSameOrDescendant(string path, string root)
    {
        return PathsEqual(path, root) || IsStrictDescendant(path, root);
    }

    private static bool IsStrictDescendant(string path, string root)
    {
        var normalizedPath = NormalizeFullPath(path);
        var normalizedRoot = NormalizeFullPath(root);
        var rootPrefix = Path.EndsInDirectorySeparator(normalizedRoot)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(rootPrefix, PathComparison);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizeFullPath(left), NormalizeFullPath(right), PathComparison);
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private sealed record EmptyDirectoryCleanupPlan(
        string DownloadRoot,
        string CategoryRoot,
        IReadOnlyList<string> CandidateDirectories);

    private static Torrent CreateTorrent(string? category)
    {
        var defaults = Settings.Get.DownloadClient.Default;

        return new()
        {
            Category = string.IsNullOrWhiteSpace(category) ? defaults.Category : category.Trim(),
            DownloadClient = Data.Enums.DownloadClient.Internal,
            DownloadAction = defaults.OnlyDownloadAvailableFiles
                ? TorrentDownloadAction.DownloadAvailableFiles
                : TorrentDownloadAction.DownloadAll,
            HostDownloadAction = defaults.HostDownloadAction,
            FinishedAction = defaults.FinishedAction,
            FinishedActionDelay = defaults.FinishedActionDelay,
            DownloadMinSize = defaults.MinFileSize,
            IncludeRegex = defaults.IncludeRegex,
            ExcludeRegex = defaults.ExcludeRegex,
            TorrentRetryAttempts = defaults.TorrentRetryAttempts,
            DownloadRetryAttempts = defaults.DownloadRetryAttempts,
            DeleteOnError = defaults.DeleteOnError,
            Lifetime = defaults.TorrentLifetime,
            Priority = defaults.Priority > 0 ? defaults.Priority : null
        };
    }

    private static QBittorrentTorrentInfo MapTorrent(Torrent torrent)
    {
        var providerProgress = Math.Clamp((torrent.RdProgress ?? 0) / 100d, 0d, 1d);
        var localProgress = torrent.Downloads.Count == 0
            ? 0d
            : torrent.Downloads.Average(download =>
                download.Completed.HasValue
                    ? 1d
                    : download.BytesTotal > 0
                        ? Math.Clamp((double)download.BytesDone / download.BytesTotal, 0d, 1d)
                        : 0d);

        var completed = torrent.Completed.HasValue && string.IsNullOrWhiteSpace(torrent.Error);
        var progress = completed ? 1d : Math.Min(0.999d, (providerProgress + localProgress) / 2d);
        var localSpeed = torrent.Downloads.Sum(download => Math.Max(0, download.Speed));
        var downloadSpeed = torrent.Downloads.Count > 0 ? localSpeed : Math.Max(0, torrent.RdSpeed ?? 0);
        var activeDownloadSize = torrent.Downloads.Sum(download => Math.Max(0, download.BytesTotal));
        var size = Math.Max(0, torrent.RdSize ?? activeDownloadSize);
        var savePath = GetSavePath(torrent.Category);

        return new()
        {
            Hash = torrent.Hash,
            Name = torrent.RdName ?? torrent.Hash,
            Category = torrent.Category ?? string.Empty,
            Progress = progress,
            State = GetState(torrent, completed, downloadSpeed),
            SavePath = savePath,
            ContentPath = GetContentPath(torrent, savePath),
            Size = size,
            DownloadSpeed = downloadSpeed,
            Eta = GetEta(completed, size, progress, downloadSpeed)
        };
    }

    private static string GetState(Torrent torrent, bool completed, long downloadSpeed)
    {
        if (!string.IsNullOrWhiteSpace(torrent.Error) || torrent.RdStatus == TorrentStatus.Error)
        {
            return "error";
        }

        if (completed)
        {
            return "pausedUP";
        }

        if (downloadSpeed > 0)
        {
            return "downloading";
        }

        return torrent.RdStatus switch
        {
            TorrentStatus.Processing or TorrentStatus.WaitingForFileSelection => "metaDL",
            TorrentStatus.Downloading when torrent.RdSeeders < 1 => "stalledDL",
            TorrentStatus.Downloading => "downloading",
            TorrentStatus.Uploading => "uploading",
            _ => "queuedDL"
        };
    }

    private static long GetEta(bool completed, long size, double progress, long downloadSpeed)
    {
        if (completed)
        {
            return 0;
        }

        if (size <= 0 || downloadSpeed <= 0 || progress <= 0)
        {
            return UnknownEta;
        }

        var eta = Math.Ceiling(size * (1d - progress) / downloadSpeed);
        return (long)Math.Clamp(eta, 0d, UnknownEta);
    }

    private static string GetSavePath(string? category)
    {
        var mappedPath = string.IsNullOrWhiteSpace(Settings.Get.Paths.MappedPath)
            ? Settings.Get.Paths.DownloadPath
            : Settings.Get.Paths.MappedPath;

        return CombineMappedPath(mappedPath, category);
    }

    private static string GetContentPath(Torrent torrent, string savePath)
    {
        if (torrent.Downloads.Count == 1)
        {
            var relativePath = DownloadHelper.GetDownloadPath(torrent, torrent.Downloads[0]);

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                return CombineMappedPath(savePath, relativePath);
            }
        }

        return string.IsNullOrWhiteSpace(torrent.RdName)
            ? savePath
            : CombineMappedPath(savePath, DownloadHelper.RemoveInvalidPathChars(torrent.RdName));
    }

    private static string CombineMappedPath(string root, string? child)
    {
        var separator = root.Contains('\\') && !root.Contains('/')
            ? '\\'
            : root.Contains('/') && !root.Contains('\\')
                ? '/'
                : Path.DirectorySeparatorChar;

        var result = root.Trim().TrimEnd('/', '\\');
        var rootOnly = result.Length == 0 && root.IndexOfAny(['/', '\\']) >= 0;

        if (rootOnly)
        {
            result = separator.ToString();
        }

        if (string.IsNullOrWhiteSpace(child))
        {
            return result;
        }

        var normalizedChild = child.Trim().Trim('/', '\\')
                                   .Replace('/', separator)
                                   .Replace('\\', separator);

        if (result == separator.ToString())
        {
            return result + normalizedChild;
        }

        return result.Length == 0 ? normalizedChild : $"{result}{separator}{normalizedChild}";
    }
}
