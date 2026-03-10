using BlazorUI.Models.Enums;
using BlazorUI.Models.Shares;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Connections;

public partial class ShareDialog
{
    [Inject]
    IShareService ShareService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid EntityId { get; set; }

    [Parameter, EditorRequired]
    public required string EntityType { get; set; }

    string? _selectedUserId;
    SharePermission _permission = SharePermission.View;
    bool _isBusy;

    static IEnumerable<SharePermission> PermissionOptions => Enum.GetValues<SharePermission>();

    async Task ShareAsync()
    {
        if (string.IsNullOrEmpty(_selectedUserId)) return;

        _isBusy = true;

        var request = new ShareEntityRequest
        {
            EntityType = EntityType,
            EntityId = EntityId,
            SharedWithUserId = _selectedUserId,
            Permission = _permission
        };

        var result = await ShareService.ShareEntityAsync(request);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Shared Successfully",
                Duration = 3000
            });
            DialogService.Close(true);
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = "Failed to share. Please try again.",
                Duration = 5000
            });
        }

        _isBusy = false;
    }

    void CancelAsync()
    {
        DialogService.Close(false);
    }
}
