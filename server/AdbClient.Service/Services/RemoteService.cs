using Microsoft.AspNetCore.SignalR;

namespace AdbClient.Service.Services;

public class RemoteService(IHubContext<AdbHub> hub, Torrents torrents)
{
    public async Task Update()
    {
        var allTorrents = await torrents.Get();
            
        // Prevent infinite recursion when serializing
        foreach (var file in allTorrents.SelectMany(torrent => torrent.Downloads))
        {
            file.Torrent = null;
        }
            
        await hub.Clients.All.SendCoreAsync("update",
        [
            allTorrents
        ]);
    }
}