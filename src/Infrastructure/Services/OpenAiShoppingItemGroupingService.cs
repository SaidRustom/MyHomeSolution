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

public sealed class OpenAiShoppingItemGroupingService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiShoppingItemGroupingService> logger)
    : IShoppingItemGroupingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string SystemPrompt = """
        You are a grocery store layout expert. Given a list of shopping item names,
        group them into logical store-aisle categories that a shopper would encounter
        while walking through a typical grocery or retail store.

        Return ONLY valid JSON with no markdown formatting, no code fences, no extra text.
        Use this exact schema:
        {
          "groups": [
            {
              "category": "Human-readable category name (e.g. Fresh Produce, Dairy & Eggs, Meat & Seafood)",
              "icon": "A Google Material Symbols icon name that best represents the category (e.g. 'nutrition' for produce, 'egg' for dairy, 'set_meal' for meat)",
              "sort_order": 1,
              "item_names": ["exact item name from the input list"]
            }
          ]
        }

        Rules:
        - Every item from the input MUST appear in exactly one group. Do not drop or add items.
        - Use the EXACT item names provided — do not rename, abbreviate, or modify them.
        - Use natural, shopper-friendly category names. Typical categories include but are not limited to:
          Fresh Produce, Dairy & Eggs, Meat & Seafood, Bakery, Deli, Frozen Foods,
          Pantry & Canned Goods, Snacks & Chips, Beverages, Breakfast & Cereal,
          Condiments & Sauces, Pasta & Grains, Baking Supplies, Spices & Seasonings,
          Household & Cleaning, Personal Care, Baby & Kids, Pet Supplies, Health & Wellness, Other.
        - Only create categories that have at least one item. Do not create empty categories.
        - sort_order should reflect a logical shopping route (fresh → refrigerated → frozen → pantry → non-food).
        - If an item is ambiguous, use your best judgment to place it in the most likely category.
        - Choose descriptive Material Symbols icon names. Common mappings:
          Fresh Produce → nutrition, Dairy & Eggs → egg, Meat & Seafood → set_meal,
          Bakery → bakery_dining, Frozen Foods → ac_unit, Pantry → inventory_2,
          Beverages → local_cafe, Snacks → cookie, Household → cleaning_services,
          Personal Care → soap, Health → health_and_safety, Other → category.
        - Keep the total number of categories reasonable (3–12 depending on item diversity).
        """;

    public async Task<ShoppingItemGroupResult> GroupItemsAsync(
        IReadOnlyList<string> itemNames,
        CancellationToken cancellationToken = default)
    {
        if (itemNames.Count == 0)
            return new ShoppingItemGroupResult();

        var config = options.Value;

        var itemList = string.Join("\n", itemNames.Select((name, i) => $"{i + 1}. {name}"));
        var userText = $"Group these {itemNames.Count} shopping items into store-aisle categories:\n\n{itemList}";

        var requestBody = new
        {
            model = config.Model,
            max_tokens = config.MaxTokens,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userText }
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
                "OpenAI API returned {StatusCode} for item grouping: {Body}",
                (int)response.StatusCode, responseBody);

            throw new InvalidOperationException(
                $"Item grouping failed. OpenAI returned status {(int)response.StatusCode}.");
        }

        return ParseResponse(responseBody, itemNames);
    }

    private ShoppingItemGroupResult ParseResponse(string responseBody, IReadOnlyList<string> originalNames)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("OpenAI returned an empty response for item grouping.");

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

        logger.LogDebug("Item grouping raw content: {Content}", content);

        var parsed = JsonSerializer.Deserialize<OpenAiGroupResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize item grouping response.");

        // Build a set of all original names for validation
        var remaining = new HashSet<string>(originalNames, StringComparer.OrdinalIgnoreCase);

        var groups = (parsed.Groups ?? [])
            .Where(g => g.ItemNames is { Count: > 0 })
            .Select(g => new ShoppingItemGroup
            {
                Category = g.Category ?? "Other",
                Icon = g.Icon ?? "category",
                SortOrder = g.SortOrder,
                ItemNames = g.ItemNames!
                    .Where(n => remaining.Remove(n))
                    .ToList()
            })
            .Where(g => g.ItemNames.Count > 0)
            .OrderBy(g => g.SortOrder)
            .ToList();

        // If any items were missed by the AI, put them in an "Other" group
        if (remaining.Count > 0)
        {
            var maxSort = groups.Count > 0 ? groups.Max(g => g.SortOrder) + 1 : 1;
            groups.Add(new ShoppingItemGroup
            {
                Category = "Other",
                Icon = "category",
                SortOrder = maxSort,
                ItemNames = remaining.ToList()
            });
        }

        return new ShoppingItemGroupResult { Groups = groups };
    }

    private sealed record OpenAiGroupResponse
    {
        [JsonPropertyName("groups")]
        public List<OpenAiGroupItem>? Groups { get; init; }
    }

    private sealed record OpenAiGroupItem
    {
        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("icon")]
        public string? Icon { get; init; }

        [JsonPropertyName("sort_order")]
        public int SortOrder { get; init; }

        [JsonPropertyName("item_names")]
        public List<string>? ItemNames { get; init; }
    }
}
