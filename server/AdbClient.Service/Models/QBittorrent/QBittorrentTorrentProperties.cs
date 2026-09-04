using System.Text.Json.Serialization;

namespace AdbClient.Service.Models.QBittorrent;

public sealed class QBittorrentTorrentProperties
{
    [JsonPropertyName("hash")]
    public required string Hash { get; init; }

    [JsonPropertyName("save_path")]
    public required string SavePath { get; init; }

    [JsonPropertyName("seeding_time")]
    public long SeedingTime { get; init; }
}
