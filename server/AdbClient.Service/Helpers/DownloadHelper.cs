using System.IO.Abstractions;
using AdbClient.Data.Models.Data;
using System.Web;

namespace AdbClient.Service.Helpers;

public static class DownloadHelper
{
    public static string? GetDownloadPath(string downloadPath, Torrent torrent, Download download, IFileSystem? fileSystem = null)
    {
        if (string.IsNullOrWhiteSpace(download.Link) || torrent.RdName == null)
        {
            return null;
        }

        var fileName = GetFileName(download);

        if (fileName == null)
        {
            return null;
        }

        var torrentPath = Path.Combine(downloadPath, RemoveInvalidPathChars(torrent.RdName));

        var subPath = FindSubPath(torrent, fileName);
        if (subPath != null)
        {
            torrentPath = Path.Combine(torrentPath, subPath);
        }

        fileSystem ??= new FileSystem();

        if (!fileSystem.Directory.Exists(torrentPath))
        {
            fileSystem.Directory.CreateDirectory(torrentPath);
        }

        return Path.Combine(torrentPath, fileName);
    }

    public static string? GetDownloadPath(Torrent torrent, Download download)
    {
        if (string.IsNullOrWhiteSpace(download.Link) || torrent.RdName == null)
        {
            return null;
        }

        var fileName = download.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = HttpUtility.UrlDecode(new Uri(download.Link).Segments.Last());
        }

        var torrentPath = RemoveInvalidPathChars(torrent.RdName);

        var subPath = FindSubPath(torrent, fileName);
        if (subPath != null)
        {
            torrentPath = Path.Combine(torrentPath, subPath);
        }

        return Path.Combine(torrentPath, fileName);
    }

    private static string? FindSubPath(Torrent torrent, string fileName)
    {
        var match = torrent.Files.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Path) && f.Path.EndsWith(fileName));
        if (match == null) return null;
        var sub = Path.GetDirectoryName(match.Path);
        return string.IsNullOrWhiteSpace(sub) ? null : sub.Trim('/').Trim('\\');
    }

    public static string? GetFileName(Download download)
    {
        if (string.IsNullOrWhiteSpace(download.Link))
        {
            return null;
        }

        var fileName = download.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = HttpUtility.UrlDecode(new Uri(download.Link).Segments.Last());
        }

        return FileHelper.RemoveInvalidFileNameChars(fileName);
    }

    public static string RemoveInvalidPathChars(string path)
    {
        return string.Concat(path.Split(Path.GetInvalidPathChars()));
    }
}
