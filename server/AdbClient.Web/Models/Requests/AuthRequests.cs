namespace AdbClient.Web.Models.Requests;

public class AuthControllerLoginRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class AuthControllerSetupProviderRequest
{
    public int Provider { get; set; }
    public string? Token { get; set; }
}

public class AuthControllerUpdateRequest
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
