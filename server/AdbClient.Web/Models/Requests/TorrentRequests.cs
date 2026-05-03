using Torrent = AdbClient.Data.Models.Data.Torrent;

namespace AdbClient.Web.Models.Requests;

public class TorrentControllerUploadFileRequest
{
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerUploadMagnetRequest
{
    public string? MagnetLink { get; set; }
    public Torrent? Torrent { get; set; }
}

public class TorrentControllerDeleteRequest
{
    public bool DeleteData { get; set; }
    public bool DeleteRdTorrent { get; set; }
    public bool DeleteLocalFiles { get; set; }
}

public class TorrentControllerCheckFilesRequest
{
    public string? MagnetLink { get; set; }
}

public class TorrentControllerVerifyRegexRequest
{
    public string? IncludeRegex { get; set; }
    public string? ExcludeRegex { get; set; }
    public string? MagnetLink { get; set; }
}
