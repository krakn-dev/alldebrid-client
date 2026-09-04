using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdbClient.Service.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdbClient.Service.BackgroundServices;

public class UpdateChecker(ILogger<UpdateChecker> logger, IHttpClientFactory httpClientFactory) : BackgroundService
{
    private const string LatestReleaseEndpoint = "https://api.github.com/repos/krakn-dev/alldebrid-client/releases/latest";

    public static string? CurrentVersion { get; private set; }

    public static string? LatestVersion { get; private set; }

    public static bool UpdateAvailable { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Startup.Ready)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        CurrentVersion = ApplicationVersion.CurrentTag;

        if (CurrentVersion == null)
        {
            logger.LogWarning("Unable to determine the current application version; update checks are disabled.");
            return;
        }

        logger.LogInformation("Update checker started on {CurrentVersion}.", CurrentVersion);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckForUpdate(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckForUpdate(CancellationToken stoppingToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(nameof(UpdateChecker));
            var release = await httpClient.GetFromJsonAsync<GitHubRelease>(LatestReleaseEndpoint, stoppingToken);

            if (string.IsNullOrWhiteSpace(release?.TagName))
            {
                logger.LogWarning("GitHub did not return a latest release version.");
                return;
            }

            LatestVersion = release.TagName;
            UpdateAvailable = ApplicationVersion.IsNewerRelease(LatestVersion, CurrentVersion);

            if (UpdateAvailable)
            {
                logger.LogInformation("A newer release is available: {LatestVersion}.", LatestVersion);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug(ex, "The GitHub update check failed; the application will retry later.");
        }
    }
}

public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }
}
