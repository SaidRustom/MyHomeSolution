using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class StatCard
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? TrendText { get; set; }

    [Parameter]
    public TrendDirection Trend { get; set; } = TrendDirection.Neutral;

    [Parameter]
    public string IconColor { get; set; } = "var(--rz-primary)";

    string TrendIcon => Trend switch
    {
        TrendDirection.Up => "trending_up",
        TrendDirection.Down => "trending_down",
        _ => "trending_flat"
    };

    string TrendCss => Trend switch
    {
        TrendDirection.Up => "rz-color-success",
        TrendDirection.Down => "rz-color-danger",
        _ => "rz-color-secondary"
    };
}

public enum TrendDirection
{
    Up,
    Down,
    Neutral
}
