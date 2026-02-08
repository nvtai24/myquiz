using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Files.Commands.UploadMultipleFiles;

public record UploadMultipleFilesCommand(List<FileUploadRequest> Files) : IRequest<List<string>>;

public class UploadMultipleFilesCommandHandler : IRequestHandler<UploadMultipleFilesCommand, List<string>>
{
    private readonly IFileService _fileService;
    private readonly IRepository<string, UploadedFile> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadMultipleFilesCommandHandler(
        IFileService fileService,
        IRepository<string, UploadedFile> repository,
        IUnitOfWork unitOfWork)
    {
        _fileService = fileService;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(UploadMultipleFilesCommand command, CancellationToken cancellationToken)
    {
        // Capture sizes before upload
        var sizes = command.Files.Select(f => f.FileStream.Length).ToList();

        var uploadedUrls = await _fileService.UploadMultipleFilesAsync(command.Files);

        var uploadedEntities = new List<UploadedFile>();

        for (int i = 0; i < command.Files.Count; i++)
        {
            var uploadedFile = new UploadedFile
            {
                FileName = command.Files[i].FileName,
                OriginalFileName = command.Files[i].FileName,
                ContentType = command.Files[i].ContentType,
                Size = sizes[i],
                Url = uploadedUrls[i]
            };
            uploadedEntities.Add(uploadedFile);
        }

        await _repository.AddRangeAsync(uploadedEntities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return uploadedUrls;
    }
}
