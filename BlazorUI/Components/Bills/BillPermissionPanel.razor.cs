using BlazorUI.Components.Common;
using BlazorUI.Components.Connections;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Shares;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Bills;

public partial class BillPermissionPanel
{
    [Inject]
    IShareService ShareService { get; set; } = default!;

    [Inject]
    IUserConnectionService UserConnectionService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid EntityId { get; set; }

    [Parameter, EditorRequired]
    public required string EntityType { get; set; }

    [Parameter]
    public string? OwnerId { get; set; }

    [Parameter]
    public string? OwnerName { get; set; }

    [Parameter]
    public bool CanEdit { get; set; }

    IReadOnlyList<ShareWithName>? _shares;
    bool IsLoading { get; set; }
    Guid? _busyShareId;

    static IEnumerable<SharePermission> PermissionOptions => Enum.GetValues<SharePermission>();

    protected override async Task OnParametersSetAsync()
    {
        await LoadSharesAsync();
    }

    async Task LoadSharesAsync()
    {
        IsLoading = true;

        var result = await ShareService.GetSharesAsync(EntityType, EntityId);
        if (result.IsSuccess)
        {
            _shares = result.Value.Select(s => new ShareWithName
            {
                Id = s.Id,
                SharedWithUserId = s.SharedWithUserId,
                SharedWithUserFullName = null,
                Permission = s.Permission,
                CreatedAt = s.CreatedAt
            }).ToList();

            // Enrich names via connection search
            var nameMap = new Dictionary<string, string>();
            foreach (var share in _shares)
            {
                var search = await UserConnectionService.SearchConnectedUsersAsync(share.SharedWithUserId);
                if (search.IsSuccess && search.Value.Count > 0)
                {
                    var user = search.Value.FirstOrDefault(u => u.Id == share.SharedWithUserId);
                    if (user is not null)
                        nameMap[share.SharedWithUserId] = user.FullName;
                }
            }

            _shares = _shares.Select(s => s with
            {
                SharedWithUserFullName = nameMap.GetValueOrDefault(s.SharedWithUserId)
            }).ToList();
        }

        IsLoading = false;
    }

    async Task OpenShareDialogAsync()
    {
        var result = await DialogService.OpenAsync<ShareDialog>(
            "Share Bill",
            new Dictionary<string, object>
            {
                { nameof(ShareDialog.EntityId), EntityId },
                { nameof(ShareDialog.EntityType), EntityType }
            },
            new DialogOptions
            {
                Width = "500px",
                CloseDialogOnOverlayClick = false,
                ShowClose = true
            });

        if (result is true)
        {
            await LoadSharesAsync();
        }
    }

    async Task UpdatePermissionAsync(ShareWithName share, SharePermission newPermission)
    {
        _busyShareId = share.Id;
        var result = await ShareService.UpdatePermissionAsync(share.Id, newPermission);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Permission Updated",
                Duration = 3000
            });
            await LoadSharesAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = "Failed to update permission.",
                Duration = 5000
            });
        }

        _busyShareId = null;
    }

    async Task RevokeShareAsync(ShareWithName share)
    {
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Revoke Access",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Remove access for {share.SharedWithUserFullName ?? share.SharedWithUserId}?" },
                { nameof(ConfirmDialog.ConfirmText), "Revoke" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "person_remove" }
            },
            new DialogOptions { Width = "400px", CloseDialogOnOverlayClick = false });

        if (confirmed is not true) return;

        _busyShareId = share.Id;
        var result = await ShareService.RevokeShareAsync(share.Id);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Access Revoked",
                Duration = 3000
            });
            await LoadSharesAsync();
        }

        _busyShareId = null;
    }

    sealed record ShareWithName
    {
        public Guid Id { get; init; }
        public required string SharedWithUserId { get; init; }
        public string? SharedWithUserFullName { get; init; }
        public SharePermission Permission { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
