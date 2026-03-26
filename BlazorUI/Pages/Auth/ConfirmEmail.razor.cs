using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class ConfirmEmail
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public string? UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsSuccess { get; set; }
    private string? ErrorMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Token))
        {
            IsLoading = false;
            ErrorMessage = "Invalid confirmation link. Please check your email and try again.";
            return;
        }

        try
        {
            var result = await AuthService.ConfirmEmailAsync(UserId, Token);
            IsSuccess = result.IsSuccess;

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "The confirmation link is invalid or has expired.";
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
}
