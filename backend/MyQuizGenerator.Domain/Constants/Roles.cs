namespace MyQuizGenerator.Domain.Constants;

/// <summary>
/// Application role constants.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    /// <summary>
    /// All available roles.
    /// </summary>
    public static readonly string[] All = [Admin, User];
}
