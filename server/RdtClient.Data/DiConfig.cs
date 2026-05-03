using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Internal;

namespace RdtClient.Data;

public static class DiConfig
{
    public static void Config(IServiceCollection services, AppSettings appSettings)
    {
        var dbPath = appSettings.Database?.Path ?? Path.Combine(appSettings.DataPath, "adbclient.db");

        if (string.IsNullOrWhiteSpace(dbPath))
        {
            throw new Exception("No database path configured. Set DataPath in appsettings.json (e.g. C:\\ProgramData\\RdtClient).");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var connectionString = $"Data Source={dbPath}";
        services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<DownloadData>();
        services.AddScoped<SettingData>();
        services.AddScoped<ITorrentData, TorrentData>();
        services.AddScoped<UserData>();
    }
}