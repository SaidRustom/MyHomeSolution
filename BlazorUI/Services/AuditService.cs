using BlazorUI.Models.AuditLogs;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class AuditService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IAuditService
{
    private const string BasePath = "api/auditlogs";

    public Task<ApiResult<IReadOnlyList<AuditLogDto>>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("maxResults", maxResults.ToString()));

        return GetAsync<IReadOnlyList<AuditLogDto>>(
            $"{BasePath}/{Uri.EscapeDataString(entityName)}s/{Uri.EscapeDataString(entityId)}{query}",
            cancellationToken);
    }
}
