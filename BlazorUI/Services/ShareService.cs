using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Shares;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class ShareService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IShareService
{
    private const string BasePath = "api/shares";

    public Task<ApiResult<IReadOnlyList<ShareDto>>> GetSharesAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("entityType", entityType),
            ("entityId", entityId.ToString()));

        return GetAsync<IReadOnlyList<ShareDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<Guid>> ShareEntityAsync(
        ShareEntityRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> UpdatePermissionAsync(
        Guid shareId, SharePermission permission, CancellationToken cancellationToken = default)
    {
        var body = new UpdateSharePermissionRequest { Permission = permission };
        return PutAsync($"{BasePath}/{shareId}", body, cancellationToken);
    }

    public Task<ApiResult> RevokeShareAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }
}
