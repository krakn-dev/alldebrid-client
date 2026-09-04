using System.Text.Json.Serialization;

namespace AdbClient.Service.Models.QBittorrent;

public sealed class QBittorrentCategory
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("savePath")]
    public required string SavePath { get; init; }
}
