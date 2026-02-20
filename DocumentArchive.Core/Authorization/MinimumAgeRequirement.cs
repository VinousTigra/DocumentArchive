using Microsoft.AspNetCore.Authorization;

namespace DocumentArchive.Core.Authorization;

public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }

    public int MinimumAge { get; }
}