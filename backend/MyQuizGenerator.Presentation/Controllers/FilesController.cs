using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Files.Commands.UploadFile;

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
        var result = await Mediator.Send(new UploadFileCommand(stream, file.FileName, file.ContentType));
        return ApiOk(new { Url = result });
    }
}
