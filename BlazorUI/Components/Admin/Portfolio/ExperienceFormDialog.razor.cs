using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin.Portfolio;

public partial class ExperienceFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IPortfolioAdminService PortfolioAdminService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    [Parameter]
    public UpsertExperienceRequest Model { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; }

    bool IsBusy { get; set; }
    string? ErrorMessage { get; set; }

    string SubmitText => IsEdit ? "Save Changes" : "Add Experience";

    async Task OnSubmitAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        var result = await PortfolioAdminService.UpsertExperienceAsync(Model);

        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = IsEdit ? "Experience Updated" : "Experience Added",
                Detail = $"'{Model.Role} at {Model.Company}' has been saved.",
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
