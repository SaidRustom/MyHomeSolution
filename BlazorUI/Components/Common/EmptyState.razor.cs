using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class EmptyState
{
    [Parameter]
    public string Icon { get; set; } = "inbox";

    [Parameter]
    public string Title { get; set; } = "No data found";

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public string? ActionText { get; set; }

    [Parameter]
    public EventCallback OnAction { get; set; }

    bool HasAction => OnAction.HasDelegate && !string.IsNullOrEmpty(ActionText);

    async Task InvokeActionAsync() => await OnAction.InvokeAsync();
}
