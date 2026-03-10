using System.Security.Claims;
using BlazorUI.Components.Bills;
using BlazorUI.Models.Bills;
using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Realtime;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorUI.Pages.Bills;

public partial class BillDetail : IDisposable
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    IBillService BillService { get; set; } = default!;

    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    [Inject]
    IShareService ShareService { get; set; } = default!;

    [Inject]
    INotificationHubClient NotificationHubClient { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    BillDetailDto? Bill { get; set; }

    bool IsLoading { get; set; }

    bool IsProcessing { get; set; }

    ApiProblemDetails? Error { get; set; }

    string? _currentUserId;
    bool _isOwner;
    bool _canEdit;

    CancellationTokenSource _cts = new();

    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthState;
        _currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? state.User.FindFirst("sub")?.Value;

        NotificationHubClient.OnUserNotification -= HandleRealtimeNotification;
        NotificationHubClient.OnUserNotification += HandleRealtimeNotification;

        await LoadBillAsync();
    }

    async Task LoadBillAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await BillService.GetBillByIdAsync(Id, _cts.Token);

        if (result.IsSuccess)
        {
            Bill = result.Value;
            _isOwner = !string.IsNullOrEmpty(_currentUserId)
                && string.Equals(Bill.CreatedByUserId, _currentUserId, StringComparison.OrdinalIgnoreCase);

            // Owners always can edit; shared users with Edit permission can edit too
            _canEdit = _isOwner;
            if (!_canEdit && !string.IsNullOrEmpty(_currentUserId))
            {
                var sharesResult = await ShareService.GetSharesAsync("Bill", Bill.Id, _cts.Token);
                if (sharesResult.IsSuccess)
                {
                    _canEdit = sharesResult.Value.Any(s =>
                        s.SharedWithUserId == _currentUserId
                        && s.Permission == SharePermission.Edit);
                }
            }
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task EditBillAsync()
    {
        if (Bill is null) return;

        var model = BillFormModel.FromDetail(Bill);

        var result = await DialogService.OpenAsync<BillFormDialog>(
            "Edit Bill",
            new Dictionary<string, object>
            {
                { nameof(BillFormDialog.Model), model },
                { nameof(BillFormDialog.IsEdit), true }
            },
            new DialogOptions
            {
                Width = "700px",
                Height = "600px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (result is true)
        {
            await LoadBillAsync();
        }
    }

    async Task DeleteBillAsync()
    {
        if (Bill is null) return;

        var confirmed = await DialogService.OpenAsync<BillDeleteConfirm>(
            "Delete Bill",
            new Dictionary<string, object>
            {
                { nameof(BillDeleteConfirm.BillTitle), Bill.Title },
                { nameof(BillDeleteConfirm.Amount), Bill.Amount },
                { nameof(BillDeleteConfirm.Currency), Bill.Currency }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var deleteResult = await BillService.DeleteBillAsync(Bill.Id, _cts.Token);

            if (deleteResult.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Bill Deleted",
                    Detail = $"'{Bill.Title}' has been deleted.",
                    Duration = 4000
                });
                NavigationManager.NavigateTo("/bills");
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = deleteResult.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
    }

    async Task MarkSplitAsPaidAsync(BillSplitDto split)
    {
        if (Bill is null) return;

        IsProcessing = true;
        var result = await BillService.MarkSplitAsPaidAsync(Bill.Id, split.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Split Marked as Paid",
                Detail = "The split has been marked as paid.",
                Duration = 4000
            });
            await LoadBillAsync();
        }
        else
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 6000
            });
        }

        IsProcessing = false;
    }

    void GoBack()
    {
        NavigationManager.NavigateTo("/bills");
    }

    public void Dispose()
    {
        NotificationHubClient.OnUserNotification -= HandleRealtimeNotification;
        _cts.Cancel();
        _cts.Dispose();
    }

    void HandleRealtimeNotification(UserPushNotification push)
    {
        if (string.Equals(push.RelatedEntityType, "Bill", StringComparison.OrdinalIgnoreCase)
            && (push.RelatedEntityId == Id || push.RelatedEntityId is null))
        {
            InvokeAsync(async () =>
            {
                await LoadBillAsync();
                StateHasChanged();
            });
        }
    }
}
