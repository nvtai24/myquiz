namespace MyQuizGenerator.Infrastructure.Settings;

/// <summary>
/// Google OAuth configuration settings loaded from appsettings.json.
/// </summary>
public class GoogleSettings
{
    public const string SectionName = "GoogleSettings";

    /// <summary>
    /// Google OAuth Client ID from Google Cloud Console.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
