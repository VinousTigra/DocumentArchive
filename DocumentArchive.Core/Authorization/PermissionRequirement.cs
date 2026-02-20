using Microsoft.AspNetCore.Authorization;

namespace DocumentArchive.Core.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }

    public string[] Permissions { get; }
}