namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IFileService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
}
