using Microsoft.EntityFrameworkCore;
using AdbClient.Data.Models.Data;
using Download = AdbClient.Data.Models.Data.Download;

namespace AdbClient.Data.Data;

public class DownloadData(DataContext dataContext)
{
    public async Task<List<Download>> GetForTorrent(Guid torrentId)
    {
        return await dataContext.Downloads
                                 .AsNoTracking()
                                 .Where(m => m.TorrentId == torrentId)
                                 .ToListAsync();
    }

    public async Task<Download?> GetById(Guid downloadId)
    {
        return await dataContext.Downloads
                                 .Include(m => m.Torrent)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.DownloadId == downloadId);
    }

    public async Task<Download?> Get(Guid torrentId, string path)
    {
        return await dataContext.Downloads
                                 .Include(m => m.Torrent)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.TorrentId == torrentId && m.Path == path);
    }

    public async Task<Download> Add(Guid torrentId, DownloadInfo downloadInfo)
    {
        var download = new Download
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrentId,
            FileName = downloadInfo.FileName,
            Path = downloadInfo.RestrictedLink,
            Added = DateTimeOffset.UtcNow,
            DownloadQueued = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        await dataContext.Downloads.AddAsync(download);
        await dataContext.SaveChangesAsync();
        await TorrentData.VoidCache();

        return download;
    }

    public Task UpdateUnrestrictedLink(Guid downloadId, string unrestrictedLink) =>
        Patch(downloadId, d => d.Link = unrestrictedLink);

    public Task UpdateFileName(Guid downloadId, string fileName) =>
        Patch(downloadId, d => d.FileName = fileName);

    public Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.DownloadStarted = dateTime);

    public Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.DownloadFinished = dateTime);

    public Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.UnpackingQueued = dateTime);

    public Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.UnpackingStarted = dateTime);

    public Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.UnpackingFinished = dateTime);

    public Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime) =>
        Patch(downloadId, d => d.Completed = dateTime);

    public Task UpdateError(Guid downloadId, string? error) =>
        Patch(downloadId, d => d.Error = error);

    public Task UpdateRetryCount(Guid downloadId, int retryCount) =>
        Patch(downloadId, d => d.RetryCount = retryCount);

    public Task UpdateRemoteId(Guid downloadId, string remoteId) =>
        Patch(downloadId, d => d.RemoteId = remoteId);

    public async Task DeleteForTorrent(Guid torrentId)
    {
        var downloads = await dataContext.Downloads
                                          .Where(m => m.TorrentId == torrentId)
                                          .ToListAsync();

        dataContext.Downloads.RemoveRange(downloads);
        await dataContext.SaveChangesAsync();
        await TorrentData.VoidCache();
    }

    public async Task Reset(Guid downloadId)
    {
        var dbDownload = await dataContext.Downloads
                                           .FirstOrDefaultAsync(m => m.DownloadId == downloadId)
                         ?? throw new Exception($"Cannot find download with ID {downloadId}");

        dbDownload.RetryCount = 0;
        dbDownload.Link = null;
        dbDownload.Added = DateTimeOffset.UtcNow;
        dbDownload.DownloadQueued = DateTimeOffset.UtcNow;
        dbDownload.DownloadStarted = null;
        dbDownload.DownloadFinished = null;
        dbDownload.UnpackingQueued = null;
        dbDownload.UnpackingStarted = null;
        dbDownload.UnpackingFinished = null;
        dbDownload.Completed = null;
        dbDownload.Error = null;

        await dataContext.SaveChangesAsync();
        await TorrentData.VoidCache();
    }

    private async Task Patch(Guid downloadId, Action<Download> mutate)
    {
        var db = await dataContext.Downloads.FirstOrDefaultAsync(m => m.DownloadId == downloadId);
        if (db == null) return;
        mutate(db);
        await dataContext.SaveChangesAsync();
        await TorrentData.VoidCache();
    }
}
