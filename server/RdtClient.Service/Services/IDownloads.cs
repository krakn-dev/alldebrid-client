using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Services;

public interface IDownloads
{
    Task<List<Download>> GetForTorrent(Guid torrentId);
    Task<Download?> GetById(Guid downloadId);
    Task<Download?> Get(Guid torrentId, string path);
    Task<Download> Add(Guid torrentId, DownloadInfo downloadInfo);
    Task UpdateUnrestrictedLink(Guid downloadId, string unrestrictedLink);
    Task UpdateFileName(Guid downloadId, string fileName);
    Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime);
    Task UpdateError(Guid downloadId, string? error);
    Task UpdateRetryCount(Guid downloadId, int retryCount);
    Task UpdateRemoteId(Guid downloadId, string remoteId);
    Task DeleteForTorrent(Guid torrentId);
    Task Reset(Guid downloadId);
}
