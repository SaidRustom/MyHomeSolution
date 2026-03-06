using BlazorUI.Models.Users;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Admin;

public partial class UserDetailPanel
{
    [Parameter, EditorRequired]
    public UserDetailDto User { get; set; } = default!;

    [Parameter]
    public bool IsProcessing { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnActivate { get; set; }

    [Parameter]
    public EventCallback OnDeactivate { get; set; }

    [Parameter]
    public EventCallback OnAssignRole { get; set; }

    [Parameter]
    public EventCallback<string> OnRemoveRole { get; set; }

    async Task GoBackAsync() => await OnBack.InvokeAsync();

    async Task ActivateAsync() => await OnActivate.InvokeAsync();

    async Task DeactivateAsync() => await OnDeactivate.InvokeAsync();

    async Task AssignRoleAsync() => await OnAssignRole.InvokeAsync();
}
