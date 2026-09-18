using Khidma.Api.Contracts.Auth;
using Khidma.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace Khidma.Api.Auth;

public static class CurrentUserMapper
{
    public static async Task<CurrentUserDto> ToDtoAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty
        };
    }
}
