using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class ExceptionLog : BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>The fully-qualified exception type (e.g. System.NullReferenceException).</summary>
    public string ExceptionType { get; set; } = default!;

    /// <summary>The exception message.</summary>
    public string Message { get; set; } = default!;

    /// <summary>Full stack trace captured at throw time.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Inner-exception chain serialised as text.</summary>
    public string? InnerException { get; set; }

    /// <summary>Service / middleware that caught the exception.</summary>
    public string ThrownByService { get; set; } = default!;

    /// <summary>Class name where the exception originated (parsed from stack trace or set explicitly).</summary>
    public string? ClassName { get; set; }

    /// <summary>Method name where the exception originated.</summary>
    public string? MethodName { get; set; }

    /// <summary>HTTP request path that triggered the exception (null for background jobs).</summary>
    public string? RequestPath { get; set; }

    /// <summary>HTTP method (GET, POST, …).</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Authenticated user id when available.</summary>
    public string? UserId { get; set; }

    /// <summary>Distributed trace id for correlation.</summary>
    public string? TraceId { get; set; }

    /// <summary>HTTP status code returned to the client.</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Severity level derived from the exception category.</summary>
    public ExceptionSeverity Severity { get; set; }

    /// <summary>Whether the exception is a known/handled type (validation, not-found, etc.).</summary>
    public bool IsHandled { get; set; }

    /// <summary>Environment name (Development, Staging, Production).</summary>
    public string? Environment { get; set; }

    /// <summary>AI-generated root cause analysis and suggested fix.</summary>
    public string? AiAnalysis { get; set; }

    /// <summary>AI-generated Copilot prompt for copy-paste fixing (null for validation exceptions).</summary>
    public string? AiSuggestedPrompt { get; set; }

    /// <summary>Whether AI analysis has been completed.</summary>
    public bool IsAiAnalysed { get; set; }

    /// <summary>When the AI analysis was completed.</summary>
    public DateTimeOffset? AiAnalysedAt { get; set; }
}
