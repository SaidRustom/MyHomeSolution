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

    public Task<ApiResult<ShoppingItemDto>> AddItemFromBillItemAsync(
        Guid listId, AddShoppingItemFromBillItemRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<ShoppingItemDto>($"{BasePath}/{listId}/items/from-bill-item", request, cancellationToken);
    }
}
