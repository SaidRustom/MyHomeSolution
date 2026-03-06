using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class Home
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;
}
