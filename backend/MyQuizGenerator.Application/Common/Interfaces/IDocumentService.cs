namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IDocumentService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken cancellationToken);
}
