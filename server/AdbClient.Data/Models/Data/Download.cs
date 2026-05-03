using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdbClient.Data.Models.Data;

public class Download
{
    [Key]
    public Guid DownloadId { get; set; }

    public Guid TorrentId { get; set; }

    [ForeignKey("TorrentId")]
    public Torrent? Torrent { get; set; }

    public string Path { get; set; } = null!;
    public string? Link { get; set; }

    public DateTimeOffset Added { get; set; }
    public DateTimeOffset? DownloadQueued { get; set; }
    public DateTimeOffset? DownloadStarted { get; set; }
    public DateTimeOffset? DownloadFinished { get; set; }
    public DateTimeOffset? UnpackingQueued { get; set; }
    public DateTimeOffset? UnpackingStarted { get; set; }
    public DateTimeOffset? UnpackingFinished { get; set; }
    public DateTimeOffset? Completed { get; set; }

    public int RetryCount { get; set; }

    public string? Error { get; set; }

    public string? RemoteId { get; set; }

    public string? FileName { get; set; }

    [NotMapped]
    public long BytesTotal { get; set; }

    [NotMapped]
    public long BytesDone { get; set; }

    [NotMapped]
    public long Speed { get; set; }
}

/// <summary>
/// Used to create <see cref="Download"/>s
/// </summary>
public class DownloadInfo
{
    /// <summary>
    /// The name of the file. Should not include directory.
    /// If the filename is not known, set tn null and `GetFileName` will be called with the unrestricted link.
    /// </summary>
    public required string? FileName;
    /// <summary>
    /// The restricted link to download this download. If the debrid serice in question does not have restricted links, use either a fake or the unrestricted link
    /// </summary>
    public required string RestrictedLink;
}