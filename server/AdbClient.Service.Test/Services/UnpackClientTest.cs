using AdbClient.Data.Models.Data;
using AdbClient.Service.Services;

namespace AdbClient.Service.Test.Services;

public class UnpackClientTest
{
    [Fact]
    public void ResolveExtractionPath_SanitizesTorrentNameWithinDestination()
    {
        var destination = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "adbclient-unpack-tests"));
        var torrent = new Torrent
        {
            Hash = "0123456789abcdef0123456789abcdef01234567",
            RdName = "../outside"
        };

        var result = UnpackClient.ResolveExtractionPath(destination, torrent, ["movie.mkv"]);

        Assert.StartsWith(
            destination + Path.DirectorySeparatorChar,
            result,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        Assert.Equal(Path.Combine(destination, "..outside"), result);
    }

    [Fact]
    public void ResolveExtractionPath_MixedTorrentAndSiblingRootsRemainInsideJobDirectory()
    {
        var destination = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "adbclient-unpack-tests"));
        var torrent = new Torrent
        {
            Hash = "0123456789abcdef0123456789abcdef01234567",
            RdName = "Movie"
        };

        var result = UnpackClient.ResolveExtractionPath(
            destination,
            torrent,
            ["Movie/movie.mkv", "OtherJob/overwrite.mkv"]);

        Assert.Equal(Path.Combine(destination, "Movie"), result);
    }

}
