namespace MyQuizGenerator.Application.Files.DTOs;

public record FileUploadRequest(Stream FileStream, string FileName, string ContentType);
