namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserInfo?> GetUserInfoAsync(string userId);
    Task<List<string>> SearchUserIdsAsync(string searchTerm);
}

public class UserInfo
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
