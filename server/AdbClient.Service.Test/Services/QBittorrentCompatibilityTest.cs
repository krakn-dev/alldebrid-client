using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using AdbClient.Data.Data;
using AdbClient.Data.Enums;
using AdbClient.Data.Models.Data;
using AdbClient.Service.Services;
using AdbClient.Service.Wrappers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using DownloadClientKind = AdbClient.Data.Enums.DownloadClient;

namespace AdbClient.Service.Test.Services;

public class QBittorrentCompatibilityTest
{
    [Fact]
    public async Task GetTorrents_MapsLogposeFieldsAndFiltersCategory()
    {
        var originalMappedPath = Settings.Get.Paths.MappedPath;
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;

        try
        {
            Settings.Get.Paths.MappedPath = "/media/downloads";
            Settings.Get.Paths.DownloadPath = @"D:\Downloads";

            var torrent = new Torrent
            {
                TorrentId = Guid.NewGuid(),
                Hash = "0123456789abcdef0123456789abcdef01234567",
                Category = "logpose",
                RdName = "One Pace Episode 01",
                RdSize = 400,
                RdProgress = 100,
                RdStatus = TorrentStatus.Finished,
                Downloads =
                [
                    new()
                    {
                        FileName = "episode.mkv",
                        Link = "https://example.test/episode.mkv",
                        BytesTotal = 400,
                        BytesDone = 100,
                        Speed = 50
                    }
                ]
            };

            var otherCategory = new Torrent
            {
                TorrentId = Guid.NewGuid(),
                Hash = "fedcba9876543210fedcba9876543210fedcba98",
                Category = "other",
                RdName = "Other"
            };

            var torrentData = new Mock<ITorrentData>();
            torrentData.Setup(data => data.Get()).ReturnsAsync([torrent, otherCategory]);

            var compatibility = CreateCompatibility(torrentData: torrentData);

            var result = await compatibility.GetTorrents("logpose");

            var info = Assert.Single(result);
            Assert.Equal(torrent.Hash, info.Hash);
            Assert.Equal("One Pace Episode 01", info.Name);
            Assert.Equal("logpose", info.Category);
            Assert.Equal(0.625d, info.Progress, 3);
            Assert.Equal("downloading", info.State);
            Assert.Equal("/media/downloads/logpose", info.SavePath);
            Assert.Equal("/media/downloads/logpose/One Pace Episode 01/episode.mkv", info.ContentPath);
            Assert.Equal(400, info.Size);
            Assert.Equal(50, info.DownloadSpeed);
            Assert.Equal(3, info.Eta);
        }
        finally
        {
            Settings.Get.Paths.MappedPath = originalMappedPath;
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task GetTorrents_ReportsCompletionOnlyAfterHostDownloadCompletes()
    {
        var torrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Hash = "0123456789abcdef0123456789abcdef01234567",
            Category = "logpose",
            RdName = "One Pace Episode 01",
            RdSize = 400,
            RdProgress = 100,
            RdStatus = TorrentStatus.Finished,
            Completed = DateTimeOffset.UtcNow,
            Downloads =
            [
                new()
                {
                    FileName = "episode.mkv",
                    Link = "https://example.test/episode.mkv",
                    Completed = DateTimeOffset.UtcNow
                }
            ]
        };

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.Get()).ReturnsAsync([torrent]);

        var compatibility = CreateCompatibility(torrentData: torrentData);
        var info = Assert.Single(await compatibility.GetTorrents("logpose"));

        Assert.Equal(1d, info.Progress);
        Assert.Equal("pausedUP", info.State);
        Assert.Equal(0, info.Eta);
    }

    [Fact]
    public async Task AddMagnet_UsesHostDownloadAndWaitsForLogposeCleanup()
    {
        const string magnet = "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=One%20Pace";

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(It.IsAny<string>())).ReturnsAsync((Torrent?)null);
        torrentData.Setup(data => data.Add(
                       It.IsAny<string?>(),
                       It.IsAny<string>(),
                       It.IsAny<string?>(),
                       It.IsAny<bool>(),
                       It.IsAny<DownloadClientKind>(),
                       It.IsAny<Torrent>()))
                   .ReturnsAsync((string? _, string hash, string? _, bool _, DownloadClientKind _, Torrent torrent) =>
                   {
                       torrent.Hash = hash;
                       return torrent;
                   });

        var enricher = new Mock<IEnricher>();
        enricher.Setup(value => value.EnrichMagnetLink(magnet)).ReturnsAsync(magnet);

        var compatibility = CreateCompatibility(torrentData: torrentData, enricher: enricher);

        await compatibility.Add(magnet, "logpose");

        torrentData.Verify(data => data.Add(
            null,
            It.Is<string>(hash => string.Equals(
                hash,
                "0123456789abcdef0123456789abcdef01234567",
                StringComparison.OrdinalIgnoreCase)),
            magnet,
            false,
            DownloadClientKind.Internal,
            It.Is<Torrent>(torrent =>
                torrent.Category == "logpose" &&
                torrent.DownloadAction == TorrentDownloadAction.DownloadAll &&
                torrent.HostDownloadAction == TorrentHostDownloadAction.DownloadAll &&
                torrent.FinishedAction == TorrentFinishedAction.None &&
                torrent.FinishedActionDelay == 0 &&
                torrent.DownloadMinSize == 0 &&
                torrent.IncludeRegex == null &&
                torrent.ExcludeRegex == null)), Times.Once);
    }

    [Fact]
    public async Task AddTorrentUrl_DownloadsAndAddsTorrentFile()
    {
        const string torrentUrl = "https://example.test/one-pace.torrent";
        var torrentBytes = Encoding.Latin1.GetBytes(
            "d4:infod6:lengthi1e4:name11:episode.mkv12:piece lengthi16384e6:pieces20:00000000000000000000ee");

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(It.IsAny<string>())).ReturnsAsync((Torrent?)null);
        torrentData.Setup(data => data.Add(
                       It.IsAny<string?>(),
                       It.IsAny<string>(),
                       It.IsAny<string?>(),
                       It.IsAny<bool>(),
                       It.IsAny<DownloadClientKind>(),
                       It.IsAny<Torrent>()))
                   .ReturnsAsync((string? _, string hash, string? _, bool _, DownloadClientKind _, Torrent torrent) =>
                   {
                       torrent.Hash = hash;
                       return torrent;
                   });

        var enricher = new Mock<IEnricher>();
        enricher.Setup(value => value.EnrichTorrentBytes(torrentBytes)).ReturnsAsync(torrentBytes);

        var handler = new StubHttpMessageHandler(torrentBytes);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                         .Returns(new HttpClient(handler));

        var compatibility = CreateCompatibility(
            torrentData: torrentData,
            enricher: enricher,
            httpClientFactory: httpClientFactory);

        await compatibility.Add(torrentUrl, "logpose");

        Assert.Equal(new Uri(torrentUrl), handler.RequestUri);
        torrentData.Verify(data => data.Add(
            null,
            It.IsAny<string>(),
            It.Is<string>(value => value == Convert.ToBase64String(torrentBytes)),
            true,
            DownloadClientKind.Internal,
            It.Is<Torrent>(torrent => torrent.Category == "logpose")), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_PersistsNewCategoryWithoutCaseDuplicates()
    {
        var originalCategories = Settings.Get.General.Categories;

        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<DataContext>()
                          .UseSqlite(connection)
                          .Options;

            await using var dataContext = new DataContext(options);
            await dataContext.Database.EnsureCreatedAsync();
            dataContext.Settings.Add(new Setting
            {
                SettingId = "General:Categories",
                Value = "movies,logpose"
            });
            await dataContext.SaveChangesAsync();

            var settingData = new SettingData(dataContext, Mock.Of<ILogger<SettingData>>());
            await settingData.ResetCache();

            var compatibility = CreateCompatibility(settings: new Settings(settingData));

            await compatibility.CreateCategory("LOGPOSE");
            await compatibility.CreateCategory("anime");

            var stored = await dataContext.Settings.AsNoTracking()
                                          .SingleAsync(setting => setting.SettingId == "General:Categories");
            Assert.Equal("movies,logpose,anime", stored.Value);
        }
        finally
        {
            Settings.Get.General.Categories = originalCategories;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_RemovesClientRecord()
    {
        var torrentId = Guid.NewGuid();
        var torrent = new Torrent
        {
            TorrentId = torrentId,
            Hash = "0123456789abcdef0123456789abcdef01234567",
            Category = "logpose",
            RdName = "One Pace Episode 01"
        };

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(torrent.Hash)).ReturnsAsync(torrent);
        torrentData.Setup(data => data.GetById(torrentId)).ReturnsAsync(torrent);

        var downloads = new Mock<IDownloads>();
        var compatibility = CreateCompatibility(torrentData, downloads);

        await compatibility.Delete(torrent.Hash, false);

        downloads.Verify(value => value.DeleteForTorrent(torrentId), Times.Once);
        torrentData.Verify(value => value.Delete(torrentId), Times.Once);
    }

    private static QBittorrentCompatibility CreateCompatibility(
        Mock<ITorrentData>? torrentData = null,
        Mock<IDownloads>? downloads = null,
        Mock<IEnricher>? enricher = null,
        Mock<IHttpClientFactory>? httpClientFactory = null,
        MockFileSystem? fileSystem = null,
        Settings? settings = null)
    {
        torrentData ??= new();
        downloads ??= new();
        enricher ??= new();
        httpClientFactory ??= new();
        fileSystem ??= new();

        var processFactory = new Mock<IProcessFactory>();
        var torrents = new Torrents(
            Mock.Of<ILogger<Torrents>>(),
            torrentData.Object,
            downloads.Object,
            processFactory.Object,
            fileSystem,
            enricher.Object,
            null!);

        return new(
            Mock.Of<ILogger<QBittorrentCompatibility>>(),
            null!,
            settings!,
            torrents,
            httpClientFactory.Object);
    }

    private sealed class StubHttpMessageHandler(byte[] responseBytes) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(responseBytes)
            });
        }
    }
}
