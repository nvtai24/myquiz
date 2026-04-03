using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Infrastructure.Settings;
using MyQuizGenerator.Application.Common.Exceptions;
using OpenAI.Chat;
using Polly;
using Polly.Retry;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace MyQuizGenerator.Infrastructure.Services;

public class OpenAiService : IAiService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<OpenAiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ResiliencePipeline _resiliencePipeline;

    public OpenAiService(IOptions<OpenAiSettings> settings, ILogger<OpenAiService> logger, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _chatClient = new ChatClient(_settings.Model, _settings.ApiKey);

        // Define a resilience pipeline with retry logic for 429 Too Many Requests
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<ClientResultException>(ex => ex.Status == 429),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry {AttemptNumber} after {RetryDelay}, reason: {Message}",
                        args.AttemptNumber, args.RetryDelay, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<GeneratedDeckResponse> GenerateDeckFromPdfAsync(Stream fileStream, string fileName, int maxQuestions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PDF deck generation for file: {FileName}, maxQuestions: {MaxQuestions}", fileName, maxQuestions);

        using var uploadBuffer = new MemoryStream();
        fileStream.Position = 0;
        await fileStream.CopyToAsync(uploadBuffer, cancellationToken);
        var fileBytes = uploadBuffer.ToArray();

        var totalPages = TryGetPdfPageCount(fileBytes);
        var targetQuestions = maxQuestions < 0 ? 20 : Math.Clamp(maxQuestions, 5, 50);
        var pageWindowSize = DeterminePdfWindowSize(totalPages);
        var pageRanges = BuildPageRanges(totalPages, pageWindowSize);

        _logger.LogInformation("PDF has {TotalPages} pages, targeting {TargetQuestions} questions with {RangeCount} passes", totalPages, targetQuestions, pageRanges.Count);

        if (pageRanges.Count == 0)
        {
            pageRanges.Add((1, pageWindowSize));
        }

        var fileId = await UploadFileToOpenAiAsync(fileBytes, fileName, cancellationToken);
        _logger.LogInformation("PDF uploaded to OpenAI with fileId: {FileId}", fileId);

        try
        {
            var questionsPerPass = DetermineQuestionsPerPass(targetQuestions, pageRanges.Count);
            var firstRange = pageRanges[0];
            GeneratedDeckResponse initialDeck;
            try
            {
                initialDeck = await GenerateDeckFromPdfRangeAsync(
                    fileId,
                    firstRange.StartPage,
                    firstRange.EndPage,
                    Math.Min(targetQuestions, questionsPerPass),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial PDF pass failed. Falling back to empty metadata and continuing with later passes.");
                initialDeck = new GeneratedDeckResponse
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    Description = string.Empty,
                    Tags = new List<string>(),
                    Questions = new List<GeneratedQuestionResponse>()
                };
            }

            var allQuestions = new List<GeneratedQuestionResponse>();
            AddUniqueQuestions(allQuestions, initialDeck.Questions);

            for (var i = 1; i < pageRanges.Count && allQuestions.Count < targetQuestions; i++)
            {
                var range = pageRanges[i];
                var remaining = targetQuestions - allQuestions.Count;
                var budget = Math.Min(remaining, questionsPerPass);

                try
                {
                    var moreQuestions = await GenerateQuestionsFromPdfRangeAsync(
                        fileId,
                        range.StartPage,
                        range.EndPage,
                        budget,
                        i + 1,
                        cancellationToken);

                    AddUniqueQuestions(allQuestions, moreQuestions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PDF pass {PassIndex} failed for pages {StartPage}-{EndPage}. Skipping this pass.", i + 1, range.StartPage, range.EndPage);
                }
            }

            // If the page-window passes still miss target due to overlap/dedup, request one global fill pass.
            if (allQuestions.Count < targetQuestions)
            {
                var remaining = targetQuestions - allQuestions.Count;
                try
                {
                    var fillQuestions = await GenerateQuestionsFromPdfRangeAsync(
                        fileId,
                        1,
                        totalPages > 0 ? totalPages : pageRanges.Last().EndPage,
                        Math.Min(remaining, Math.Max(4, questionsPerPass + 2)),
                        0,
                        cancellationToken,
                        isGlobalFill: true);

                    AddUniqueQuestions(allQuestions, fillQuestions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Global fill pass failed. Returning currently generated questions.");
                }
            }

            var result = new GeneratedDeckResponse
            {
                Name = string.IsNullOrWhiteSpace(initialDeck.Name) ? Path.GetFileNameWithoutExtension(fileName) : initialDeck.Name,
                Description = initialDeck.Description,
                Tags = initialDeck.Tags ?? new List<string>(),
                Questions = allQuestions.Take(targetQuestions).ToList()
            };

            _logger.LogInformation("PDF deck generation completed. Deck: {DeckName}, Questions: {QuestionCount}", result.Name, result.Questions.Count);

            return result;
        }
        finally
        {
            await TryDeleteOpenAiFileAsync(fileId, cancellationToken);
        }
    }

    public async Task<GeneratedDeckResponse> GenerateDeckAsync(string text, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting text deck generation, text length: {TextLength} characters", text.Length);

        var systemPrompt = """
            You are a helpful assistant that generates a study deck from the provided text.
            The output must be a valid JSON object with the following structure:
            {
              "name": "A suitable concise title for the deck",
              "description": "A brief description of what this deck covers",
              "tags": ["tag1", "tag2", "tag3"],
              "questions": [ ... ]
            }

            Each question object must follow these STRICT rules based on its type:

            Type 0 – Multiple Choice:
            {
              "content": "Question text?",
              "type": 0,
              "options": ["A", "B", "C", "D"],   // EXACTLY 4 distinct options. NEVER use "True"/"False" as options for type 0.
              "correctAnswers": ["A"],             // Must contain exactly 1 value that is in the options array.
              "explanation": "Why A is correct",
              "hint": "Hint"
            }

            Type 1 – True / False:
            {
              "content": "Statement to evaluate.",
              "type": 1,
              "options": ["True", "False"],         // ALWAYS exactly these two values, in this order.
              "correctAnswers": ["True"],            // Must be exactly ["True"] or ["False"].
              "explanation": "Why it is true/false",
              "hint": "Hint"
            }

            Type 2 – Fill in the Blank:
            {
              "content": "The capital of France is ___.",
              "type": 2,
              "options": [],                         // ALWAYS an empty array for fill-in-the-blank.
              "correctAnswers": ["Paris"],           // One or more acceptable answers.
              "explanation": "Explanation",
              "hint": "Hint"
            }

            CRITICAL RULES:
            - For type 0 (Multiple Choice): options MUST have exactly 4 items. Do NOT use "True" and "False" as options. correctAnswers must be a subset of options.
            - For type 1 (True/False): options MUST be ["True", "False"]. correctAnswers MUST be ["True"] or ["False"].
            - For type 2 (Fill in the Blank): options MUST be an empty array [].
            - Every correctAnswer value MUST exactly match one of the options (for type 0 and type 1).
            - Generate a mix of all three question types.
            Only return the JSON object. Do not include any markdown formatting or extra text.
            """;

        var userPrompt = $"Generate a study deck with 5-10 questions from the following text:\n\n{text}";

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };

        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
            var jsonResponse = completion.Content[0].Text;

            // Clean up markdown code blocks if present
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
            }

            try
            {
                var deck = JsonSerializer.Deserialize<GeneratedDeckResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var result = deck ?? new GeneratedDeckResponse();
                result.Questions = SanitizeQuestions(result.Questions);
                _logger.LogInformation("Text deck generation completed. Deck: {DeckName}, Questions: {QuestionCount}", result.Name, result.Questions.Count);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse deck JSON response");
                return new GeneratedDeckResponse();
            }
        }, cancellationToken);
    }

    public async Task<List<GeneratedQuestionResponse>> GenerateQuestionsFromChunkAsync(string chunkText, int chunkIndex, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting chunk question generation, chunk {ChunkIndex}, length: {TextLength} characters", chunkIndex + 1, chunkText.Length);
        var systemPrompt = """
            You are a helpful assistant that generates study questions from the provided text.
            The output must be a valid JSON array of question objects.

            Each question object must follow these STRICT rules based on its type:

            Type 0 – Multiple Choice:
            { "content": "Question?", "type": 0, "options": ["A","B","C","D"], "correctAnswers": ["A"], "explanation": "...", "hint": "..." }
            - options: EXACTLY 4 distinct items. NEVER use "True"/"False" as the only options for type 0.
            - correctAnswers: exactly 1 value that appears in options.

            Type 1 – True / False:
            { "content": "Statement.", "type": 1, "options": ["True","False"], "correctAnswers": ["True"], "explanation": "...", "hint": "..." }
            - options: ALWAYS ["True","False"].
            - correctAnswers: MUST be ["True"] or ["False"].

            Type 2 – Fill in the Blank:
            { "content": "The ___ is ...", "type": 2, "options": [], "correctAnswers": ["answer"], "explanation": "...", "hint": "..." }
            - options: ALWAYS empty array [].

            CRITICAL: correctAnswers must match options exactly (for type 0 and 1). Generate a mix of types.
            Only return the JSON array. No markdown.
            """;

        var userPrompt = $"Generate 5-10 study questions from the following text (chunk {chunkIndex + 1}):\n\n{chunkText}";

        List<ChatMessage> messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        ];

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };

        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
            var jsonResponse = completion.Content[0].Text;

            // Clean up markdown code blocks if present
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Replace("```", "").Trim();
            }

            try
            {
                var questions = JsonSerializer.Deserialize<List<GeneratedQuestionResponse>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var result = SanitizeQuestions(questions ?? new List<GeneratedQuestionResponse>());
                _logger.LogInformation("Chunk question generation completed. Questions: {QuestionCount}", result.Count);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse questions JSON response");
                return new List<GeneratedQuestionResponse>();
            }
        }, cancellationToken);
    }

    private async Task<string> UploadFileToOpenAiAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken)
    {
        using var httpClient = CreateOpenAiHttpClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("user_data"), "purpose");

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", fileName);

        using var response = await httpClient.PostAsync("files", form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI file upload failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new Exception("Failed to upload PDF file to OpenAI.");
        }

        using var json = JsonDocument.Parse(body);
        if (json.RootElement.TryGetProperty("id", out var idElement))
        {
            return idElement.GetString() ?? throw new Exception("OpenAI file upload returned an empty file id.");
        }

        throw new Exception("OpenAI file upload response does not contain file id.");
    }

    private async Task<string> CreateResponseWithFileAsync(string fileId, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        using var httpClient = CreateOpenAiHttpClient();

        var payload = new
        {
            model = _settings.Model,
            max_output_tokens = _settings.MaxTokens,
            temperature = _settings.Temperature,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new { type = "input_text", text = systemPrompt }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_file", file_id = fileId },
                        new { type = "input_text", text = userPrompt }
                    }
                }
            }
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await httpClient.PostAsync(
                    "responses",
                    new StringContent(payloadJson, Encoding.UTF8, "application/json"),
                    cancellationToken);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 429 && attempt < maxAttempts)
                    {
                        var retryDelay = ResolveRateLimitDelay(response, body, attempt);
                        _logger.LogWarning("OpenAI responses rate-limited on attempt {Attempt}. Retrying after {DelaySeconds}s.", attempt, retryDelay.TotalSeconds);
                        await Task.Delay(retryDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogError("OpenAI responses.create failed: {StatusCode} - {Body}", response.StatusCode, body);
                    throw new BadRequestException($"OpenAI responses.create failed ({(int)response.StatusCode}): {TruncateForError(body)}");
                }

                return ExtractOutputText(body);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "OpenAI responses timeout on attempt {Attempt}, retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
        }

        throw new BadRequestException("OpenAI responses request timed out after retries.");
    }

    private static string ExtractOutputText(string responseBody)
    {
        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement) && outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var typeElement)
                        && typeElement.ValueKind == JsonValueKind.String
                        && typeElement.GetString() == "output_text"
                        && contentItem.TryGetProperty("text", out var textElement)
                        && textElement.ValueKind == JsonValueKind.String)
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }
            }
        }

        throw new Exception("OpenAI response does not contain output_text.");
    }

    private static GeneratedDeckResponse DeserializeDeckResponse(string responseText)
    {
        var jsonResponse = responseText.Trim();

        if (jsonResponse.StartsWith("```json"))
        {
            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (jsonResponse.StartsWith("```"))
        {
            jsonResponse = jsonResponse.Replace("```", "").Trim();
        }

        try
        {
            var deck = JsonSerializer.Deserialize<GeneratedDeckResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new GeneratedDeckResponse();
            deck.Questions = SanitizeQuestions(deck.Questions);
            return deck;
        }
        catch (JsonException)
        {
            return new GeneratedDeckResponse();
        }
    }

    private static List<GeneratedQuestionResponse> DeserializeQuestionsResponse(string responseText)
    {
        var jsonResponse = responseText.Trim();

        if (jsonResponse.StartsWith("```json"))
        {
            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (jsonResponse.StartsWith("```"))
        {
            jsonResponse = jsonResponse.Replace("```", "").Trim();
        }

        try
        {
            var questions = JsonSerializer.Deserialize<List<GeneratedQuestionResponse>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<GeneratedQuestionResponse>();
            return SanitizeQuestions(questions);
        }
        catch (JsonException)
        {
            return new List<GeneratedQuestionResponse>();
        }
    }

    private async Task<GeneratedDeckResponse> GenerateDeckFromPdfRangeAsync(
        string fileId,
        int startPage,
        int endPage,
        int questionCount,
        CancellationToken cancellationToken)
    {
        var systemPrompt = """
            You are a helpful assistant that generates a study deck from a PDF.
            Return exactly one valid JSON object with this structure:
            {
              "name": "A suitable concise title",
              "description": "A brief description",
              "tags": ["tag1", "tag2", "tag3"],
              "questions": [ ... ]
            }

            Each question MUST follow these STRICT rules by type:

            Type 0 – Multiple Choice:
            { "content": "?", "type": 0, "options": ["A","B","C","D"], "correctAnswers": ["A"], "explanation": "...", "hint": "..." }
            - EXACTLY 4 distinct options. NEVER use only "True"/"False" for type 0.
            - correctAnswers: exactly 1 value that is in options.

            Type 1 – True / False:
            { "content": "Statement.", "type": 1, "options": ["True","False"], "correctAnswers": ["True"], "explanation": "...", "hint": "..." }
            - options ALWAYS ["True","False"]. correctAnswers MUST be ["True"] or ["False"].

            Type 2 – Fill in the Blank:
            { "content": "The ___ is ...", "type": 2, "options": [], "correctAnswers": ["answer"], "explanation": "...", "hint": "..." }
            - options ALWAYS empty [].

            CRITICAL: For type 0 and 1, every correctAnswer must exactly match an option. Generate a mix of types.
            Return JSON only. No markdown.
            """;

        var userPrompt = $"Focus primarily on pages {startPage}-{endPage}. Generate exactly {questionCount} diverse questions from that page range.";
        var responseText = await CreateResponseWithFileAsync(fileId, systemPrompt, userPrompt, cancellationToken);
        var deck = DeserializeDeckResponse(responseText);

        if (deck.Questions.Count > questionCount)
        {
            deck.Questions = deck.Questions.Take(questionCount).ToList();
        }

        return deck;
    }

    private async Task<List<GeneratedQuestionResponse>> GenerateQuestionsFromPdfRangeAsync(
        string fileId,
        int startPage,
        int endPage,
        int questionCount,
        int passIndex,
        CancellationToken cancellationToken,
        bool isGlobalFill = false)
    {
        var systemPrompt = """
            You generate study questions from a PDF.
            Return exactly one valid JSON array of question objects.

            Each question MUST follow these STRICT rules by type:

            Type 0 – Multiple Choice:
            { "content": "?", "type": 0, "options": ["A","B","C","D"], "correctAnswers": ["A"], "explanation": "...", "hint": "..." }
            - EXACTLY 4 distinct options. NEVER use only "True"/"False" for type 0.
            - correctAnswers: exactly 1 value that is in options.

            Type 1 – True / False:
            { "content": "Statement.", "type": 1, "options": ["True","False"], "correctAnswers": ["True"], "explanation": "...", "hint": "..." }
            - options ALWAYS ["True","False"]. correctAnswers MUST be ["True"] or ["False"].

            Type 2 – Fill in the Blank:
            { "content": "The ___ is ...", "type": 2, "options": [], "correctAnswers": ["answer"], "explanation": "...", "hint": "..." }
            - options ALWAYS empty [].

            CRITICAL: For type 0 and 1, every correctAnswer must exactly match an option. Generate a mix of types.
            Return JSON only. No markdown.
            """;

        var userPrompt = isGlobalFill
            ? $"Global fill pass: generate exactly {questionCount} additional questions not overlapping with previous ones."
            : $"Pass {passIndex}: focus primarily on pages {startPage}-{endPage} and generate exactly {questionCount} additional questions.";

        var responseText = await CreateResponseWithFileAsync(fileId, systemPrompt, userPrompt, cancellationToken);
        var questions = DeserializeQuestionsResponse(responseText);

        if (questions.Count > questionCount)
        {
            questions = questions.Take(questionCount).ToList();
        }

        return questions;
    }

    private static int TryGetPdfPageCount(byte[] pdfBytes)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);
            return document.NumberOfPages;
        }
        catch
        {
            return 0;
        }
    }

    private static List<(int StartPage, int EndPage)> BuildPageRanges(int totalPages, int windowSize)
    {
        var ranges = new List<(int StartPage, int EndPage)>();

        if (totalPages <= 0 || windowSize <= 0)
        {
            return ranges;
        }

        for (var start = 1; start <= totalPages; start += windowSize)
        {
            var end = Math.Min(totalPages, start + windowSize - 1);
            ranges.Add((start, end));
        }

        return ranges;
    }

    private static int DeterminePdfWindowSize(int totalPages)
    {
        // Keep each pass focused but not too sparse.
        if (totalPages <= 0 || totalPages <= 15)
        {
            return 2;
        }

        if (totalPages <= 50)
        {
            return 3;
        }

        return 4; // Very long PDFs need slightly bigger windows to avoid too many calls.
    }

    private static int DetermineQuestionsPerPass(int targetQuestions, int totalRanges)
    {
        if (totalRanges <= 0)
        {
            return 6;
        }

        // Balanced density: enough coverage per pass without making questions too generic.
        var average = (int)Math.Ceiling((double)targetQuestions / totalRanges);
        return Math.Clamp(average, 5, 7);
    }

    private static void AddUniqueQuestions(List<GeneratedQuestionResponse> target, IEnumerable<GeneratedQuestionResponse> source)
    {
        var existing = new HashSet<string>(
            target.Select(q => NormalizeQuestionKey(q.Content)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var question in source)
        {
            if (string.IsNullOrWhiteSpace(question.Content))
            {
                continue;
            }

            var key = NormalizeQuestionKey(question.Content);
            if (existing.Add(key))
            {
                target.Add(question);
            }
        }
    }

    private static string NormalizeQuestionKey(string content)
    {
        return content.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Post-process AI-generated questions to fix common inconsistencies:
    /// - MC question with only True/False options → convert to TrueFalse type
    /// - TrueFalse question with more than 2 options → convert to MC type
    /// - Ensure correctAnswers actually appear in options
    /// - Ensure TrueFalse always has ["True","False"] options
    /// - Ensure FillInTheBlank has empty options
    /// </summary>
    private static List<GeneratedQuestionResponse> SanitizeQuestions(List<GeneratedQuestionResponse> questions)
    {
        foreach (var q in questions)
        {
            var optionsList = q.Options?.ToList() ?? new List<string>();
            var correctList = q.CorrectAnswers?.ToList() ?? new List<string>();

            // Detect mismatch: type says MC but options look like True/False
            if (q.Type == Domain.Enums.QuestionType.MultipleChoice)
            {
                var isTrueFalseOptions = optionsList.Count == 2
                    && optionsList.Any(o => o.Equals("True", StringComparison.OrdinalIgnoreCase))
                    && optionsList.Any(o => o.Equals("False", StringComparison.OrdinalIgnoreCase));

                if (isTrueFalseOptions)
                {
                    // Re-classify as True/False
                    q.Type = Domain.Enums.QuestionType.TrueFalse;
                }
            }

            // Detect mismatch: type says TrueFalse but options have more than 2 items (looks like MC)
            if (q.Type == Domain.Enums.QuestionType.TrueFalse && optionsList.Count > 2)
            {
                q.Type = Domain.Enums.QuestionType.MultipleChoice;
            }

            // Now enforce per-type rules
            switch (q.Type)
            {
                case Domain.Enums.QuestionType.TrueFalse:
                    q.Options = new[] { "True", "False" };
                    // Normalize correct answers to exact "True" or "False"
                    q.CorrectAnswers = correctList
                        .Select(a => a.Trim().Equals("True", StringComparison.OrdinalIgnoreCase) ? "True" : "False")
                        .Distinct()
                        .Take(1)
                        .ToArray();
                    if (q.CorrectAnswers.Length == 0)
                    {
                        q.CorrectAnswers = new[] { "True" };
                    }
                    break;

                case Domain.Enums.QuestionType.FillInTheBlank:
                    q.Options = Array.Empty<string>();
                    // correctAnswers stay as-is (free text)
                    break;

                case Domain.Enums.QuestionType.MultipleChoice:
                default:
                    // Ensure correctAnswers are subset of options (case-insensitive match, use exact option casing)
                    if (optionsList.Count > 0)
                    {
                        var fixedCorrect = new List<string>();
                        foreach (var answer in correctList)
                        {
                            var match = optionsList.FirstOrDefault(o => o.Equals(answer, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                fixedCorrect.Add(match);
                            }
                        }

                        // If none matched (AI hallucinated an answer not in options), keep the first option as answer
                        if (fixedCorrect.Count == 0 && optionsList.Count > 0)
                        {
                            fixedCorrect.Add(optionsList[0]);
                        }

                        q.CorrectAnswers = fixedCorrect.Distinct().ToArray();
                    }
                    break;
            }
        }

        return questions;
    }

    private static TimeSpan ResolveRateLimitDelay(HttpResponseMessage response, string body, int attempt)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var raw = values.FirstOrDefault();
            if (int.TryParse(raw, out var seconds) && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds + 1);
            }
        }

        // Example body fragment: "Please try again in 10.557s."
        var match = Regex.Match(body, @"try again in\s+([0-9]+(?:\.[0-9]+)?)s", RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var secondsFromBody) && secondsFromBody > 0)
        {
            return TimeSpan.FromSeconds(Math.Ceiling(secondsFromBody) + 1);
        }

        return TimeSpan.FromSeconds(Math.Min(30, 3 * attempt));
    }

    private static string TruncateForError(string content, int maxLength = 1200)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "(empty response body)";
        }

        return content.Length <= maxLength ? content : content[..maxLength] + "...(truncated)";
    }

    private async Task TryDeleteOpenAiFileAsync(string fileId, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = CreateOpenAiHttpClient();
            using var _ = await httpClient.DeleteAsync($"files/{fileId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temporary OpenAI file {FileId}", fileId);
        }
    }

    private HttpClient CreateOpenAiHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.Timeout = TimeSpan.FromMinutes(10);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        return client;
    }
}

