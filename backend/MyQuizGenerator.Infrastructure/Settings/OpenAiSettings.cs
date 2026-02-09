namespace MyQuizGenerator.Infrastructure.Settings;

public class OpenAiSettings
{
    public const string SectionName = "OpenAiSettings";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4096;
}
