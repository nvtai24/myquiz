using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Controller for AI-powered deck generation with SSE streaming.
/// </summary>
[Authorize]
[Route("api/decks/generate")]
[ApiController]
public class DeckGenerationController : BaseApiController
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    private const int ChunkSize = 15000;
    private const int MaxParallelChunks = 5;

    public DeckGenerationController(
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

    /// <summary>
    /// Generates quiz questions from an uploaded file with SSE streaming.
    /// Streams progress and questions as they are generated.
    /// </summary>
    /// <param name="file">The file to process (PDF, DOCX, PPTX, TXT).</param>
    [HttpPost("stream")]
    public async Task GenerateStream(IFormFile file, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            if (file == null || file.Length == 0)
            {
                await SendSseEventAsync("error", new { message = "No file uploaded." }, jsonOptions, cancellationToken);
                return;
            }

            var userId = _currentUserService.UserId ?? string.Empty;

            // 1. Check subscription plan
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
                    await SendSseEventAsync("error", new { message = $"Daily limit reached ({activePlan.DailyGenerateLimit})" }, jsonOptions, cancellationToken);
                    return;
                }
            }

            // 2. Extract text
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            var text = await _documentService.ExtractTextAsync(memoryStream, file.FileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                await SendSseEventAsync("error", new { message = "Could not extract text from file." }, jsonOptions, cancellationToken);
                return;
            }

            // 3. Chunk and generate
            var chunks = ChunkText(text, ChunkSize);
            var maxQuestions = activePlan?.MaxQuestionsPerGenerate ?? -1;
            var totalChunks = chunks.Count;

            await SendSseEventAsync("start", new { totalChunks, maxQuestions }, jsonOptions, cancellationToken);

            // First chunk - get deck metadata
            cancellationToken.ThrowIfCancellationRequested();
            var generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);

            await SendSseEventAsync("metadata", new
            {
                name = generatedDeck.Name,
                description = generatedDeck.Description,
                tags = generatedDeck.Tags
            }, jsonOptions, cancellationToken);

            var allQuestions = new List<GeneratedQuestionResponse>(generatedDeck.Questions);

            // Check if first chunk already has enough questions
            if (maxQuestions >= 0 && allQuestions.Count >= maxQuestions)
            {
                // Trim to limit and skip remaining chunks
                allQuestions = allQuestions.Take(maxQuestions).ToList();

                await SendSseEventAsync("questions", new
                {
                    chunk = 1,
                    totalChunks,
                    questions = allQuestions,
                    totalQuestions = allQuestions.Count
                }, jsonOptions, cancellationToken);
            }
            else
            {
                // Send first batch of questions
                await SendSseEventAsync("questions", new
                {
                    chunk = 1,
                    totalChunks,
                    questions = generatedDeck.Questions,
                    totalQuestions = generatedDeck.Questions.Count
                }, jsonOptions, cancellationToken);

                var remainingChunks = chunks.Skip(1).ToList();

                // Process remaining chunks in parallel batches
                for (int batchStart = 0; batchStart < remainingChunks.Count; batchStart += MaxParallelChunks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Early stop - check before starting new batch
                    if (maxQuestions >= 0 && allQuestions.Count >= maxQuestions)
                    {
                        break;
                    }

                    // Calculate how many more questions we need
                    var questionsNeeded = maxQuestions >= 0 ? maxQuestions - allQuestions.Count : int.MaxValue;

                    // Limit batch size based on questions needed (estimate ~5 questions per chunk)
                    var estimatedChunksNeeded = maxQuestions >= 0 ? Math.Max(1, (int)Math.Ceiling(questionsNeeded / 5.0)) : MaxParallelChunks;
                    var chunksToProcess = Math.Min(MaxParallelChunks, estimatedChunksNeeded);

                    var batch = remainingChunks
                        .Skip(batchStart)
                        .Take(chunksToProcess)
                        .Select((chunk, idx) => (Chunk: chunk, Index: batchStart + idx + 1))
                        .ToList();

                    var tasks = batch.Select(item =>
                        _aiService.GenerateQuestionsFromChunkAsync(item.Chunk, item.Index, cancellationToken));

                    var batchResults = await Task.WhenAll(tasks);

                    // Send each batch result
                    foreach (var (questions, idx) in batchResults.Select((q, i) => (q, i)))
                    {
                        // Check if we've reached limit before adding more
                        if (maxQuestions >= 0 && allQuestions.Count >= maxQuestions)
                        {
                            break;
                        }

                        var questionsToAdd = questions;

                        // Trim this batch if it would exceed limit
                        if (maxQuestions >= 0)
                        {
                            var remaining = maxQuestions - allQuestions.Count;
                            if (questions.Count > remaining)
                            {
                                questionsToAdd = questions.Take(remaining).ToList();
                            }
                        }

                        allQuestions.AddRange(questionsToAdd);
                        var chunkNumber = batchStart + idx + 2; // +2 because chunk 1 is already processed

                        await SendSseEventAsync("questions", new
                        {
                            chunk = chunkNumber,
                            totalChunks,
                            questions = questionsToAdd,
                            totalQuestions = allQuestions.Count
                        }, jsonOptions, cancellationToken);

                        // Check again after adding - if limit reached, break out
                        if (maxQuestions >= 0 && allQuestions.Count >= maxQuestions)
                        {
                            break;
                        }
                    }
                }
            }

            // Increment rate limit
            if (activePlan != null && activePlan.DailyGenerateLimit >= 0)
            {
                await _rateLimitService.IncrementDailyGenerateCountAsync(userId, cancellationToken);
            }

            // Send completion
            await SendSseEventAsync("complete", new
            {
                name = generatedDeck.Name,
                description = generatedDeck.Description,
                tags = generatedDeck.Tags,
                totalQuestions = allQuestions.Count
            }, jsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is expected
        }
        catch (Exception ex)
        {
            try
            {
                await SendSseEventAsync("error", new { message = ex.Message }, jsonOptions, CancellationToken.None);
            }
            catch
            {
                // Ignore if we can't send the error
            }
        }
    }

    private async Task SendSseEventAsync<T>(string eventType, T data, JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, jsonOptions);
        await Response.WriteAsync($"event: {eventType}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static List<string> ChunkText(string text, int chunkSize)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;

        var paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            if (paragraph.Length > chunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

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

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}
