using System.Text.Json.Serialization;

namespace AdbClient.Service.Models.QBittorrent;

public sealed class QBittorrentTorrentInfo
{
    [JsonPropertyName("hash")]
    public required string Hash { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("progress")]
    public double Progress { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("content_path")]
    public required string ContentPath { get; init; }

    [JsonPropertyName("save_path")]
    public required string SavePath { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("dlspeed")]
    public long DownloadSpeed { get; init; }

    [JsonPropertyName("eta")]
    public long Eta { get; init; }

    [JsonPropertyName("ratio")]
    public double Ratio { get; init; }

    [JsonPropertyName("ratio_limit")]
    public double RatioLimit { get; init; } = -1;

    [JsonPropertyName("seeding_time")]
    public long SeedingTime { get; init; }

    [JsonPropertyName("seeding_time_limit")]
    public long SeedingTimeLimit { get; init; } = -1;

    [JsonPropertyName("inactive_seeding_time_limit")]
    public long InactiveSeedingTimeLimit { get; init; } = -1;

    [JsonPropertyName("last_activity")]
    public long LastActivity { get; init; }
}
