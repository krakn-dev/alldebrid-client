using System.Text.Json.Serialization;

namespace AdbClient.Service.Models.QBittorrent;

public sealed class QBittorrentTorrentFile
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
