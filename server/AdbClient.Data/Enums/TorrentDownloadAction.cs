using System.ComponentModel;

namespace AdbClient.Data.Enums;

public enum TorrentDownloadAction
{
    [Description("Download All Files")]
    DownloadAll = 0,

    [Description("Download All Available Files")]
    DownloadAvailableFiles = 1,

    [Description("Manually Select Files")]
    DownloadManual = 2
}