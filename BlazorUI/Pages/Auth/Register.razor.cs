using BlazorUI.Models.Auth;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class Register
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private RegisterRequest Model { get; set; } = new();
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task HandleRegisterAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await AuthService.RegisterAsync(Model);

            if (result.IsSuccess)
            {
                var encodedEmail = Uri.EscapeDataString(Model.Email);
                Navigation.NavigateTo($"/email-sent?email={encodedEmail}");
            }
            else
            {
                ErrorMessage = BuildErrorMessage(result.Problem);
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Unable to reach the server. Please try again later.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildErrorMessage(Models.Common.ApiProblemDetails problem)
    {
        if (problem.Errors is { Count: > 0 })
        {
            return string.Join(" ", problem.Errors.SelectMany(e => e.Value));
        }

        return problem.Detail is { Length: > 0 }
            ? problem.Detail
            : problem.Title;
    }
}
