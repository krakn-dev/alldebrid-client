using AdbClient.Data.Enums;
using AdbClient.Service.Models.QBittorrent;
using AdbClient.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdbClient.Web.Controllers;

/// <summary>
/// Implements the qBittorrent Web API download-client surface used by Logpose, Sonarr, and Radarr.
/// </summary>
[ApiController]
[Route("api/v2")]
public sealed class QBittorrentController(
    ILogger<QBittorrentController> logger,
    IQBittorrentCompatibility compatibility) : ControllerBase
{
    public const string CompatibleVersion = "v4.3.4";
    public const string CompatibleWebApiVersion = "2.8.1";

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(
        [FromForm(Name = "username")] string? userName,
        [FromForm(Name = "password")] string? password)
    {
        if (Settings.Get.General.AuthenticationType == AuthenticationType.None)
        {
            Response.Cookies.Append("SID", "anonymous", new()
            {
                HttpOnly = true,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            return Text("Ok.");
        }

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return Text("Fails.");
        }

        var authenticated = await compatibility.Login(userName, password);
        return Text(authenticated ? "Ok." : "Fails.");
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("app/version")]
    public IActionResult Version()
    {
        return Text(CompatibleVersion);
    }

    [AllowAnonymous]
    [HttpGet("app/webapiVersion")]
    public IActionResult WebApiVersion()
    {
        return Text(CompatibleWebApiVersion);
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("app/preferences")]
    public ActionResult<QBittorrentPreferences> Preferences()
    {
        return Ok(compatibility.GetPreferences());
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("app/defaultSavePath")]
    public IActionResult DefaultSavePath()
    {
        return Text(compatibility.GetPreferences().SavePath);
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("torrents/categories")]
    public async Task<ActionResult<IReadOnlyDictionary<string, QBittorrentCategory>>> Categories()
    {
        return Ok(await compatibility.GetCategories());
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/createCategory")]
    public async Task<IActionResult> CreateCategory([FromForm(Name = "category")] string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest("Category cannot be empty.");
        }

        try
        {
            await compatibility.CreateCategory(category);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/add")]
    public async Task<IActionResult> Add(
        [FromForm(Name = "urls")] string? urls,
        [FromForm(Name = "category")] string? category,
        [FromForm(Name = "torrents")] List<IFormFile>? torrentFiles,
        [FromForm(Name = "paused")] bool? paused,
        [FromForm(Name = "sequentialDownload")] bool? sequentialDownload,
        [FromForm(Name = "firstLastPiecePrio")] bool? firstLastPiecePriority,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(urls) && (torrentFiles == null || torrentFiles.Count == 0))
        {
            return BadRequest("At least one torrent URL or file is required.");
        }

        if (paused is true)
        {
            return BadRequest("Adding torrents in a paused state is not supported.");
        }

        if (sequentialDownload is true || firstLastPiecePriority is true)
        {
            return BadRequest("Sequential and first/last-piece downloads are not supported.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(urls))
            {
                await compatibility.Add(urls, category, cancellationToken);
            }

            if (torrentFiles != null)
            {
                foreach (var torrentFile in torrentFiles)
                {
                    if (torrentFile.Length <= 0)
                    {
                        return BadRequest("Torrent file cannot be empty.");
                    }

                    if (torrentFile.Length > QBittorrentCompatibility.MaxTorrentFileSizeBytes)
                    {
                        return BadRequest("Torrent file exceeds the 32 MB limit.");
                    }

                    await using var fileStream = torrentFile.OpenReadStream();
                    await using var memoryStream = new MemoryStream();
                    await fileStream.CopyToAsync(memoryStream, cancellationToken);
                    await compatibility.Add(memoryStream.ToArray(), category);
                }
            }

            return Text("Ok.");
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Rejected invalid torrent metadata");
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Rejected qBittorrent add request");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Rejected conflicting qBittorrent add request");
            return Conflict(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Unable to download torrent metadata");
            return StatusCode(StatusCodes.Status502BadGateway, "Unable to download torrent metadata.");
        }
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("torrents/info")]
    public async Task<ActionResult<IReadOnlyList<QBittorrentTorrentInfo>>> Info(
        [FromQuery(Name = "category")] string? category)
    {
        var torrents = await compatibility.GetTorrents(category);
        return Ok(torrents);
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("torrents/properties")]
    public async Task<ActionResult<QBittorrentTorrentProperties>> Properties(
        [FromQuery(Name = "hash")] string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return BadRequest("Torrent hash cannot be empty.");
        }

        var properties = await compatibility.GetProperties(hash);
        return properties == null ? NotFound() : Ok(properties);
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpGet("torrents/files")]
    public async Task<ActionResult<IReadOnlyList<QBittorrentTorrentFile>>> Files(
        [FromQuery(Name = "hash")] string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return BadRequest("Torrent hash cannot be empty.");
        }

        var files = await compatibility.GetFiles(hash);
        return files == null ? NotFound() : Ok(files);
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/setCategory")]
    public async Task<IActionResult> SetCategory(
        [FromForm(Name = "hashes")] string? hashes,
        [FromForm(Name = "category")] string? category)
    {
        if (string.IsNullOrWhiteSpace(hashes))
        {
            return BadRequest("At least one torrent hash is required.");
        }

        try
        {
            await compatibility.SetCategory(hashes, category);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/topPrio")]
    public async Task<IActionResult> SetTopPriority([FromForm(Name = "hashes")] string? hashes)
    {
        if (string.IsNullOrWhiteSpace(hashes))
        {
            return BadRequest("At least one torrent hash is required.");
        }

        try
        {
            await compatibility.SetTopPriority(hashes);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/setShareLimits")]
    public IActionResult SetShareLimits()
    {
        // ADC never seeds. It accepts these qBittorrent parameters but retains jobs by ADC policy.
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/setForceStart")]
    public IActionResult SetForceStart(
        [FromForm(Name = "hashes")] string? hashes,
        [FromForm(Name = "value")] bool value)
    {
        if (string.IsNullOrWhiteSpace(hashes))
        {
            return BadRequest("At least one torrent hash is required.");
        }

        if (ContainsAllSelection(hashes))
        {
            return BadRequest("Bulk selection of every torrent is not supported.");
        }

        // ADC starts every accepted provider job immediately; there is no separate seeding mode.
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/delete")]
    public async Task<IActionResult> Delete(
        [FromForm(Name = "hashes")] string? hashes,
        [FromForm(Name = "deleteFiles")] bool deleteFiles)
    {
        if (string.IsNullOrWhiteSpace(hashes))
        {
            return BadRequest("At least one torrent hash is required.");
        }

        if (ContainsAllSelection(hashes))
        {
            return BadRequest("Bulk selection of every torrent is not supported.");
        }

        try
        {
            await compatibility.Delete(hashes, deleteFiles);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Refused unsafe qBittorrent delete request");
            return Conflict(ex.Message);
        }
    }

    private static bool ContainsAllSelection(string hashes)
    {
        return hashes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Any(hash => hash.Equals("all", StringComparison.OrdinalIgnoreCase));
    }

    private ContentResult Text(string value)
    {
        return Content(value, "text/plain");
    }
}
