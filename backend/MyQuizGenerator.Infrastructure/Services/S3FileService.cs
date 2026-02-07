using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Infrastructure.Settings;

namespace MyQuizGenerator.Infrastructure.Services;

public class S3FileService : IFileService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _storageSettings;

    public S3FileService(IAmazonS3 s3Client, IOptions<StorageSettings> storageSettings)
    {
        _s3Client = s3Client;
        _storageSettings = storageSettings.Value;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var key = $"{Guid.NewGuid()}_{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _storageSettings.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            // CannedACL is often not needed if bucket policy is public or using cloudfront/presigned urls, 
            // but user had acceptable config before. We will omit CannedACL if not strictly required or use based on verify.
            // Keeping it simple as per previous working version minus the CannedACL error if any.
            // Re-adding CannedACL.PublicRead as it was in original plan, user removed it likely due to bucket settings.
            // Safer to OMIT CannedACL for likely "Bucket Owner Enforced" settings or similar defaults in 2024.
            // I will OMIT it for now to avoid 400 Bad Request if bucket blocks ACLs.
        };

        await _s3Client.PutObjectAsync(request);

        return $"https://{_storageSettings.BucketName}.s3.{_storageSettings.Region}.amazonaws.com/{key}";
    }
}
