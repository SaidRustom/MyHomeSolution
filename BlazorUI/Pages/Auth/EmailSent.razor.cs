using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class EmailSent
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "email")]
    private string? Email { get; set; }

    private bool IsResending { get; set; }
    private bool ResendSuccess { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Navigation.NavigateTo("/register");
        }
    }

    private async Task HandleResendAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return;

        IsResending = true;
        ResendSuccess = false;

        try
        {
            await AuthService.ResendConfirmationAsync(Email);
            ResendSuccess = true;
        }
        catch
        {
            // Silently handle - don't reveal if email exists
        }
        finally
        {
            IsResending = false;
        }
    }
}
