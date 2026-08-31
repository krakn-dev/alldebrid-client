using AdbClient.Service.Models.QBittorrent;

namespace AdbClient.Service.Services;

public interface IQBittorrentCompatibility
{
    Task<bool> Login(string userName, string password);
    Task CreateCategory(string category);
    Task Add(string urls, string? category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QBittorrentTorrentInfo>> GetTorrents(string? category);
    Task Delete(string hashes, bool deleteFiles);
}
