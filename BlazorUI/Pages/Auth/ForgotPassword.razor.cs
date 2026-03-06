using BlazorUI.Models.Auth;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class ForgotPassword
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ForgotPasswordRequest Model { get; set; } = new();
    private bool IsLoading { get; set; }
    private bool EmailSent { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task HandleSubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            await AuthService.ForgotPasswordAsync(Model.Email);
            EmailSent = true;
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
}
