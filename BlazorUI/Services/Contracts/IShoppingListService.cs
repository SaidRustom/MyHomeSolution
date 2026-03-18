using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.ShoppingLists;

namespace BlazorUI.Services.Contracts;

public interface IShoppingListService
{
    Task<ApiResult<PaginatedList<ShoppingListBriefDto>>> GetShoppingListsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        ShoppingListCategory? category = null,
        bool? isCompleted = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ShoppingListDetailDto>> GetShoppingListByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> CreateShoppingListAsync(
        CreateShoppingListRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateShoppingListAsync(
        Guid id, UpdateShoppingListRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteShoppingListAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<ShoppingItemDto>> AddItemAsync(
        Guid listId, AddShoppingItemRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateItemAsync(
        Guid listId, Guid itemId, UpdateShoppingItemRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> RemoveItemAsync(
        Guid listId, Guid itemId, CancellationToken cancellationToken = default);

    Task<ApiResult> ToggleItemAsync(
        Guid listId, Guid itemId, CancellationToken cancellationToken = default);

    Task<ApiResult> ToggleAllItemsAsync(
        Guid listId, bool check, CancellationToken cancellationToken = default);

    Task<ApiResult<ShoppingItemDto>> AddItemFromBillItemAsync(
        Guid listId, AddShoppingItemFromBillItemRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<ProcessReceiptResultDto>> ProcessReceiptAsync(
        Guid listId, Stream fileStream, string fileName, string contentType,
        List<SplitRequest>? splits = null, CancellationToken cancellationToken = default);

    Task<ApiResult> ResolveCrossListMatchAsync(
        Guid targetListId, ResolveCrossListMatchRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult<ShoppingItemGroupResultDto>> GroupItemsAsync(
        Guid listId, CancellationToken cancellationToken = default);
}
