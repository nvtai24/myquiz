namespace MyQuizGenerator.Application.Common.Interfaces;

using MyQuizGenerator.Application.Files.Commands;
using MyQuizGenerator.Application.Files.Commands.UploadFile;

public interface IFileService
{
    Task<string> UploadFileAsync(FileUploadRequest file);

    Task<List<string>> UploadMultipleFilesAsync(List<FileUploadRequest> files);
}
