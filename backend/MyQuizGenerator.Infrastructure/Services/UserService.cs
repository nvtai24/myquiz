using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public async Task<List<string>> SearchUserIdsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return new List<string>();
        
        var searchLower = searchTerm.ToLower();
        var query = _userManager.Users.AsQueryable();
        
        query = query.Where(u =>
            (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
            (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
            (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
            
        return await query.Select(u => u.Id).ToListAsync();
    }
}
