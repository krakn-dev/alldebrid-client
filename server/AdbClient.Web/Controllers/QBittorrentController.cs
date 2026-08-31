using AdbClient.Data.Enums;
using AdbClient.Service.Models.QBittorrent;
using AdbClient.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdbClient.Web.Controllers;

/// <summary>
/// Implements the qBittorrent Web API subset consumed by Logpose.
/// </summary>
[ApiController]
[Route("api/v2")]
public sealed class QBittorrentController(
    ILogger<QBittorrentController> logger,
    IQBittorrentCompatibility compatibility) : ControllerBase
{
    public const string CompatibleVersion = "v4.3.2";

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

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/createCategory")]
    public async Task<IActionResult> CreateCategory([FromForm(Name = "category")] string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest("Category cannot be empty.");
        }

        await compatibility.CreateCategory(category);
        return Ok();
    }

    [Authorize(Policy = "AuthSetting")]
    [HttpPost("torrents/add")]
    public async Task<IActionResult> Add(
        [FromForm(Name = "urls")] string? urls,
        [FromForm(Name = "category")] string? category,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(urls))
        {
            return BadRequest("At least one torrent URL is required.");
        }

        try
        {
            await compatibility.Add(urls, category, cancellationToken);
            return Text("Ok.");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Rejected qBittorrent add request");
            return BadRequest(ex.Message);
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
    [HttpPost("torrents/delete")]
    public async Task<IActionResult> Delete(
        [FromForm(Name = "hashes")] string? hashes,
        [FromForm(Name = "deleteFiles")] bool deleteFiles)
    {
        if (string.IsNullOrWhiteSpace(hashes))
        {
            return BadRequest("At least one torrent hash is required.");
        }

        await compatibility.Delete(hashes, deleteFiles);
        return Ok();
    }

    private ContentResult Text(string value)
    {
        return Content(value, "text/plain");
    }
}
