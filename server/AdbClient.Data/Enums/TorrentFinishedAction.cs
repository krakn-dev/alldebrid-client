using System.ComponentModel;

namespace AdbClient.Data.Enums;

public enum TorrentFinishedAction
{
    [Description("No Action")]
    None = 0,

    [Description("Remove Torrent From Client And Provider")]
    RemoveAllTorrents = 1,

    [Description("Remove Torrent From Provider")]
    RemoveProvider = 2,
    
    [Description("Remove Torrent From Client")]
    RemoveClient = 3
}
