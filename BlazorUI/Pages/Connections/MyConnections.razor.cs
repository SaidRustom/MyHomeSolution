using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.UserConnections;
using BlazorUI.Components.Connections;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Pages.Connections;

public partial class MyConnections : IDisposable
{
    [Inject]
    IUserConnectionService ConnectionService { get; set; } = default!;

    [Inject]
    IUserService UserService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    PaginatedList<UserConnectionDto> _connections = new();
    IReadOnlyList<UserConnectionDto> _receivedRequests = [];
    IReadOnlyList<UserConnectionDto> _sentRequests = [];

    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }
    string? SearchTerm { get; set; }

    int _currentPage = 1;
    const int PageSize = 20;

    readonly CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;
        Error = null;

        var connectionsTask = ConnectionService.GetConnectionsAsync(
            pageNumber: _currentPage,
            pageSize: PageSize,
            status: ConnectionStatus.Accepted,
            searchTerm: SearchTerm,
            cancellationToken: _cts.Token);

        var receivedTask = ConnectionService.GetPendingRequestsAsync(
            sent: false, cancellationToken: _cts.Token);

        var sentTask = ConnectionService.GetPendingRequestsAsync(
            sent: true, cancellationToken: _cts.Token);

        await Task.WhenAll(connectionsTask, receivedTask, sentTask);

        var connectionsResult = await connectionsTask;
        var receivedResult = await receivedTask;
        var sentResult = await sentTask;

        if (connectionsResult.IsSuccess)
            _connections = connectionsResult.Value;
        else
            Error = connectionsResult.Problem;

        if (receivedResult.IsSuccess)
            _receivedRequests = receivedResult.Value;

        if (sentResult.IsSuccess)
            _sentRequests = sentResult.Value;

        IsLoading = false;
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await LoadDataAsync();
    }

    async Task OnClearAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        await LoadDataAsync();
    }

    async Task LoadMoreAsync()
    {
        _currentPage++;
        await LoadDataAsync();
    }

    async Task ShowAddFriendAsync()
    {
        var users = await UserService.GetUsersAsync(pageSize: 100, cancellationToken: _cts.Token);
        if (!users.IsSuccess) return;

        var selectedUserId = await DialogService.OpenAsync<AddFriendDialog>(
            "Add Friend",
            new Dictionary<string, object>
            {
                { nameof(AddFriendDialog.AvailableUsers), users.Value.Items.ToList() }
            },
            new DialogOptions
            {
                Width = "480px",
                CloseDialogOnOverlayClick = false
            });

        if (selectedUserId is string userId && !string.IsNullOrWhiteSpace(userId))
        {
            var result = await ConnectionService.SendConnectionRequestAsync(
                new SendConnectionRequestModel { AddresseeId = userId },
                _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Request Sent",
                    Detail = "Connection request sent successfully.",
                    Duration = 4000
                });

                await LoadDataAsync();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = result.Problem.Detail ?? "Failed to send connection request.",
                    Duration = 6000
                });
            }
        }
    }

    async Task AcceptRequestAsync(UserConnectionDto request)
    {
        var result = await ConnectionService.AcceptRequestAsync(request.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Accepted",
                Detail = $"You are now connected with {request.ConnectedUserName}.",
                Duration = 4000
            });

            await LoadDataAsync();
        }
        else
        {
            ShowError(result.Problem);
        }
    }

    async Task DeclineRequestAsync(UserConnectionDto request)
    {
        var result = await ConnectionService.DeclineRequestAsync(request.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Declined",
                Detail = "Connection request declined.",
                Duration = 4000
            });

            await LoadDataAsync();
        }
        else
        {
            ShowError(result.Problem);
        }
    }

    async Task CancelRequestAsync(UserConnectionDto request)
    {
        var result = await ConnectionService.CancelRequestAsync(request.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Cancelled",
                Detail = "Connection request cancelled.",
                Duration = 4000
            });

            await LoadDataAsync();
        }
        else
        {
            ShowError(result.Problem);
        }
    }

    async Task RemoveConnectionAsync(UserConnectionDto connection)
    {
        var confirmed = await DialogService.Confirm(
            $"Remove {connection.ConnectedUserName} from your connections?",
            "Remove Connection",
            new ConfirmOptions
            {
                OkButtonText = "Remove",
                CancelButtonText = "Cancel"
            });

        if (confirmed != true) return;

        var result = await ConnectionService.RemoveConnectionAsync(connection.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Removed",
                Detail = "Connection removed.",
                Duration = 4000
            });

            await LoadDataAsync();
        }
        else
        {
            ShowError(result.Problem);
        }
    }

    void ShowError(ApiProblemDetails? problem)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = "Error",
            Detail = problem?.Detail ?? "An unexpected error occurred.",
            Duration = 6000
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
