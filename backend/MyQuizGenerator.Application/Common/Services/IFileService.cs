namespace MyQuizGenerator.Application.Common.Interfaces;

using MyQuizGenerator.Application.Files.DTOs;

public interface IFileService
{
    Task<string> UploadFileAsync(FileUploadRequest file);

    Task<List<string>> UploadMultipleFilesAsync(List<FileUploadRequest> files);
}
