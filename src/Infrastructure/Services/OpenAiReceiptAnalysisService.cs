using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Infrastructure.Configuration;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class OpenAiReceiptAnalysisService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiReceiptAnalysisService> logger)
    : IReceiptAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string SystemPrompt = """
        You are a precise receipt parser. Analyze the receipt image and extract structured data.
        Return ONLY valid JSON with no markdown formatting, no code fences, no extra text.
        Use this exact schema:
        {
          "store_name": "string",
          "store_address": "string or null",
          "transaction_date": "ISO 8601 datetime string",
          "currency": "3-letter ISO currency code",
          "items": [
            {
              "name": "simplified item name (with brand)",
              "generic_name": "generic item name without brand",
              "price": number,
              "quantity": integer,
              "is_taxable": boolean
            }
          ],
          "subtotal": number,
          "discount": number,
          "total": number
        }
        Rules:
        - "name": Simplify item names to be human-readable (e.g. "ORG BNS CHKN BRST" → "Organic Boneless Chicken Breast"). Keep brand names here.
        - "generic_name": A plain generic name without any brand. Drop brand names entirely (e.g. "Organic Banana" from "Dole Organic Banana", "Chicken Breast" from "Maple Leaf Chicken Breast", "Paper Towels" from "Bounty Paper Towels").
        - price is the line total (unit price × quantity). Do not duplicate: if the receipt shows qty and a line total, use the line total as price.
        - discount is the total discount amount (positive number). Use 0 if none.
        - "is_taxable": In Ontario, Canada, most basic groceries (fresh produce, dairy, bread, meat, etc.) are zero-rated (HST exempt). Prepared foods, snacks, candy, carbonated drinks, alcohol, household items, cleaning supplies, personal care, and non-food items are taxable. Set true if the item is taxable under Ontario HST rules.
        - If currency is ambiguous, default to "CAD".
        - If the date is missing, use the current date and time.

        ACCURACY IS CRITICAL:
        - The sum of all item prices MUST equal the subtotal. Verify this before responding.
        - The total MUST equal subtotal - discount + tax. Read the receipt's printed total and use it exactly.
        - If a value is hard to read or ambiguous, prefer the receipt's printed subtotal/total over your own calculation.
        - Do NOT guess or fabricate prices. If an item's price is unreadable, skip that item entirely rather than entering an incorrect value.
        - Do NOT invent items that are not clearly visible on the receipt.
        - quantity should be 1 unless the receipt explicitly shows a different quantity for the line item.
        - If the receipt image is too blurry, cut off, or otherwise unreadable, return fewer items rather than guessing. It is better to be incomplete than inaccurate.
        """ ;

    public async Task<ReceiptAnalysisResult> AnalyzeAsync(
        Stream imageStream, string contentType, CancellationToken cancellationToken = default)
    {
        return await AnalyzeCoreAsync(
            imageStream, contentType,
            "Parse this receipt and return the JSON.",
            cancellationToken);
    }

    public async Task<ReceiptAnalysisResult> AnalyzeAsync(
        Stream imageStream, string contentType,
        IReadOnlyList<string> existingItemNames,
        CancellationToken cancellationToken = default)
    {
        var userText = "Parse this receipt and return the JSON.";

        // Cap at 100 items to stay within the model's context window
        var namesToInclude = existingItemNames.Count > 100
            ? existingItemNames.Take(100).ToList()
            : existingItemNames;

        if (namesToInclude.Count > 0)
        {
            var itemsList = string.Join("\n", namesToInclude.Select(n => $"- {n}"));
            userText += $"""


                IMPORTANT: The following items already exist on the user's shopping list.
                When naming receipt items, if an item closely matches one of these existing names
                (even if abbreviated, misspelled, or using a different language variant on the receipt),
                use the EXACT name from this list instead of creating a new name:
                {itemsList}
                """;
        }

        return await AnalyzeCoreAsync(imageStream, contentType, userText, cancellationToken);
    }

    private async Task<ReceiptAnalysisResult> AnalyzeCoreAsync(
        Stream imageStream, string contentType, string userText,
        CancellationToken cancellationToken)
    {
        var config = options.Value;

        var imageBytes = await ReadStreamAsync(imageStream, cancellationToken);
        var base64Image = Convert.ToBase64String(imageBytes);
        var dataUri = $"data:{contentType};base64,{base64Image}";

        var requestBody = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userText },
                        new { type = "image_url", image_url = new { url = dataUri, detail = "high" } }
                    }
                }
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
            logger.LogError(
                "OpenAI API returned {StatusCode}: {Body}",
                (int)response.StatusCode, responseBody);

            throw new InvalidOperationException(
                $"Receipt analysis failed. OpenAI returned status {(int)response.StatusCode}.");
        }

        return ParseResponse(responseBody);
    }

    private ReceiptAnalysisResult ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("OpenAI returned an empty response.");

        // Strip markdown fences if present
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

        logger.LogDebug("Receipt analysis raw content: {Content}", content);

        var parsed = JsonSerializer.Deserialize<OpenAiReceiptResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize receipt analysis response.");

        return new ReceiptAnalysisResult
        {
            StoreName = parsed.StoreName ?? "Unknown Store",
            StoreAddress = parsed.StoreAddress,
            TransactionDate = parsed.TransactionDate,
            Currency = string.IsNullOrWhiteSpace(parsed.Currency) ? "CAD" : parsed.Currency,
            Subtotal = parsed.Subtotal,
            Discount = parsed.Discount,
            Total = parsed.Total,
            Items = parsed.Items?.Select(i => new ReceiptLineItem
            {
                Name = i.Name ?? "Unknown Item",
                GenericName = i.GenericName,
                Price = i.Price,
                Quantity = i.Quantity < 1 ? 1 : i.Quantity,
                IsTaxable = i.IsTaxable
            }).ToList() ?? []
        };
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private sealed record OpenAiReceiptResponse
    {
        [JsonPropertyName("store_name")]
        public string? StoreName { get; init; }

        [JsonPropertyName("store_address")]
        public string? StoreAddress { get; init; }

        [JsonPropertyName("transaction_date")]
        public DateTimeOffset TransactionDate { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("items")]
        public List<OpenAiReceiptItem>? Items { get; init; }

        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; init; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; init; }

        [JsonPropertyName("total")]
        public decimal Total { get; init; }
    }

    private sealed record OpenAiReceiptItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("generic_name")]
        public string? GenericName { get; init; }

        [JsonPropertyName("price")]
        public decimal Price { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; } = 1;

        [JsonPropertyName("is_taxable")]
        public bool IsTaxable { get; init; }
    }
}
