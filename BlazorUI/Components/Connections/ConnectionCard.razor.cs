using BlazorUI.Models.UserConnections;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Connections;

public partial class ConnectionCard
{
    [Parameter, EditorRequired]
    public UserConnectionDto Connection { get; set; } = default!;

    [Parameter]
    public EventCallback<UserConnectionDto> OnRemove { get; set; }
}
