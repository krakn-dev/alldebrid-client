using Microsoft.AspNetCore.Authorization;
using AdbClient.Data.Enums;
using AdbClient.Service.Services;

namespace AdbClient.Service.Middleware;

public class AuthSettingRequirement : IAuthorizationRequirement
{

}

public class AuthSettingHandler : AuthorizationHandler<AuthSettingRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthSettingRequirement requirement) 
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        if (Settings.Get.General.AuthenticationType == AuthenticationType.None)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask; 
    }
}
