using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AdbClient.Data.Models.Data;

namespace AdbClient.Service.Services;

public interface IDownloadableFileFilter
{
    public bool IsDownloadable(Torrent torrent, string filePath, long fileSize);
}

public class DownloadableFileFilter(ILogger<DownloadableFileFilter> logger) : IDownloadableFileFilter
{
    public bool IsDownloadable(Torrent torrent, string filePath, long fileSize)
    {
        var isDownloadable = PassesSizeFilter(torrent, filePath, fileSize) &&
                             PassesFilePathFilter(torrent, filePath);

        if (isDownloadable)
        {
            logger.LogDebug("File {filePath} was included after filtering", filePath);
        }
        
        return isDownloadable;
    }

    private bool PassesSizeFilter(Torrent torrent, string filePath, long fileSize)
    {
        if (torrent.DownloadMinSize <= 0 || fileSize > torrent.DownloadMinSize * 1024 * 1024)
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} file size {fileSize} smaller than minimum {downloadMinSize}", filePath, fileSize, torrent.DownloadMinSize);

        return false;
    }

    private bool PassesFilePathFilter(Torrent torrent, string filePath)
    {
        return PassesIncludeRegexFilter(torrent, filePath) && PassesExcludeRegexFilter(torrent, filePath);
    }

    private bool PassesIncludeRegexFilter(Torrent torrent, string filePath)
    {
        if (string.IsNullOrWhiteSpace(torrent.IncludeRegex) || Regex.IsMatch(filePath, torrent.IncludeRegex))
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} does not match regex {includeRegex}", filePath, torrent.IncludeRegex);

        return false;
    }

    private bool PassesExcludeRegexFilter(Torrent torrent, string filePath)
    {
        // If the IncludeRegex is set, ignore the ExcludeRegex 
        if (!string.IsNullOrWhiteSpace(torrent.IncludeRegex))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(torrent.ExcludeRegex) || !Regex.IsMatch(filePath, torrent.ExcludeRegex))
        {
            return true;
        }

        logger.LogDebug("Not downloading file {filePath} matches regex {excludeRegex}", filePath, torrent.ExcludeRegex);

        return false;
    }
}
