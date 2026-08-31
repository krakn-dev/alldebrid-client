using System.Net;
using System.Text.Json;
using AdbClient.Service.Middleware;
using AdbClient.Service.Models.QBittorrent;
using AdbClient.Service.Services;
using AdbClient.Web.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdbClient.Service.Test.Controllers;

public class QBittorrentContractTest
{
    [Fact]
    public async Task LoginAndVersion_MatchLogposeRequests()
    {
        var compatibility = new RecordingCompatibility();
        await using var app = await StartApplication(compatibility);
        using var client = CreateClient(app);

        using var login = await client.PostAsync("api/v2/auth/login", Form(
            ("username", "logpose"),
            ("password", "secret")));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("text/plain", login.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Ok.", await login.Content.ReadAsStringAsync());

        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("SID=", cookie, StringComparison.Ordinal);

        using var versionRequest = new HttpRequestMessage(HttpMethod.Get, "api/v2/app/version");
        versionRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        using var version = await client.SendAsync(versionRequest);

        Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        Assert.Equal("text/plain", version.Content.Headers.ContentType?.MediaType);
        Assert.Equal(QBittorrentController.CompatibleVersion, await version.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TorrentLifecycle_MatchesLogposeRequestsAndResponseFields()
    {
        const string hash = "0123456789abcdef0123456789abcdef01234567";
        const string magnet = "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=One%20Pace";

        var compatibility = new RecordingCompatibility
        {
            Torrents =
            [
                new()
                {
                    Hash = hash,
                    Name = "One Pace Episode 01",
                    Category = "logpose",
                    Progress = 0.75,
                    State = "downloading",
                    ContentPath = "/downloads/logpose/One Pace Episode 01/episode.mkv",
                    SavePath = "/downloads/logpose",
                    Size = 1_000,
                    DownloadSpeed = 250,
                    Eta = 1
                }
            ]
        };

        await using var app = await StartApplication(compatibility);
        using var client = CreateClient(app);

        using var login = await client.PostAsync("api/v2/auth/login", Form(
            ("username", "logpose"),
            ("password", "secret")));
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);

        using var createCategory = await client.PostAsync("api/v2/torrents/createCategory", Form(
            ("category", "logpose"),
            ("savePath", string.Empty)));
        Assert.Equal(HttpStatusCode.OK, createCategory.StatusCode);
        Assert.Equal("logpose", compatibility.CreatedCategory);

        using var add = await client.PostAsync("api/v2/torrents/add", Form(
            ("urls", magnet),
            ("category", "logpose")));
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        Assert.Equal("Ok.", await add.Content.ReadAsStringAsync());
        Assert.Equal((magnet, "logpose"), compatibility.AddedTorrent);

        using var info = await client.GetAsync("api/v2/torrents/info?category=logpose");
        Assert.Equal(HttpStatusCode.OK, info.StatusCode);
        Assert.Equal("logpose", compatibility.RequestedCategory);

        using var document = JsonDocument.Parse(await info.Content.ReadAsStringAsync());
        var torrent = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(hash, torrent.GetProperty("hash").GetString());
        Assert.Equal("One Pace Episode 01", torrent.GetProperty("name").GetString());
        Assert.Equal("logpose", torrent.GetProperty("category").GetString());
        Assert.Equal(0.75, torrent.GetProperty("progress").GetDouble());
        Assert.Equal("downloading", torrent.GetProperty("state").GetString());
        Assert.Equal("/downloads/logpose/One Pace Episode 01/episode.mkv", torrent.GetProperty("content_path").GetString());
        Assert.Equal("/downloads/logpose", torrent.GetProperty("save_path").GetString());
        Assert.Equal(1_000, torrent.GetProperty("size").GetInt64());
        Assert.Equal(250, torrent.GetProperty("dlspeed").GetInt64());
        Assert.Equal(1, torrent.GetProperty("eta").GetInt64());

        using var delete = await client.PostAsync("api/v2/torrents/delete", Form(
            ("hashes", hash),
            ("deleteFiles", "false")));
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal((hash, false), compatibility.DeletedTorrent);
    }

    [Theory]
    [InlineData("https://nyaa.si/?q=a031cac6baf81b804c4d034dfaef0e5e4a671145")]
    [InlineData("https://nyaa.si/?q=A031CAC6BAF81B804C4D034DFAEF0E5E4A671145")]
    public async Task AddNyaaInfoHashSearchUrl_MatchesLogposeRequest(string searchUrl)
    {
        var compatibility = new RecordingCompatibility();
        await using var app = await StartApplication(compatibility);
        using var client = CreateClient(app);

        using var add = await client.PostAsync("api/v2/torrents/add", Form(
            ("urls", searchUrl),
            ("category", "logpose")));

        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        Assert.Equal("Ok.", await add.Content.ReadAsStringAsync());
        Assert.Equal((searchUrl, "logpose"), compatibility.AddedTorrent);
    }

    [Fact]
    public async Task DeleteRetry_RemainsIdempotentForLogpose()
    {
        const string hash = "0123456789abcdef0123456789abcdef01234567";
        var compatibility = new RecordingCompatibility();
        await using var app = await StartApplication(compatibility);
        using var client = CreateClient(app);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var delete = await client.PostAsync("api/v2/torrents/delete", Form(
                ("hashes", hash),
                ("deleteFiles", "false")));
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        }

        Assert.Equal(2, compatibility.DeleteCount);
        Assert.Equal((hash, false), compatibility.DeletedTorrent);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] values)
    {
        return new(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value)));
    }

    private static async Task<WebApplication> StartApplication(IQBittorrentCompatibility compatibility)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(QBittorrentController).Assembly);
        builder.Services.AddSingleton(compatibility);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options => options.Cookie.Name = "SID");
        builder.Services.AddSingleton<IAuthorizationHandler, AuthSettingHandler>();
        builder.Services.AddAuthorizationBuilder()
               .AddPolicy("AuthSetting", policy => policy.Requirements.Add(new AuthSettingRequirement()));

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();

        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        var address = Assert.Single(server.Features.Get<IServerAddressesFeature>()!.Addresses);

        return new(new HttpClientHandler { UseCookies = false })
        {
            BaseAddress = new(address)
        };
    }

    private sealed class RecordingCompatibility : IQBittorrentCompatibility
    {
        public IReadOnlyList<QBittorrentTorrentInfo> Torrents { get; init; } = [];
        public string? CreatedCategory { get; private set; }
        public (string Urls, string? Category)? AddedTorrent { get; private set; }
        public string? RequestedCategory { get; private set; }
        public (string Hashes, bool DeleteFiles)? DeletedTorrent { get; private set; }
        public int DeleteCount { get; private set; }

        public Task<bool> Login(string userName, string password)
        {
            return Task.FromResult(true);
        }

        public Task CreateCategory(string category)
        {
            CreatedCategory = category;
            return Task.CompletedTask;
        }

        public Task Add(string urls, string? category, CancellationToken cancellationToken = default)
        {
            AddedTorrent = (urls, category);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<QBittorrentTorrentInfo>> GetTorrents(string? category)
        {
            RequestedCategory = category;
            return Task.FromResult(Torrents);
        }

        public Task Delete(string hashes, bool deleteFiles)
        {
            DeletedTorrent = (hashes, deleteFiles);
            DeleteCount++;
            return Task.CompletedTask;
        }
    }
}
