namespace MyQuizGenerator.Infrastructure.Settings;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string ClientBaseUrl { get; set; } = "http://localhost:3000";
}
