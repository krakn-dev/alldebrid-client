namespace AdbClient.Data.Models.TorrentClient;

public class TorrentClientFile
{
    public long Id { get; set; }
    public string Path { get; set; } = default!;
    public long Bytes { get; set; }
    public bool Selected { get; set; }
    public string? DownloadLink { get; set; }
}