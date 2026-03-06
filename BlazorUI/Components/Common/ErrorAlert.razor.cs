using BlazorUI.Models.Common;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class ErrorAlert
{
    [Parameter]
    public ApiProblemDetails? Problem { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Message { get; set; }

    [Parameter]
    public bool IsVisible { get; set; } = true;

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    [Parameter]
    public EventCallback OnRetry { get; set; }

    bool HasRetry => OnRetry.HasDelegate;

    string DisplayTitle => Title ?? Problem?.Title ?? "Error";

    string DisplayMessage => Message ?? Problem?.Detail ?? "An unexpected error occurred.";

    IReadOnlyDictionary<string, string[]>? ValidationErrors =>
        Problem?.Errors is { } errors ? new Dictionary<string, string[]>(errors) : null;

    bool HasValidationErrors => ValidationErrors is { Count: > 0 };

    async Task DismissAsync() => await OnDismiss.InvokeAsync();

    async Task RetryAsync() => await OnRetry.InvokeAsync();
}
