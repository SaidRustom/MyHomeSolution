namespace BlazorUI.Models.Common;

public sealed record ApiProblemDetails
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; init; }
}
