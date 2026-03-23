using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Services;
using MyQuizGenerator.Application.Files.DTOs;

namespace MyQuizGenerator.Application.Files.Commands.UploadFile;

public record UploadFileCommand(FileUploadRequest File) : IRequest<string>;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, string>
{
    private readonly IFileService _fileService;

    public UploadFileCommandHandler(
        IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        return await _fileService.UploadFileAsync(request.File);
    }
}
