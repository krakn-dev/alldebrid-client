using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.Internal;
using RdtClient.Service.Helpers;
using RdtClient.Service.Services;

namespace RdtClient.Web.Controllers;

[Authorize(Policy = "AuthSetting")]
[Route("Api/Settings")]
public class SettingsController(Settings settings, Torrents torrents) : Controller
{
    [HttpGet]
    [Route("")]
    public ActionResult Get()
    {
        var result = SettingData.GetAll();
        return Ok(result);
    }

    [HttpPut]
    [Route("")]
    public async Task<ActionResult> Update([FromBody] IList<SettingProperty>? settings1)
    {
        if (settings1 == null)
        {
            return BadRequest();
        }

        await settings.Update(settings1);
        
        return Ok();
    }

    [HttpGet]
    [Route("Profile")]
    public async Task<ActionResult<Profile>> Profile()
    {
        var profile = await torrents.GetProfile();
        return Ok(profile);
    }

    [HttpGet]
    [Route("Version")]
    public ActionResult<Version> Version()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        return Ok(new
        {
            Version = version
        });
    }
        
    [HttpPost]
    [Route("TestPath")]
    public async Task<ActionResult> TestPath([FromBody] SettingsControllerTestPathRequest? request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        if (String.IsNullOrEmpty(request.Path))
        {
            return BadRequest("Invalid path");
        }

        var path = request.Path.TrimEnd('/').TrimEnd('\\');

        if (!Directory.Exists(path))
        {
            throw new($"Path {path} does not exist");
        }

        var testFile = $"{path}/test.txt";

        await System.IO.File.WriteAllTextAsync(testFile, "RealDebridClient Test File, you can remove this file.");
            
        await FileHelper.Delete(testFile);

        return Ok();
    }
        
    [HttpGet]
    [Route("TestDownloadSpeed")]
    public async Task<ActionResult> TestDownloadSpeed(CancellationToken cancellationToken)
    {
        var downloadPath = Settings.Get.Paths.DownloadPath;

        var testFilePath = Path.Combine(downloadPath, "testDefault.rar");

        await FileHelper.Delete(testFilePath);

        var download = new Download
        {
            Link = "https://34.download.real-debrid.com/speedtest/testDefault.rar",
            Torrent = new()
            {
                DownloadClient = RdtClient.Data.Enums.DownloadClient.Internal,
                RdName = "testDefault.rar"
            }
        };

        var downloadClient = new DownloadClient(download, download.Torrent, downloadPath);

        await downloadClient.Start();

        while (!downloadClient.Finished)
        {
            await Task.Delay(1000, CancellationToken.None);

            if (cancellationToken.IsCancellationRequested)
            {
                await downloadClient.Cancel();
            }

            if (downloadClient.BytesDone > 1024 * 1024 * 50)
            {
                await downloadClient.Cancel();

                break;
            }
        }

        await FileHelper.Delete(testFilePath);

        return Ok(downloadClient.Speed);
    }
        
    [HttpGet]
    [Route("TestWriteSpeed")]
    public async Task<ActionResult> TestWriteSpeed()
    {
        var downloadPath = Settings.Get.Paths.DownloadPath;

        var testFilePath = Path.Combine(downloadPath, "test.tmp");

        await FileHelper.Delete(testFilePath);

        const Int32 testFileSize = 64 * 1024 * 1024;

        var watch = new Stopwatch();

        watch.Start();

        var rnd = new Random();

        await using var fileStream = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Write);

        var buffer = new Byte[64 * 1024];

        while (fileStream.Length < testFileSize)
        {
            rnd.NextBytes(buffer);

            await fileStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
        }
            
        watch.Stop();

        var writeSpeed = fileStream.Length / watch.Elapsed.TotalSeconds;
            
        fileStream.Close();

        await FileHelper.Delete(testFilePath);
        
        return Ok(writeSpeed);
    }

}

public class SettingsControllerTestPathRequest
{
    public String? Path { get; set; }
}