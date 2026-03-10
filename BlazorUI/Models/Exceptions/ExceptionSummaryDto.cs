namespace BlazorUI.Models.Exceptions;

public sealed record ExceptionSummaryDto
{
    public int TotalExceptions { get; init; }
    public int UnhandledExceptions { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int TodayCount { get; init; }
    public IReadOnlyList<ExceptionTypeCountDto> TopExceptionTypes { get; init; } = [];
}

public sealed record ExceptionTypeCountDto
{
    public string ExceptionType { get; init; } = default!;
    public int Count { get; init; }

    public string ShortExceptionType
    {
        get
        {
            var lastDot = ExceptionType.LastIndexOf('.');
            return lastDot >= 0 ? ExceptionType[(lastDot + 1)..] : ExceptionType;
        }
    }
}
