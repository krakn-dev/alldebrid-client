using System.IO.Abstractions;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using AdbClient.Service.BackgroundServices;
using AdbClient.Service.Helpers;
using AdbClient.Service.Middleware;
using AdbClient.Service.Services;
using AdbClient.Service.Services.TorrentClients;
using AdbClient.Service.Wrappers;

namespace AdbClient.Service;

public static class DiConfig
{
    public const string ProviderHttpClient = "AllDebrid";
    public static readonly string UserAgent = $"alldebrid-client {ApplicationVersion.CurrentText ?? "unknown"}";

    public static void RegisterAdbServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IAllDebridNetClientFactory, AllDebridNetClientFactory>();
        services.AddScoped<AllDebridTorrentClient>();

        services.AddSingleton<IProcessFactory, ProcessFactory>();
        services.AddSingleton<IFileSystem, FileSystem>();

        services.AddScoped<Authentication>();
        services.AddScoped<IDownloads, Downloads>();
        services.AddScoped<Downloads>();
        services.AddScoped<RemoteService>();
        services.AddScoped<Settings>();
        services.AddScoped<Torrents>();
        services.AddScoped<TorrentRunner>();

        services.AddSingleton<IDownloadableFileFilter, DownloadableFileFilter>();
        services.AddSingleton<ITrackerListGrabber, TrackerListGrabber>();
        services.AddSingleton<IEnricher, Enricher>();

        services.AddSingleton<IAuthorizationHandler, AuthSettingHandler>();

        services.AddHostedService<ProviderUpdater>();
        services.AddHostedService<Startup>();
        services.AddHostedService<TaskRunner>();
        services.AddHostedService<UpdateChecker>();
        services.AddHostedService<WatchFolderChecker>();
        services.AddHostedService<WebsocketsUpdater>();
    }

    public static void RegisterHttpClients(this IServiceCollection services)
    {
        var retryPolicy = HttpPolicyExtensions
                          .HandleTransientHttpError()
                          .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                          .WaitAndRetryAsync(retryCount: 5, sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.ConfigureHttpClient(httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            });
        });

        services.AddHttpClient(ProviderHttpClient)
                .AddPolicyHandler(retryPolicy);
    }
}
