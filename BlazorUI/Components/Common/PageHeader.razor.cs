using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class PageHeader
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public RenderFragment? Actions { get; set; }

    [Parameter]
    public RenderFragment? Breadcrumb { get; set; }
}
