using Microsoft.AspNetCore.Authorization;

namespace DocumentArchive.Core.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userPermissions = context.User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        if (requirement.Permissions.Any(p => userPermissions.Contains(p))) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}