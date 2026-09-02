using AdbClient.Data.Data;
using AdbClient.Data.Models.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AdbClient.Service.Test.Data;

public class SettingDataTest
{
    [Fact]
    public async Task Seed_ReplacesLegacyChunkSizeSettingWithParallelChunkCount()
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
            SettingId = "DownloadClient:ChunkCount",
            Value = "50"
        });
        await dataContext.SaveChangesAsync();
        dataContext.ChangeTracker.Clear();

        var settingData = new SettingData(dataContext, Mock.Of<ILogger<SettingData>>());
        await settingData.Seed();

        var settings = await dataContext.Settings.AsNoTracking().ToListAsync();

        Assert.DoesNotContain(settings, setting => setting.SettingId == "DownloadClient:ChunkCount");
        Assert.Contains(settings, setting =>
            setting.SettingId == "DownloadClient:ParallelChunkCount" && setting.Value == "0");
    }
}
