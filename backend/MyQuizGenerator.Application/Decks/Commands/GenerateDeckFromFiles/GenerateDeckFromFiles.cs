using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles;

public record GenerateDeckFromFilesCommand(Stream FileStream, string FileName, string ContentType) : IRequest<GeneratedDeckResponse>;

public class GenerateDeckFromFilesCommandHandler : IRequestHandler<GenerateDeckFromFilesCommand, GeneratedDeckResponse>
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public GenerateDeckFromFilesCommandHandler(
        IDocumentService documentService,
        IAiService aiService,
        ICurrentUserService currentUserService,
        IRateLimitService rateLimitService,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _documentService = documentService;
        _aiService = aiService;
        _currentUserService = currentUserService;
        _rateLimitService = rateLimitService;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    private const int ChunkSize = 15000; // Characters per chunk

    public async Task<GeneratedDeckResponse> Handle(GenerateDeckFromFilesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;

        // 1. Check DailyGenerateLimit from user's active subscription plan (-1 = unlimited)
        var now = DateTime.UtcNow;
        var activePlan = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == userId && usp.StartDate <= now && usp.EndDate > now)
            .OrderByDescending(usp => usp.SubscriptionPlan.Order)
            .Select(usp => usp.SubscriptionPlan)
            .FirstOrDefaultAsync(cancellationToken);

        if (activePlan != null && activePlan.DailyGenerateLimit >= 0)
        {
            var currentCount = await _rateLimitService.GetDailyGenerateCountAsync(userId, cancellationToken);
            if (currentCount >= activePlan.DailyGenerateLimit)
            {
                throw new BadRequestException(
                    $"You have reached the daily generation limit ({activePlan.DailyGenerateLimit}) for your current plan ({activePlan.Name}). Please try again tomorrow or upgrade your plan.");
            }
        }

        // 2. Extract text from the file
        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var text = await _documentService.ExtractTextAsync(memoryStream, request.FileName, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception("Could not extract text from file.");
        }

        // 3. Chunk text and generate questions
        var chunks = ChunkText(text, ChunkSize);
        GeneratedDeckResponse generatedDeck;

        if (chunks.Count == 1)
        {
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);
        }
        else
        {
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);

            for (int i = 1; i < chunks.Count; i++)
            {
                var additionalQuestions = await _aiService.GenerateQuestionsFromChunkAsync(chunks[i], i, cancellationToken);
                generatedDeck.Questions.AddRange(additionalQuestions);
            }
        }

        // 4. Limit questions based on MaxQuestionsPerGenerate from subscription plan (-1 = unlimited)
        var maxQuestions = activePlan?.MaxQuestionsPerGenerate ?? 0;
        if (maxQuestions >= 0 && generatedDeck.Questions.Count > maxQuestions)
        {
            generatedDeck.Questions = generatedDeck.Questions.Take(maxQuestions).ToList();
        }

        // 5. Increment counter only after successful generation (AI errors don't consume quota, -1 = unlimited)
        if (activePlan != null && activePlan.DailyGenerateLimit >= 0)
        {
            await _rateLimitService.IncrementDailyGenerateCountAsync(userId, cancellationToken);
        }

        // 6. Return generated deck (no DB save - FE will call save API separately)
        return generatedDeck;
    }

    private static List<string> ChunkText(string text, int chunkSize)
    {
        var chunks = new List<string>();

        if (string.IsNullOrEmpty(text))
            return chunks;

        // Try to split at paragraph boundaries for better context
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            // If adding this paragraph exceeds chunk size, save current chunk and start new one
            if (currentChunk.Length + paragraph.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            // If single paragraph is larger than chunk size, split it
            if (paragraph.Length > chunkSize)
            {
                // Save any pending content first
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // Split large paragraph by sentences or fixed size
                for (int i = 0; i < paragraph.Length; i += chunkSize)
                {
                    var length = Math.Min(chunkSize, paragraph.Length - i);
                    chunks.Add(paragraph.Substring(i, length).Trim());
                }
            }
            else
            {
                currentChunk.AppendLine(paragraph);
            }
        }

        // Add remaining content
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}
