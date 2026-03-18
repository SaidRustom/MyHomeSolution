using System.Net.Http.Json;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class BillService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IBillService
{
    private const string BasePath = "api/bills";

    public Task<ApiResult<PaginatedList<BillBriefDto>>> GetBillsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("category", category?.ToString()),
            ("paidByUserId", paidByUserId),
            ("searchTerm", searchTerm),
            ("fromDate", fromDate?.ToString("O")),
            ("toDate", toDate?.ToString("O")),
            ("sortBy", sortBy),
            ("sortDirection", sortDirection),
            ("isFullyPaid", isFullyPaid?.ToString()),
            ("splitWithUserId", splitWithUserId),
            ("hasLinkedTask", hasLinkedTask?.ToString()),
            ("shoppingListId", shoppingListId?.ToString()));

        return GetAsync<PaginatedList<BillBriefDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<BillDetailDto>> GetBillByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<BillDetailDto>($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<UserBalanceDto>>> GetBalancesAsync(
        string? counterpartyUserId = null, CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("counterpartyUserId", counterpartyUserId));

        return GetAsync<IReadOnlyList<UserBalanceDto>>($"{BasePath}/balances{query}", cancellationToken);
    }

    public Task<ApiResult<SpendingSummaryDto>> GetSpendingSummaryAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("fromDate", fromDate?.ToString("O")),
            ("toDate", toDate?.ToString("O")));

        return GetAsync<SpendingSummaryDto>($"{BasePath}/summary{query}", cancellationToken);
    }

    public Task<ApiResult<Guid>> CreateBillAsync(
        CreateBillRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> UpdateBillAsync(
        Guid id, UpdateBillRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{id}", request, cancellationToken);
    }

    public Task<ApiResult> DeleteBillAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult> MarkSplitAsPaidAsync(
        Guid billId, Guid splitId, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{billId}/splits/{splitId}/pay", cancellationToken: cancellationToken);
    }

    public async Task<ApiResult<string>> UploadReceiptAsync(
        Guid billId, Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        using var response = await Http.PostAsync($"{BasePath}/{billId}/receipt", content, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ReceiptUploadResult>(cancellationToken: cancellationToken);
            return ApiResult<string>.Success(result?.ReceiptUrl ?? string.Empty, statusCode);
        }

        return ApiResult<string>.Failure(
            new ApiProblemDetails { Title = "Upload failed", Detail = "Failed to upload receipt." },
            statusCode);
    }

    public async Task<string?> GetReceiptDataUrlAsync(
        Guid billId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Http.GetAsync($"{BasePath}/{billId}/receipt", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiResult<BillDetailDto>> CreateBillFromReceiptAsync(
        Stream fileStream, string fileName, string contentType,
        BillCategory category = BillCategory.General,
        List<SplitRequest>? splits = null,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var queryParts = new List<string> { $"category={category}" };
        if (splits is { Count: > 0 })
        {
            var encoded = string.Join(",", splits.Select(s =>
                s.Percentage.HasValue ? $"{s.UserId}:{s.Percentage.Value}" : s.UserId));
            queryParts.Add($"splitUserIds={encoded}");
        }

        var queryString = string.Join("&", queryParts);

        using var response = await Http.PostAsync($"{BasePath}/from-receipt?{queryString}", content, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<BillDetailDto>(cancellationToken: cancellationToken);
            return ApiResult<BillDetailDto>.Success(result!, statusCode);
        }

        var problem = await TryReadProblemFromResponseAsync(response, cancellationToken);
        return ApiResult<BillDetailDto>.Failure(problem, statusCode);
    }

    private static async Task<ApiProblemDetails> TryReadProblemFromResponseAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(cancellationToken: cancellationToken);
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

    private sealed record ReceiptUploadResult(string ReceiptUrl);
}
