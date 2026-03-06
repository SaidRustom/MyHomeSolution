using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Shares;

namespace BlazorUI.Services.Contracts;

public interface IShareService
{
    Task<ApiResult<IReadOnlyList<ShareDto>>> GetSharesAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> ShareEntityAsync(
        ShareEntityRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdatePermissionAsync(
        Guid shareId, SharePermission permission, CancellationToken cancellationToken = default);

    Task<ApiResult> RevokeShareAsync(
        Guid id, CancellationToken cancellationToken = default);
}
