using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using System.Text.Json;
using AdbClient.Data.Data;
using AdbClient.Data.Enums;
using AdbClient.Data.Models.Data;
using AdbClient.Data.Models.Internal;
using AdbClient.Data.Models.TorrentClient;
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
    public async Task AddMagnet_UsesExposedDownloadDefaults()
    {
        const string magnet = "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=One%20Pace";
        var originalDefaults = Settings.Get.DownloadClient.Default;
        Settings.Get.DownloadClient.Default = new DbSettingsDefaultsWithCategory
        {
            Category = "from-defaults",
            OnlyDownloadAvailableFiles = true,
            HostDownloadAction = TorrentHostDownloadAction.DownloadNone,
            FinishedAction = TorrentFinishedAction.RemoveClient,
            FinishedActionDelay = 7,
            MinFileSize = 123,
            IncludeRegex = @"\.mkv$",
            ExcludeRegex = null,
            TorrentRetryAttempts = 4,
            DownloadRetryAttempts = 5,
            DeleteOnError = 6,
            TorrentLifetime = 8,
            Priority = 2
        };

        try
        {
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

            await compatibility.Add(magnet, null);

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
                    torrent.Category == "from-defaults" &&
                    torrent.DownloadAction == TorrentDownloadAction.DownloadAvailableFiles &&
                    torrent.HostDownloadAction == TorrentHostDownloadAction.DownloadNone &&
                    torrent.FinishedAction == TorrentFinishedAction.RemoveClient &&
                    torrent.FinishedActionDelay == 7 &&
                    torrent.DownloadMinSize == 123 &&
                    torrent.IncludeRegex == @"\.mkv$" &&
                    torrent.ExcludeRegex == null &&
                    torrent.TorrentRetryAttempts == 4 &&
                    torrent.DownloadRetryAttempts == 5 &&
                    torrent.DeleteOnError == 6 &&
                    torrent.Lifetime == 8 &&
                    torrent.Priority == 2)), Times.Once);
        }
        finally
        {
            Settings.Get.DownloadClient.Default = originalDefaults;
        }
    }

    [Theory]
    [InlineData("https://nyaa.si/?q=a031cac6baf81b804c4d034dfaef0e5e4a671145")]
    [InlineData("https://nyaa.si/?q=A031CAC6BAF81B804C4D034DFAEF0E5E4A671145")]
    public async Task AddNyaaInfoHashSearchUrl_QueuesCanonicalMagnetAndPreservesCategory(string searchUrl)
    {
        const string infoHash = "a031cac6baf81b804c4d034dfaef0e5e4a671145";
        const string storedHash = "A031CAC6BAF81B804C4D034DFAEF0E5E4A671145";
        const string magnet = $"magnet:?xt=urn:btih:{infoHash}";

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(storedHash)).ReturnsAsync((Torrent?)null);
        torrentData.Setup(data => data.Add(
                       It.IsAny<string?>(),
                       storedHash,
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

        var httpClientFactory = new Mock<IHttpClientFactory>();
        var compatibility = CreateCompatibility(
            torrentData: torrentData,
            enricher: enricher,
            httpClientFactory: httpClientFactory);

        await compatibility.Add(searchUrl, "logpose");

        enricher.Verify(value => value.EnrichMagnetLink(magnet), Times.Once);
        httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
        torrentData.Verify(data => data.Add(
            null,
            storedHash,
            magnet,
            false,
            DownloadClientKind.Internal,
            It.Is<Torrent>(torrent => torrent.Category == "logpose")), Times.Once);
    }

    [Theory]
    [InlineData("https://nyaa.si/?q=a031cac6baf81b804c4d034dfaef0e5e4a67114")]
    [InlineData("https://nyaa.si/?q=a031cac6baf81b804c4d034dfaef0e5e4a6711450")]
    [InlineData("https://nyaa.si/?q=g031cac6baf81b804c4d034dfaef0e5e4a671145")]
    [InlineData("https://nyaa.si/?q=a031cac6baf81b804c4d034dfaef0e5e4a671145&f=0")]
    public async Task AddNyaaInfoHashSearchUrl_RejectsInvalidQuery(string searchUrl)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var compatibility = CreateCompatibility(httpClientFactory: httpClientFactory);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            compatibility.Add(searchUrl, "logpose"));

        Assert.Contains("Unsupported Nyaa search URL", exception.Message, StringComparison.Ordinal);
        httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddNyaaInfoHashSearchUrl_RetryUsesExistingTorrent()
    {
        const string infoHash = "a031cac6baf81b804c4d034dfaef0e5e4a671145";
        const string storedHash = "A031CAC6BAF81B804C4D034DFAEF0E5E4A671145";
        const string magnet = $"magnet:?xt=urn:btih:{infoHash}";
        const string searchUrl = $"https://nyaa.si/?q={infoHash}";
        var existingTorrent = new Torrent
        {
            TorrentId = Guid.NewGuid(),
            Hash = storedHash,
            Category = "logpose"
        };

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(storedHash)).ReturnsAsync(existingTorrent);

        var enricher = new Mock<IEnricher>();
        enricher.Setup(value => value.EnrichMagnetLink(magnet)).ReturnsAsync(magnet);

        var compatibility = CreateCompatibility(torrentData: torrentData, enricher: enricher);

        await compatibility.Add(searchUrl, "logpose");

        torrentData.Verify(data => data.Add(
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<DownloadClientKind>(),
            It.IsAny<Torrent>()), Times.Never);
    }

    [Fact]
    public async Task AddTorrentUrlOnOtherHost_DownloadsAndAddsTorrentFile()
    {
        const string torrentUrl = "https://example.test/?q=a031cac6baf81b804c4d034dfaef0e5e4a671145";
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

    [Fact]
    public async Task DeleteWithFiles_RemovesClientRecord()
    {
        var torrentId = Guid.NewGuid();
        var torrent = new Torrent
        {
            TorrentId = torrentId,
            Hash = "0123456789abcdef0123456789abcdef01234567",
            Category = "logpose",
            RdName = null
        };

        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(torrent.Hash)).ReturnsAsync(torrent);
        torrentData.Setup(data => data.GetById(torrentId)).ReturnsAsync(torrent);

        var downloads = new Mock<IDownloads>();
        var compatibility = CreateCompatibility(torrentData, downloads);

        await compatibility.Delete(torrent.Hash, true);

        downloads.Verify(value => value.DeleteForTorrent(torrentId), Times.Once);
        torrentData.Verify(value => value.Delete(torrentId), Times.Once);
    }

    [Fact]
    public async Task DeleteWithoutFiles_RemovesEmptySingleFileJobDirectoryAndPreservesRoots()
    {
        const string jobName = "[One Pace][303] Long Ring Long Land 00 [1080p][E85B9E9D].mkv";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var categoryRoot = Path.Combine(downloadRoot, "logpose");
        var jobDirectory = Path.Combine(categoryRoot, jobName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(jobDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, jobName);
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.False(fileSystem.Directory.Exists(jobDirectory));
            Assert.True(fileSystem.Directory.Exists(categoryRoot));
            Assert.True(fileSystem.Directory.Exists(downloadRoot));
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_RemovesNestedEmptyPackDirectoriesBottomUp()
    {
        const string packName = "[One Pace][106-114] Whiskey Peak [480p]";
        const string firstFile = "[One Pace][106-109] Whiskey Peak 01 [480p][AAAAAAAA].mkv";
        const string secondFile = "[One Pace][110-114] Whiskey Peak 02 [480p][BBBBBBBB].mkv";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var categoryRoot = Path.Combine(downloadRoot, "logpose");
        var jobDirectory = Path.Combine(categoryRoot, packName);
        var nestedDirectory = Path.Combine(jobDirectory, packName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(nestedDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(packName, firstFile);
            torrent.Downloads.Add(new()
            {
                DownloadId = Guid.NewGuid(),
                TorrentId = torrent.TorrentId,
                FileName = secondFile,
                Link = "https://example.test/second-file"
            });
            torrent.RdFiles = JsonSerializer.Serialize(new[]
            {
                new TorrentClientFile { Path = $"{packName}/{firstFile}" },
                new TorrentClientFile { Path = $"{packName}/{secondFile}" }
            });

            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.False(fileSystem.Directory.Exists(nestedDirectory));
            Assert.False(fileSystem.Directory.Exists(jobDirectory));
            Assert.True(fileSystem.Directory.Exists(categoryRoot));
            Assert.True(fileSystem.Directory.Exists(downloadRoot));
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_PreservesNonEmptyJobDirectory()
    {
        const string jobName = "One Pace Episode 01";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var jobDirectory = Path.Combine(downloadRoot, "logpose", jobName);
        var retainedFile = Path.Combine(jobDirectory, "keep.mkv");
        var fileSystem = new MockFileSystem();
        fileSystem.AddFile(retainedFile, new MockFileData("media"));

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, "episode.mkv");
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.True(fileSystem.Directory.Exists(jobDirectory));
            Assert.True(fileSystem.File.Exists(retainedFile));
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_PreservesDirectoryOutsideCategoryRoot()
    {
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var categoryRoot = Path.Combine(downloadRoot, "logpose");
        var outsideDirectory = Path.Combine(downloadRoot, "outside-job");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(categoryRoot);
        fileSystem.AddDirectory(outsideDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(Path.Combine("..", "outside-job"), "episode.mkv");
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.True(fileSystem.Directory.Exists(outsideDirectory));
            Assert.True(fileSystem.Directory.Exists(categoryRoot));
            Assert.True(fileSystem.Directory.Exists(downloadRoot));
            torrentData.Verify(value => value.Delete(torrent.TorrentId), Times.Once);
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_PreservesSiblingDirectoryOutsideJobRoot()
    {
        const string jobName = "One Pace Episode 01";
        const string siblingName = "other-job";
        const string fileName = "episode.mkv";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var categoryRoot = Path.Combine(downloadRoot, "logpose");
        var jobDirectory = Path.Combine(categoryRoot, jobName);
        var siblingDirectory = Path.Combine(categoryRoot, siblingName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(jobDirectory);
        fileSystem.AddDirectory(siblingDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, fileName);
            torrent.RdFiles = JsonSerializer.Serialize(new[]
            {
                new TorrentClientFile { Path = $"../{siblingName}/{fileName}" }
            });
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.True(fileSystem.Directory.Exists(jobDirectory));
            Assert.True(fileSystem.Directory.Exists(siblingDirectory));
            torrentData.Verify(value => value.Delete(torrent.TorrentId), Times.Once);
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Theory]
    [InlineData("job")]
    [InlineData("category")]
    [InlineData("download")]
    public async Task DeleteWithoutFiles_PreservesJobWhenPathContainsReparsePoint(string reparseLocation)
    {
        const string jobName = "One Pace Episode 01";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var categoryRoot = Path.Combine(downloadRoot, "logpose");
        var jobDirectory = Path.Combine(categoryRoot, jobName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(jobDirectory);
        var reparseDirectory = reparseLocation switch
        {
            "job" => jobDirectory,
            "category" => categoryRoot,
            "download" => downloadRoot,
            _ => throw new ArgumentOutOfRangeException(nameof(reparseLocation))
        };
        fileSystem.File.SetAttributes(
            reparseDirectory,
            fileSystem.File.GetAttributes(reparseDirectory) | FileAttributes.ReparsePoint);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, "episode.mkv");
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.True(fileSystem.Directory.Exists(jobDirectory));
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_WithoutCategoryPreservesDownloadRoot()
    {
        const string jobName = "One Pace Episode 01";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var jobDirectory = Path.Combine(downloadRoot, jobName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(jobDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, "episode.mkv");
            torrent.Category = null;
            var torrentData = CreateTorrentDataForDelete(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);

            Assert.False(fileSystem.Directory.Exists(jobDirectory));
            Assert.True(fileSystem.Directory.Exists(downloadRoot));
            torrentData.Verify(value => value.Delete(torrent.TorrentId), Times.Once);
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    [Fact]
    public async Task DeleteWithoutFiles_RetryIsIdempotentWhenRecordAndDirectoryAreMissing()
    {
        const string jobName = "One Pace Episode 01";
        var originalDownloadPath = Settings.Get.Paths.DownloadPath;
        var downloadRoot = GetTestDownloadRoot();
        var jobDirectory = Path.Combine(downloadRoot, "logpose", jobName);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(jobDirectory);

        try
        {
            Settings.Get.Paths.DownloadPath = downloadRoot;
            var torrent = CreateDeletionTorrent(jobName, "episode.mkv");
            var torrentData = new Mock<ITorrentData>();
            torrentData.SetupSequence(data => data.GetByHash(torrent.Hash))
                       .ReturnsAsync(torrent)
                       .ReturnsAsync((Torrent?)null);
            torrentData.Setup(data => data.GetById(torrent.TorrentId)).ReturnsAsync(torrent);
            var compatibility = CreateCompatibility(torrentData: torrentData, fileSystem: fileSystem);

            await compatibility.Delete(torrent.Hash, false);
            await compatibility.Delete(torrent.Hash, false);

            Assert.False(fileSystem.Directory.Exists(jobDirectory));
            torrentData.Verify(value => value.Delete(torrent.TorrentId), Times.Once);
        }
        finally
        {
            Settings.Get.Paths.DownloadPath = originalDownloadPath;
        }
    }

    private static Torrent CreateDeletionTorrent(string rdName, string fileName)
    {
        var torrentId = Guid.NewGuid();

        return new()
        {
            TorrentId = torrentId,
            Hash = Guid.NewGuid().ToString("N"),
            Category = "logpose",
            RdName = rdName,
            Downloads =
            [
                new()
                {
                    DownloadId = Guid.NewGuid(),
                    TorrentId = torrentId,
                    FileName = fileName,
                    Link = "https://example.test/file"
                }
            ]
        };
    }

    private static string GetTestDownloadRoot()
    {
        return Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "adbclient-qbittorrent-tests",
            "downloads"));
    }

    private static Mock<ITorrentData> CreateTorrentDataForDelete(Torrent torrent)
    {
        var torrentData = new Mock<ITorrentData>();
        torrentData.Setup(data => data.GetByHash(torrent.Hash)).ReturnsAsync(torrent);
        torrentData.Setup(data => data.GetById(torrent.TorrentId)).ReturnsAsync(torrent);
        return torrentData;
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
            httpClientFactory.Object,
            fileSystem);
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
