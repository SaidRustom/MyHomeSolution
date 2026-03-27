using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin.Portfolio;

public partial class ProfileFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IPortfolioAdminService PortfolioAdminService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public UpdateProfileRequest Model { get; set; } = new();

    bool IsBusy { get; set; }
    string? ErrorMessage { get; set; }

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        var result = await PortfolioAdminService.UpdateProfileAsync(Model);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Profile Updated",
                Detail = "Portfolio profile has been saved.",
                Duration = 4000
            });
            DialogService.Close(true);
        }
        else
        {
            ErrorMessage = result.Problem.ToUserMessage();
        }

        IsBusy = false;
    }

    void Cancel() => DialogService.Close(false);
}
