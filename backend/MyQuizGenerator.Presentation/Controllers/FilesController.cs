using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Files.Commands.UploadFile;
using MyQuizGenerator.Application.Files.Commands;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Application.Files.Commands.UploadMultipleFiles;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FilesController : BaseApiController
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return ApiBadRequest("File is empty");
        }

        using var stream = file.OpenReadStream();
        var fileRequest = new FileUploadRequest(stream, file.FileName, file.ContentType);
        var result = await Mediator.Send(new UploadFileCommand(fileRequest));
        return ApiOk(new { Url = result });
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return ApiBadRequest("No files uploaded");
        }

        var fileRequests = new List<FileUploadRequest>();
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                fileRequests.Add(new FileUploadRequest(file.OpenReadStream(), file.FileName, file.ContentType));
            }
        }

        var result = await Mediator.Send(new UploadMultipleFilesCommand(fileRequests));
        return ApiOk(new { Urls = result });
    }
}
