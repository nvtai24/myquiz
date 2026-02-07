namespace MyQuizGenerator.Application.Files.Commands.UploadFile;

public record FileUploadRequest(Stream FileStream, string FileName, string ContentType);
