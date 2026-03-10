using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public sealed class ExceptionsController(
    IApplicationDbContext dbContext,
    IExceptionAnalysisService analysisService,
    IDateTimeProvider dateTimeProvider)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ExceptionBriefDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ExceptionSeverity? severity = null,
        [FromQuery] bool? isHandled = null,
        [FromQuery] string? exceptionType = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExceptionLogs.AsNoTracking().AsQueryable();

        if (severity.HasValue)
            query = query.Where(e => e.Severity == severity.Value);

        if (isHandled.HasValue)
            query = query.Where(e => e.IsHandled == isHandled.Value);

        if (!string.IsNullOrWhiteSpace(exceptionType))
            query = query.Where(e => e.ExceptionType.Contains(exceptionType));

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e =>
                e.Message.Contains(searchTerm) ||
                (e.ClassName != null && e.ClassName.Contains(searchTerm)) ||
                (e.RequestPath != null && e.RequestPath.Contains(searchTerm)));

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        query = query.OrderByDescending(e => e.OccurredAt);

        var result = await PaginatedList<ExceptionBriefDto>.CreateAsync(
            query.Select(e => new ExceptionBriefDto
            {
                Id = e.Id,
                OccurredAt = e.OccurredAt,
                ExceptionType = e.ExceptionType,
                Message = e.Message,
                ThrownByService = e.ThrownByService,
                ClassName = e.ClassName,
                RequestPath = e.RequestPath,
                HttpMethod = e.HttpMethod,
                HttpStatusCode = e.HttpStatusCode,
                Severity = e.Severity,
                IsHandled = e.IsHandled,
                IsAiAnalysed = e.IsAiAnalysed
            }),
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExceptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetException(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExceptionLogs
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ExceptionDetailDto
            {
                Id = e.Id,
                OccurredAt = e.OccurredAt,
                ExceptionType = e.ExceptionType,
                Message = e.Message,
                StackTrace = e.StackTrace,
                InnerException = e.InnerException,
                ThrownByService = e.ThrownByService,
                ClassName = e.ClassName,
                MethodName = e.MethodName,
                RequestPath = e.RequestPath,
                HttpMethod = e.HttpMethod,
                UserId = e.UserId,
                TraceId = e.TraceId,
                HttpStatusCode = e.HttpStatusCode,
                Severity = e.Severity,
                IsHandled = e.IsHandled,
                Environment = e.Environment,
                AiAnalysis = e.AiAnalysis,
                AiSuggestedPrompt = e.AiSuggestedPrompt,
                IsAiAnalysed = e.IsAiAnalysed,
                AiAnalysedAt = e.AiAnalysedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return NotFound();

        return Ok(entity);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExceptionSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExceptionLogs.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var unhandled = await query.CountAsync(e => !e.IsHandled, cancellationToken);
        var critical = await query.CountAsync(e => e.Severity == ExceptionSeverity.Critical, cancellationToken);
        var high = await query.CountAsync(e => e.Severity == ExceptionSeverity.High, cancellationToken);
        var todayCount = await query.CountAsync(
            e => e.OccurredAt >= DateTimeOffset.UtcNow.Date, cancellationToken);

        var topExceptionTypes = await query
            .GroupBy(e => e.ExceptionType)
            .Select(g => new ExceptionTypeCount { ExceptionType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Ok(new ExceptionSummaryDto
        {
            TotalExceptions = total,
            UnhandledExceptions = unhandled,
            CriticalCount = critical,
            HighCount = high,
            TodayCount = todayCount,
            TopExceptionTypes = topExceptionTypes
        });
    }

    [HttpPost("{id:guid}/analyse")]
    [ProducesResponseType(typeof(ExceptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyseException(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExceptionLogs
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return NotFound();

        var isValidation = entity.ExceptionType.Contains("ValidationException",
            StringComparison.OrdinalIgnoreCase);

        var result = await analysisService.AnalyseAsync(
            entity.ExceptionType,
            entity.Message,
            entity.StackTrace,
            entity.InnerException,
            isValidation,
            cancellationToken);

        entity.AiAnalysis = result.Analysis;
        entity.AiSuggestedPrompt = result.SuggestedPrompt;
        entity.IsAiAnalysed = true;
        entity.AiAnalysedAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ExceptionDetailDto
        {
            Id = entity.Id,
            OccurredAt = entity.OccurredAt,
            ExceptionType = entity.ExceptionType,
            Message = entity.Message,
            StackTrace = entity.StackTrace,
            InnerException = entity.InnerException,
            ThrownByService = entity.ThrownByService,
            ClassName = entity.ClassName,
            MethodName = entity.MethodName,
            RequestPath = entity.RequestPath,
            HttpMethod = entity.HttpMethod,
            UserId = entity.UserId,
            TraceId = entity.TraceId,
            HttpStatusCode = entity.HttpStatusCode,
            Severity = entity.Severity,
            IsHandled = entity.IsHandled,
            Environment = entity.Environment,
            AiAnalysis = entity.AiAnalysis,
            AiSuggestedPrompt = entity.AiSuggestedPrompt,
            IsAiAnalysed = entity.IsAiAnalysed,
            AiAnalysedAt = entity.AiAnalysedAt
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteException(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExceptionLogs.FindAsync([id], cancellationToken);
        if (entity is null)
            return NotFound();

        dbContext.ExceptionLogs.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public sealed class ExceptionBriefDto
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string ExceptionType { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string ThrownByService { get; init; } = default!;
    public string? ClassName { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public int? HttpStatusCode { get; init; }
    public ExceptionSeverity Severity { get; init; }
    public bool IsHandled { get; init; }
    public bool IsAiAnalysed { get; init; }
}

public sealed class ExceptionDetailDto
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string ExceptionType { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public string? InnerException { get; init; }
    public string ThrownByService { get; init; } = default!;
    public string? ClassName { get; init; }
    public string? MethodName { get; init; }
    public string? RequestPath { get; init; }
    public string? HttpMethod { get; init; }
    public string? UserId { get; init; }
    public string? TraceId { get; init; }
    public int? HttpStatusCode { get; init; }
    public ExceptionSeverity Severity { get; init; }
    public bool IsHandled { get; init; }
    public string? Environment { get; init; }
    public string? AiAnalysis { get; init; }
    public string? AiSuggestedPrompt { get; init; }
    public bool IsAiAnalysed { get; init; }
    public DateTimeOffset? AiAnalysedAt { get; init; }
}

public sealed class ExceptionSummaryDto
{
    public int TotalExceptions { get; init; }
    public int UnhandledExceptions { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int TodayCount { get; init; }
    public IReadOnlyList<ExceptionTypeCount> TopExceptionTypes { get; init; } = [];
}

public sealed class ExceptionTypeCount
{
    public string ExceptionType { get; init; } = default!;
    public int Count { get; init; }
}
