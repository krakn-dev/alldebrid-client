using System.Text.Json.Serialization;

namespace AdbClient.Service.Models.QBittorrent;

public sealed class QBittorrentPreferences
{
    [JsonPropertyName("save_path")]
    public required string SavePath { get; init; }

    [JsonPropertyName("max_ratio_enabled")]
    public bool MaxRatioEnabled { get; init; }

    [JsonPropertyName("max_ratio")]
    public float MaxRatio { get; init; } = -1;

    [JsonPropertyName("max_seeding_time_enabled")]
    public bool MaxSeedingTimeEnabled { get; init; }

    [JsonPropertyName("max_seeding_time")]
    public long MaxSeedingTime { get; init; } = -1;

    [JsonPropertyName("max_inactive_seeding_time_enabled")]
    public bool MaxInactiveSeedingTimeEnabled { get; init; }

    [JsonPropertyName("max_inactive_seeding_time")]
    public long MaxInactiveSeedingTime { get; init; } = -1;

    [JsonPropertyName("max_ratio_act")]
    public int MaxRatioAction { get; init; }

    [JsonPropertyName("queueing_enabled")]
    public bool QueueingEnabled { get; init; } = true;

    [JsonPropertyName("dht")]
    public bool DhtEnabled { get; init; } = true;
}
