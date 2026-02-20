using Microsoft.AspNetCore.Authorization;

namespace DocumentArchive.Core.Authorization;

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var dateOfBirthClaim = context.User.FindFirst("DateOfBirth");
        if (dateOfBirthClaim == null)
            return Task.CompletedTask;

        if (DateTime.TryParse(dateOfBirthClaim.Value, out var dateOfBirth))
        {
            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            if (age >= requirement.MinimumAge)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}