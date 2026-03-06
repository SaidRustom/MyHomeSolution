using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class LoadingIndicator
{
    [Parameter]
    public string Text { get; set; } = "Loading…";

    [Parameter]
    public bool IsVisible { get; set; } = true;

    [Parameter]
    public bool Overlay { get; set; }
}
