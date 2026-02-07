using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Files.Commands.UploadFile;

public record UploadFileCommand(Stream FileStream, string FileName, string ContentType) : IRequest<string>;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, string>
{
    private readonly IFileService _fileService;
    private readonly IRepository<string, UploadedFile> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadFileCommandHandler(
        IFileService fileService,
        IRepository<string, UploadedFile> repository,
        IUnitOfWork unitOfWork)
    {
        _fileService = fileService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var url = await _fileService.UploadFileAsync(request.FileStream, request.FileName, request.ContentType);

        var uploadedFile = new UploadedFile
        {
            FileName = request.FileName,
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.FileStream.Length,
            Url = url
        };

        await _repository.AddAsync(uploadedFile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return url;
    }
}
