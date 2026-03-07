using Microsoft.AspNetCore.Identity;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Infrastructure.Identity;

namespace MyQuizGenerator.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;

    public UserService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        return new UserInfo
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        };
    }
}
