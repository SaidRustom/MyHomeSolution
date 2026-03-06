using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class StatusBadge
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public StatusSeverity Severity { get; set; } = StatusSeverity.Info;

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public bool IsPill { get; set; } = true;

    string BadgeStyle => Severity switch
    {
        StatusSeverity.Success => "success",
        StatusSeverity.Warning => "warning",
        StatusSeverity.Danger => "danger",
        StatusSeverity.Info => "info",
        StatusSeverity.Secondary => "secondary",
        _ => "light"
    };
}

public enum StatusSeverity
{
    Info,
    Success,
    Warning,
    Danger,
    Secondary
}
