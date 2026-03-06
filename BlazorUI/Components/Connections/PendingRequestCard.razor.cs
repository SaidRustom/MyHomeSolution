using BlazorUI.Models.UserConnections;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Connections;

public partial class PendingRequestCard
{
    [Parameter, EditorRequired]
    public UserConnectionDto Request { get; set; } = default!;

    [Parameter]
    public bool IsSent { get; set; }

    [Parameter]
    public EventCallback<UserConnectionDto> OnAccept { get; set; }

    [Parameter]
    public EventCallback<UserConnectionDto> OnDecline { get; set; }

    [Parameter]
    public EventCallback<UserConnectionDto> OnCancel { get; set; }
}
