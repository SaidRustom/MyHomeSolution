using BlazorUI.Models.UserConnections;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Connections;

public partial class ConnectionCard
{
    [Inject] DialogService DialogService { get; set; } = default!;

    [Parameter, EditorRequired]
    public UserConnectionDto Connection { get; set; } = default!;

    [Parameter]
    public EventCallback<UserConnectionDto> OnRemove { get; set; }

    async Task OpenSharedHistoryAsync()
    {
        if (string.IsNullOrEmpty(Connection.ConnectedUserId)) return;

        await DialogService.OpenAsync<SharedHistoryDialog>(
            $"Shared with {Connection.ConnectedUserName ?? "User"}",
            new Dictionary<string, object>
            {
                { nameof(SharedHistoryDialog.UserId), Connection.ConnectedUserId }
            },
            new DialogOptions
            {
                Width = "680px",
                Height = "600px",
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });
    }
}
