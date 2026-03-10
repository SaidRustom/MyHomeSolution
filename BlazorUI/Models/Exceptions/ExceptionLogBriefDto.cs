namespace BlazorUI.Models.Exceptions;

public sealed record ExceptionLogBriefDto
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
    public int Severity { get; init; }
    public bool IsHandled { get; init; }
    public bool IsAiAnalysed { get; init; }

    public string SeverityLabel => Severity switch
    {
        0 => "Low",
        1 => "Medium",
        2 => "High",
        3 => "Critical",
        _ => "Unknown"
    };

    public string SeverityColor => Severity switch
    {
        0 => "var(--rz-info)",
        1 => "var(--rz-warning)",
        2 => "var(--rz-danger)",
        3 => "#d32f2f",
        _ => "var(--rz-secondary)"
    };

    public string ShortExceptionType
    {
        get
        {
            var lastDot = ExceptionType.LastIndexOf('.');
            return lastDot >= 0 ? ExceptionType[(lastDot + 1)..] : ExceptionType;
        }
    }
}
