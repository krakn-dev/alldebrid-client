using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AdbClient.Web;

public static class AuthenticationRedirects
{
    public static Task HandleLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = context.Request.Path.StartsWithSegments(
            "/api/v2",
            StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        return Task.CompletedTask;
    }
}
