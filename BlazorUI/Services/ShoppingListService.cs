using System.Net.Http.Json;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class ShoppingListService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IShoppingListService
{
    private const string BasePath = "api/shoppinglists";

    public Task<ApiResult<PaginatedList<ShoppingListBriefDto>>> GetShoppingListsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ShoppingListCategory? category = null,
        bool? isCompleted = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("category", category?.ToString()),
            ("isCompleted", isCompleted?.ToString()),
            ("searchTerm", searchTerm));

        return GetAsync<PaginatedList<ShoppingListBriefDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<ShoppingListDetailDto>> GetShoppingListByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<ShoppingListDetailDto>($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<Guid>> CreateShoppingListAsync(
        CreateShoppingListRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> UpdateShoppingListAsync(
        Guid id, UpdateShoppingListRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{id}", request, cancellationToken);
    }

    public Task<ApiResult> DeleteShoppingListAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<ShoppingItemDto>> AddItemAsync(
        Guid listId, AddShoppingItemRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<ShoppingItemDto>($"{BasePath}/{listId}/items", request, cancellationToken);
    }

    public Task<ApiResult> UpdateItemAsync(
        Guid listId, Guid itemId, UpdateShoppingItemRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{listId}/items/{itemId}", request, cancellationToken);
    }

    public Task<ApiResult> RemoveItemAsync(
        Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{listId}/items/{itemId}", cancellationToken);
    }

    public Task<ApiResult> ToggleItemAsync(
        Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{listId}/items/{itemId}/toggle", cancellationToken: cancellationToken);
    }

    public Task<ApiResult> ToggleAllItemsAsync(
        Guid listId, bool check, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{listId}/items/toggle-all?check={check}", cancellationToken: cancellationToken);
    }

    public Task<ApiResult<ShoppingItemDto>> AddItemFromBillItemAsync(
        Guid listId, AddShoppingItemFromBillItemRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<ShoppingItemDto>($"{BasePath}/{listId}/items/from-bill-item", request, cancellationToken);
    }

    public async Task<ApiResult<ProcessReceiptResultDto>> ProcessReceiptAsync(
        Guid listId, Stream fileStream, string fileName, string contentType,
        List<SplitRequest>? splits = null, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var queryParts = new List<string>();
        if (splits is { Count: > 0 })
        {
            var encoded = string.Join(",", splits.Select(s =>
                s.Percentage.HasValue ? $"{s.UserId}:{s.Percentage.Value}" : s.UserId));
            queryParts.Add($"splitUserIds={encoded}");
        }

        var queryString = queryParts.Count > 0
            ? "?" + string.Join("&", queryParts)
            : string.Empty;

        using var response = await Http.PostAsync(
            $"{BasePath}/{listId}/process-receipt{queryString}", content, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ProcessReceiptResultDto>(
                cancellationToken: cancellationToken);
            return ApiResult<ProcessReceiptResultDto>.Success(result!, statusCode);
        }

        var problem = await TryReadProblemFromResponseAsync(response, cancellationToken);
        return ApiResult<ProcessReceiptResultDto>.Failure(problem, statusCode);
    }

    public Task<ApiResult> ResolveCrossListMatchAsync(
        Guid targetListId, ResolveCrossListMatchRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync($"{BasePath}/{targetListId}/resolve-cross-list-match", request, cancellationToken);
    }

    public Task<ApiResult<ShoppingItemGroupResultDto>> GroupItemsAsync(
        Guid listId, CancellationToken cancellationToken = default)
    {
        return GetAsync<ShoppingItemGroupResultDto>($"{BasePath}/{listId}/group-items", cancellationToken);
    }

    private static async Task<ApiProblemDetails> TryReadProblemFromResponseAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
                cancellationToken: cancellationToken);
            if (problem is not null) return problem;
        }
        catch { }

        return new ApiProblemDetails
        {
            Title = "Error",
            Detail = $"Request failed with status {(int)response.StatusCode}.",
            Status = (int)response.StatusCode
        };
    }
}
