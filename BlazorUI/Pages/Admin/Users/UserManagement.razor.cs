using BlazorUI.Components.Admin;
using BlazorUI.Components.Common;
using BlazorUI.Models.Common;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;


namespace BlazorUI.Pages.Admin.Users;

public partial class UserManagement : IDisposable
{
    [Inject]
    IUserService UserService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    PaginatedList<UserDto> UserData { get; set; } = new();

    UserDetailDto? SelectedUser { get; set; }

    bool IsLoading { get; set; }
    bool IsProcessing { get; set; }

    ApiProblemDetails? Error { get; set; }

    string? SearchTerm { get; set; }
    bool? ActiveFilter { get; set; }

    int _currentPage = 1;
    const int PageSize = 20;

    readonly CancellationTokenSource _cts = new();

    static readonly object[] StatusOptions =
    [
        new { Text = "Active", Value = (bool?)true },
        new { Text = "Inactive", Value = (bool?)false }
    ];

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
    }

    async Task LoadUsersAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await UserService.GetUsersAsync(
            searchTerm: SearchTerm,
            isActive: ActiveFilter,
            pageNumber: _currentPage,
            pageSize: PageSize,
            cancellationToken: _cts.Token);

        if (result.IsSuccess)
        {
            UserData = result.Value;
        }
        else
        {
            Error = result.Problem;
        }

        IsLoading = false;
    }

    async Task OnLoadDataAsync(LoadDataArgs args)
    {
        _currentPage = (args.Skip ?? 0) / PageSize + 1;
        await LoadUsersAsync();
    }

    async Task OnSearchAsync()
    {
        _currentPage = 1;
        await LoadUsersAsync();
    }

    async Task OnClearFiltersAsync()
    {
        _currentPage = 1;
        SearchTerm = null;
        ActiveFilter = null;
        await LoadUsersAsync();
    }

    async Task ViewUser(UserDto user)
    {
        var result = await UserService.GetUserByIdAsync(user.Id, _cts.Token);

        if (result.IsSuccess)
        {
            SelectedUser = result.Value;
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
    }

    void BackToList()
    {
        SelectedUser = null;
    }

    async Task CreateUserAsync()
    {
        var model = new CreateUserFormModel();

        var result = await DialogService.OpenAsync<UserFormDialog>(
            "Create User",
            new Dictionary<string, object>
            {
                { nameof(UserFormDialog.Model), model }
            },
            new DialogOptions
            {
                Width = "520px",
                CloseDialogOnOverlayClick = false,
                ShowClose = false
            });

        if (result is true)
        {
            await LoadUsersAsync();
        }
    }

    async Task ToggleActiveAsync(UserDto user)
    {
        var action = user.IsActive ? "deactivate" : "activate";
        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            $"{(user.IsActive ? "Deactivate" : "Activate")} User",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Are you sure you want to {action} {user.FullName}?" },
                { nameof(ConfirmDialog.ConfirmText), user.IsActive ? "Deactivate" : "Activate" },
                { nameof(ConfirmDialog.ConfirmIcon), user.IsActive ? "block" : "check_circle" },
                { nameof(ConfirmDialog.ConfirmStyle), user.IsActive ? ButtonStyle.Warning : ButtonStyle.Success }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            var result = user.IsActive
                ? await UserService.DeactivateUserAsync(user.Id, _cts.Token)
                : await UserService.ActivateUserAsync(user.Id, _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = user.IsActive ? "User Deactivated" : "User Activated",
                    Detail = $"{user.FullName} has been {action}d.",
                    Duration = 4000
                });
                await LoadUsersAsync();
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
        }
    }

    async Task ActivateSelectedUserAsync()
    {
        if (SelectedUser is null) return;

        IsProcessing = true;
        var result = await UserService.ActivateUserAsync(SelectedUser.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "User Activated",
                Detail = $"{SelectedUser.FullName} has been activated.",
                Duration = 4000
            });
            await RefreshSelectedUserAsync();
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

    async Task DeactivateSelectedUserAsync()
    {
        if (SelectedUser is null) return;

        IsProcessing = true;
        var result = await UserService.DeactivateUserAsync(SelectedUser.Id, _cts.Token);

        if (result.IsSuccess)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "User Deactivated",
                Detail = $"{SelectedUser.FullName} has been deactivated.",
                Duration = 4000
            });
            await RefreshSelectedUserAsync();
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

    async Task AssignRoleToSelectedUserAsync()
    {
        if (SelectedUser is null) return;

        var role = await DialogService.OpenAsync<AssignRoleDialog>(
            "Assign Role",
            new Dictionary<string, object>
            {
                { nameof(AssignRoleDialog.UserName), SelectedUser.FullName },
                { nameof(AssignRoleDialog.ExistingRoles), SelectedUser.Roles }
            },
            new DialogOptions
            {
                Width = "400px",
                CloseDialogOnOverlayClick = false
            });

        if (role is string roleName && !string.IsNullOrWhiteSpace(roleName))
        {
            IsProcessing = true;
            var result = await UserService.AssignRoleAsync(SelectedUser.Id, roleName, _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Role Assigned",
                    Detail = $"'{roleName}' role assigned to {SelectedUser.FullName}.",
                    Duration = 4000
                });
                await RefreshSelectedUserAsync();
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
    }

    async Task RemoveRoleFromSelectedUserAsync(string roleName)
    {
        if (SelectedUser is null) return;

        var confirmed = await DialogService.OpenAsync<ConfirmDialog>(
            "Remove Role",
            new Dictionary<string, object>
            {
                { nameof(ConfirmDialog.Message), $"Remove the '{roleName}' role from {SelectedUser.FullName}?" },
                { nameof(ConfirmDialog.ConfirmText), "Remove" },
                { nameof(ConfirmDialog.ConfirmIcon), "remove_circle" },
                { nameof(ConfirmDialog.ConfirmStyle), ButtonStyle.Danger }
            },
            new DialogOptions
            {
                Width = "450px",
                CloseDialogOnOverlayClick = false
            });

        if (confirmed is true)
        {
            IsProcessing = true;
            var result = await UserService.RemoveRoleAsync(SelectedUser.Id, roleName, _cts.Token);

            if (result.IsSuccess)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Role Removed",
                    Detail = $"'{roleName}' role removed from {SelectedUser.FullName}.",
                    Duration = 4000
                });
                await RefreshSelectedUserAsync();
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
    }

    async Task RefreshSelectedUserAsync()
    {
        if (SelectedUser is null) return;

        var result = await UserService.GetUserByIdAsync(SelectedUser.Id, _cts.Token);

        if (result.IsSuccess)
        {
            SelectedUser = result.Value;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
