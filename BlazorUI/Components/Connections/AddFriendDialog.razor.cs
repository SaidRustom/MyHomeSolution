using BlazorUI.Models.Common;
using BlazorUI.Models.UserConnections;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;

namespace BlazorUI.Components.Connections;

public partial class AddFriendDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IUserConnectionService ConnectionService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public List<UserDto> AvailableUsers { get; set; } = [];

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    string? _selectedUserId;
    bool _isBusy;
    string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;

        var currentUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? state.User.FindFirst("sub")?.Value;

        AvailableUsers = AvailableUsers.Where(x => x.Id != currentUserId).ToList();

        base.OnInitialized();
    }

    async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(_selectedUserId)) return;

        _isBusy = true;
        _errorMessage = null;

        try
        {
            var result = await ConnectionService.SendConnectionRequestAsync(
                new SendConnectionRequestModel { AddresseeId = _selectedUserId });

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Request Sent",
                    Detail = "Connection request sent successfully.",
                    Duration = 4000
                });
                DialogService.Close(true);
            }
            else
            {
                _errorMessage = result.Problem.ToUserMessage();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            _isBusy = false;
        }
    }

    void Cancel() => DialogService.Close(null);
}
