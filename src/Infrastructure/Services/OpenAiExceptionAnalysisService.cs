using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OpenAiExceptionAnalysisService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiExceptionAnalysisService> logger)
    : IExceptionAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string SystemPrompt = """
        You are a senior .NET software engineer performing production exception triage.
        Given an exception with its type, message, stack trace, and inner exceptions,
        provide a concise, actionable analysis.

        Return ONLY valid JSON with no markdown formatting, no code fences, no extra text.
        Use this exact schema:
        {
          "analysis": "A clear, professional summary of: (1) Root cause, (2) Why it happened, (3) Suggested fix with code guidance if applicable, (4) Prevention recommendations.",
          "suggested_prompt": "A ready-to-use GitHub Copilot prompt that a developer can copy-paste to fix this issue. The prompt should be specific, include file/class hints from the stack trace, and describe the exact change needed. Set to null if the exception is a validation exception."
        }
        Rules:
        - Keep the analysis under 500 words but thorough.
        - The suggested_prompt should be a professional instruction for an AI coding assistant.
        - If the exception is a validation exception, set suggested_prompt to null.
        - Reference specific classes and methods from the stack trace when possible.
        - Be actionable, not generic.
        """;

    public async Task<ExceptionAnalysisResult> AnalyseAsync(
        string exceptionType,
        string message,
        string? stackTrace,
        string? innerException,
        bool isValidationException,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = options.Value;

            var userContent = new StringBuilder();
            userContent.AppendLine($"**Exception Type:** {exceptionType}");
            userContent.AppendLine($"**Message:** {message}");
            userContent.AppendLine($"**Is Validation Exception:** {isValidationException}");

            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                userContent.AppendLine($"**Stack Trace:**\n{stackTrace}");
            }

            if (!string.IsNullOrWhiteSpace(innerException))
            {
                userContent.AppendLine($"**Inner Exception:**\n{innerException}");
            }

            var requestBody = new
            {
                model = config.Model,
                max_tokens = config.MaxTokens,
                temperature = 0.3,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userContent.ToString() }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "OpenAI exception analysis returned {StatusCode}: {Body}",
                    (int)response.StatusCode, responseBody);

                return new ExceptionAnalysisResult(
                    "AI analysis unavailable — OpenAI API returned an error.",
                    null);
            }

            return ParseResponse(responseBody, isValidationException);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to obtain AI analysis for exception");
            return new ExceptionAnalysisResult(
                "AI analysis unavailable — an error occurred while contacting the AI service.",
                null);
        }
    }

    private ExceptionAnalysisResult ParseResponse(string responseBody, bool isValidationException)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var firstNewLine = content.IndexOf('\n');
            if (firstNewLine > 0)
                content = content[(firstNewLine + 1)..];
            if (content.EndsWith("```"))
                content = content[..^3];
            content = content.Trim();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AiAnalysisResponse>(content, JsonOptions);
            if (parsed is not null)
            {
                return new ExceptionAnalysisResult(
                    parsed.Analysis ?? "No analysis provided.",
                    isValidationException ? null : parsed.SuggestedPrompt);
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse AI analysis JSON; using raw content");
        }

        return new ExceptionAnalysisResult(content, null);
    }

    private sealed class AiAnalysisResponse
    {
        [JsonPropertyName("analysis")]
        public string? Analysis { get; set; }

        [JsonPropertyName("suggested_prompt")]
        public string? SuggestedPrompt { get; set; }
    }
}
