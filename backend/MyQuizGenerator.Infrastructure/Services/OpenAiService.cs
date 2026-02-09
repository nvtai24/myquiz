using System.Text.Json;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Infrastructure.Settings;
using OpenAI.Chat;

namespace MyQuizGenerator.Infrastructure.Services;

public class OpenAiService : IAiService
{
    private readonly ChatClient _chatClient;
    private readonly OpenAiSettings _settings;

    public OpenAiService(IOptions<OpenAiSettings> settings)
    {
        _settings = settings.Value;
        _chatClient = new ChatClient(_settings.Model, _settings.ApiKey);
    }

    public async Task<GeneratedDeckResponse> GenerateDeckAsync(string text, CancellationToken cancellationToken)
    {
        var systemPrompt = """
            You are a helpful assistant that generates a study deck from the provided text.
            The output must be a valid JSON object with the following structure:
            {
              "name": "A suitable concise title for the deck",
              "description": "A brief description of what this deck covers",
              "tags": ["tag1", "tag2", "tag3"],
              "questions": [
                {
                  "content": "Question text",
                  "type": 0, // 0 for Multiple Choice, 1 for True/False, 2 for Fill in the Blank
                  "options": ["Option 1", "Option 2", "Option 3", "Option 4"], // Only for Multiple Choice, include the correct answer
                  "correctAnswers": ["Option 1"], // List of correct answers
                  "explanation": "Explanation for the answer",
                  "hint": "Hint for the question"
                }
              ]
            }
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
            Temperature = _settings.Temperature,
            MaxOutputTokenCount = _settings.MaxTokens
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

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
            return deck ?? new GeneratedDeckResponse();
        }
        catch (JsonException)
        {
            // Handle parsing error or retry - for now return empty
            return new GeneratedDeckResponse();
        }
    }
}
