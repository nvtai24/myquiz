using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Files.Commands.UploadFile;

public record UploadFileCommand(FileUploadRequest File) : IRequest<string>;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, string>
{
    private readonly IFileService _fileService;

    //private readonly IRepository<string, UploadedFile> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadFileCommandHandler(
        IFileService fileService,
        IUnitOfWork unitOfWork)
    {
        _fileService = fileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        return await _fileService.UploadFileAsync(request.File);
    }
}
