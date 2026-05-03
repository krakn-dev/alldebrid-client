using System.IO.Abstractions;
using RdtClient.Data.Models.Data;
using System.Web;

namespace RdtClient.Service.Helpers;

public static class DownloadHelper
{
    public static string? GetDownloadPath(string downloadPath, Torrent torrent, Download download, IFileSystem? fileSystem = null)
    {
        var fileUrl = download.Link;

        if (string.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var directory = RemoveInvalidPathChars(torrent.RdName);
        
        var torrentPath = Path.Combine(downloadPath, directory);

        var fileName = GetFileName(download);

        if (fileName == null)
        {
            return null;
        }

        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !string.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count > 0)
        {
            var matchingTorrentFile = matchingTorrentFiles[0];

            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!string.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                torrentPath = Path.Combine(torrentPath, subPath);
            }
        }

        fileSystem ??= new FileSystem();

        if (!fileSystem.Directory.Exists(torrentPath))
        {
            fileSystem.Directory.CreateDirectory(torrentPath);
        }

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
    }

    public static string? GetDownloadPath(Torrent torrent, Download download)
    {
        var fileUrl = download.Link;

        if (string.IsNullOrWhiteSpace(fileUrl) || torrent.RdName == null)
        {
            return null;
        }

        var uri = new Uri(fileUrl);
        var torrentPath = RemoveInvalidPathChars(torrent.RdName);

        var fileName = download.FileName;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = uri.Segments.Last();

            fileName = HttpUtility.UrlDecode(fileName);
        }

        var matchingTorrentFiles = torrent.Files.Where(m => m.Path.EndsWith(fileName)).Where(m => !string.IsNullOrWhiteSpace(m.Path)).ToList();

        if (matchingTorrentFiles.Count > 0)
        {
            var matchingTorrentFile = matchingTorrentFiles[0];

            var subPath = Path.GetDirectoryName(matchingTorrentFile.Path);

            if (!string.IsNullOrWhiteSpace(subPath))
            {
                subPath = subPath.Trim('/').Trim('\\');

                torrentPath = Path.Combine(torrentPath, subPath);
            }
        }

        var filePath = Path.Combine(torrentPath, fileName);

        return filePath;
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
