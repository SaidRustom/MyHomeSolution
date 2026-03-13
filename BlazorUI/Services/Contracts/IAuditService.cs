using BlazorUI.Models.AuditLogs;
using BlazorUI.Models.Common;

namespace BlazorUI.Services.Contracts;

public interface IAuditService
{
    Task<ApiResult<IReadOnlyList<AuditLogDto>>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        int maxResults = 50,
        CancellationToken cancellationToken = default);
}
