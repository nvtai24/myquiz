using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Files.Commands.UploadMultipleFiles;

public record UploadMultipleFilesCommand(List<FileUploadRequest> Files) : IRequest<List<string>>;

public class UploadMultipleFilesCommandHandler : IRequestHandler<UploadMultipleFilesCommand, List<string>>
{
    private readonly IFileService _fileService;
    // private readonly IRepository<string, UploadedFile> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadMultipleFilesCommandHandler(
        IFileService fileService,
        IUnitOfWork unitOfWork)
    {
        _fileService = fileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(UploadMultipleFilesCommand command, CancellationToken cancellationToken)
    {
        return await _fileService.UploadMultipleFilesAsync(command.Files);
    }
}
