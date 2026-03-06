using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IShareService
{
    Task<bool> HasAccessAsync(
        string entityType, Guid entityId, string userId, SharePermission requiredPermission,
        CancellationToken cancellationToken = default);
}
