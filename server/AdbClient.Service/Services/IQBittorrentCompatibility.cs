using AdbClient.Service.Models.QBittorrent;

namespace AdbClient.Service.Services;

public interface IQBittorrentCompatibility
{
    Task<bool> Login(string userName, string password);
    QBittorrentPreferences GetPreferences();
    Task<IReadOnlyDictionary<string, QBittorrentCategory>> GetCategories();
    Task CreateCategory(string category);
    Task Add(string urls, string? category, CancellationToken cancellationToken = default);
    Task Add(byte[] torrentBytes, string? category);
    Task<IReadOnlyList<QBittorrentTorrentInfo>> GetTorrents(string? category);
    Task<QBittorrentTorrentProperties?> GetProperties(string hash);
    Task<IReadOnlyList<QBittorrentTorrentFile>?> GetFiles(string hash);
    Task SetCategory(string hashes, string? category);
    Task SetTopPriority(string hashes);
    Task Delete(string hashes, bool deleteFiles);
}
