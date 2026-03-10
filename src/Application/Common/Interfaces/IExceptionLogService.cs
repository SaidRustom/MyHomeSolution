using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IExceptionLogService
{
    Task LogAsync(
        Exception exception,
        string thrownByService,
        string? requestPath = null,
        string? httpMethod = null,
        string? userId = null,
        string? traceId = null,
        int? httpStatusCode = null,
        bool isHandled = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an exception log entry and returns its Id so callers
    /// (e.g. background services) can link it.
    /// </summary>
    Task<Guid?> LogAndReturnIdAsync(
        Exception exception,
        string thrownByService,
        string? requestPath = null,
        string? httpMethod = null,
        string? userId = null,
        string? traceId = null,
        int? httpStatusCode = null,
        bool isHandled = false,
        CancellationToken cancellationToken = default);
}
