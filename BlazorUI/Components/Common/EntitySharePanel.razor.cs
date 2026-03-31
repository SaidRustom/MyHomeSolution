using BlazorUI.Components.Connections;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Shares;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorUI.Components.Common;

public partial class EntitySharePanel : IDisposable
{
    [Inject]
    IShareService ShareService { get; set; } = default!;

    [Inject]
    IUserConnectionService UserConnectionService { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    /// <summary>The entity identifier to load shares for.</summary>
    [Parameter, EditorRequired]
    public Guid EntityId { get; set; }

    /// <summary>The entity type string (e.g. "Bill", "ShoppingList", "Budget", "HouseholdTask").</summary>
    [Parameter, EditorRequired]
    public required string EntityType { get; set; }

    /// <summary>User ID of the entity owner.</summary>
    [Parameter]
    public string? OwnerId { get; set; }

    /// <summary>Display name of the entity owner.</summary>
    [Parameter]
    public string? OwnerName { get; set; }

    /// <summary>Avatar URL for the entity owner.</summary>
    [Parameter]
    public string? OwnerAvatarUrl { get; set; }

    /// <summary>Whether the current user can manage shares (add/update/revoke).</summary>
    [Parameter]
    public bool CanManage { get; set; }

    /// <summary>Fires when shares change (added, updated, revoked) so the parent can react.</summary>
    [Parameter]
    public EventCallback OnSharesChanged { get; set; }

    IReadOnlyList<ShareEntry>? _shares;
    bool _isLoading;
    Guid? _busyShareId;
    Guid _lastEntityId;
    CancellationTokenSource _cts = new();

    static IEnumerable<SharePermission> PermissionOptions => Enum.GetValues<SharePermission>();

    protected override async Task OnParametersSetAsync()
    {
        if (_lastEntityId != EntityId)
        {
            _lastEntityId = EntityId;
            await LoadSharesAsync();
        }
    }

    async Task LoadSharesAsync()
    {
        _isLoading = true;

        var currentUserId = (await AuthState).User.FindFirst("sub")?.Value;
        var currentUserName = (await AuthState).User.FindFirst("name")?.Value;

        var result = await ShareService.GetSharesAsync(EntityType, EntityId, _cts.Token);
        if (result.IsSuccess)
        {
            var entries = result.Value.Select(s => new ShareEntry
            {
                Id = s.Id,
                SharedWithUserId = s.SharedWithUserId,
                Permission = s.Permission,
                SharedAt = s.CreatedAt
            }).ToList();

            // Enrich display names and avatars via connected-user search
            foreach (var entry in entries)
            {
                if (entry.SharedWithUserId == currentUserId)
                    entry.DisplayName = currentUserName;
                else
                {
                    var search = await UserConnectionService.SearchConnectedUsersAsync(entry.SharedWithUserId, cancellationToken: _cts.Token);
                    if (search.IsSuccess)
                    {
                        var user = search.Value.FirstOrDefault(u => u.Id == entry.SharedWithUserId);
                        if (user is not null)
                        {
                            entry.DisplayName = user.FullName;
                        }
                    }
                }
            }

            _shares = entries;
        }

        _isLoading = false;
    }

    async Task OpenShareDialogAsync()
    {
        var result = await DialogService.OpenAsync<ShareDialog>(
            $"Share {FormatEntityType()}",
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
            await OnSharesChanged.InvokeAsync();
        }
    }

    async Task UpdatePermissionAsync(ShareEntry share, SharePermission newPermission)
    {
        _busyShareId = share.Id;
        var result = await ShareService.UpdatePermissionAsync(share.Id, newPermission, _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Permission Updated",
                Duration = 3000
            });
            await LoadSharesAsync();
            await OnSharesChanged.InvokeAsync();
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

    async Task RevokeShareAsync(ShareEntry share)
    {
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Revoke Access",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Remove access for {share.DisplayName ?? share.SharedWithUserId}?" },
                { nameof(ConfirmDialog.ConfirmText), "Revoke" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger },
                { nameof(ConfirmDialog.ConfirmIcon), "person_remove" }
            },
            new DialogOptions { Width = "400px", CloseDialogOnOverlayClick = false });

        if (confirmed is not true) return;

        _busyShareId = share.Id;
        var result = await ShareService.RevokeShareAsync(share.Id, _cts.Token);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Access Revoked",
                Duration = 3000
            });
            await LoadSharesAsync();
            await OnSharesChanged.InvokeAsync();
        }

        _busyShareId = null;
    }

    string FormatEntityType() => EntityType switch
    {
        "ShoppingList" => "Shopping List",
        "HouseholdTask" => "Task",
        _ => EntityType
    };

    async Task OpenSharedHistoryAsync(string userId, string? displayName)
    {
        var currentUserId = (await AuthState).User.FindFirst("sub")?.Value;
        if (userId == currentUserId) return;

        await DialogService.OpenAsync<SharedHistoryDialog>(
            $"Shared with {displayName ?? "User"}",
            new Dictionary<string, object>
            {
                { nameof(SharedHistoryDialog.UserId), userId }
            },
            new DialogOptions
            {
                Width = "680px",
                Height = "600px",
                CloseDialogOnOverlayClick = true,
                ShowClose = true
            });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    sealed class ShareEntry
    {
        public Guid Id { get; init; }
        public required string SharedWithUserId { get; init; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public SharePermission Permission { get; init; }
        public DateTimeOffset SharedAt { get; init; }
    }
}
