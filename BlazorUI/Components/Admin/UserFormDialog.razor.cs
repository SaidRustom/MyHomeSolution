using BlazorUI.Models.Common;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin;

public partial class UserFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IUserService UserService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public CreateUserFormModel Model { get; set; } = new();

    bool IsBusy { get; set; }

    string? ErrorMessage { get; set; }

    async Task OnSubmit()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = Model.ToRequest();
            var result = await UserService.CreateUserAsync(request);

            if (result.IsSuccess)
            {
                Notifications.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "User Created",
                    Detail = $"Account for '{Model.Email}' has been created.",
                    Duration = 4000
                });
                DialogService.Close(true);
            }
            else
            {
                ErrorMessage = result.Problem.ToUserMessage();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    void Cancel() => DialogService.Close(null);
}
