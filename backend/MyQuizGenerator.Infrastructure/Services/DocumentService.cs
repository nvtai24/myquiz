using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using MyQuizGenerator.Application.Common.Interfaces;
using UglyToad.PdfPig;

namespace MyQuizGenerator.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        return extension switch
        {
            ".pdf" => ExtractFromPdf(fileStream),
            ".docx" => ExtractFromDocx(fileStream),
            ".pptx" => ExtractFromPptx(fileStream),
            ".txt" => await ExtractFromTxt(fileStream, cancellationToken),
            _ => throw new ArgumentException($"Unsupported file format: {extension}")
        };
    }

    private string ExtractFromPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private string ExtractFromDocx(Stream stream)
    {
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var sb = new StringBuilder();

        var body = wordDoc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }

        return sb.ToString();
    }

    private string ExtractFromPptx(Stream stream)
    {
        using var pptDoc = PresentationDocument.Open(stream, false);
        var sb = new StringBuilder();

        var presentationPart = pptDoc.PresentationPart;
        if (presentationPart == null) return string.Empty;

        foreach (var slideId in presentationPart.Presentation.SlideIdList?.ChildElements.Cast<SlideId>() ?? Enumerable.Empty<SlideId>())
        {
            if (slideId.RelationshipId == null) continue;

            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
            var slideText = new StringBuilder();

            // Extract text from shapes
            foreach (var paragraph in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
            {
                foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                {
                    slideText.Append(text.Text + " ");
                }
                slideText.AppendLine();
            }
            sb.AppendLine(slideText.ToString());
        }

        return sb.ToString();
    }

    private async Task<string> ExtractFromTxt(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
