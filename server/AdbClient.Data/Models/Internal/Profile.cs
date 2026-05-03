namespace RdtClient.Data.Models.Internal;

public class Profile
{
    public string? Provider { get; set; }
    public string? UserName { get; set; }
    public DateTimeOffset? Expiration { get; set; }
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool? IsInsecure { get; set; }
    public bool? DisableUpdateNotification { get; set; }
}