using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;

namespace BlazorUI.Services.Contracts;

public interface IBillService
{
    Task<ApiResult<PaginatedList<BillBriefDto>>> GetBillsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        BillCategory? category = null,
        string? paidByUserId = null,
        string? searchTerm = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? sortBy = null,
        string? sortDirection = null,
        bool? isFullyPaid = null,
        string? splitWithUserId = null,
        bool? hasLinkedTask = null,
        Guid? shoppingListId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<BillDetailDto>> GetBillByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyList<UserBalanceDto>>> GetBalancesAsync(
        string? counterpartyUserId = null, CancellationToken cancellationToken = default);

    Task<ApiResult<SpendingSummaryDto>> GetSpendingSummaryAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> CreateBillAsync(
        CreateBillRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateBillAsync(
        Guid id, UpdateBillRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteBillAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult> MarkSplitAsPaidAsync(
        Guid billId, Guid splitId, CancellationToken cancellationToken = default);

    Task<ApiResult<string>> UploadReceiptAsync(
        Guid billId, Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    Task<string?> GetReceiptDataUrlAsync(
        Guid billId, CancellationToken cancellationToken = default);

    Task<ApiResult<BillDetailDto>> CreateBillFromReceiptAsync(
        Stream fileStream, string fileName, string contentType,
        BillCategory category = BillCategory.General,
        List<SplitRequest>? splits = null,
        CancellationToken cancellationToken = default);
}
