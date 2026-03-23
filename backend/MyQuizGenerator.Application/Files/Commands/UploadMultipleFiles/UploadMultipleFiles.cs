using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Services;
using MyQuizGenerator.Application.Files.DTOs;

namespace MyQuizGenerator.Application.Files.Commands.UploadMultipleFiles;

public record UploadMultipleFilesCommand(List<FileUploadRequest> Files) : IRequest<List<string>>;

public class UploadMultipleFilesCommandHandler : IRequestHandler<UploadMultipleFilesCommand, List<string>>
{
    private readonly IFileService _fileService;

    public UploadMultipleFilesCommandHandler(
        IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<List<string>> Handle(UploadMultipleFilesCommand command, CancellationToken cancellationToken)
    {
        return await _fileService.UploadMultipleFilesAsync(command.Files);
    }
}
