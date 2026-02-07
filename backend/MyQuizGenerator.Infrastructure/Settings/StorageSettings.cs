namespace MyQuizGenerator.Infrastructure.Settings;

public class StorageSettings
{
    public const string SectionName = "StorageSettings";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
