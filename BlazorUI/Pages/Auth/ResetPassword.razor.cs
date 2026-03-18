using BlazorUI.Models.Auth;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class ResetPassword
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public string? UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    private ResetPasswordRequest Model { get; set; } = new();
    private bool IsLoading { get; set; }
    private bool IsComplete { get; set; }
    private bool IsInvalidLink { get; set; }
    private string? ErrorMessage { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Token))
        {
            IsInvalidLink = true;
            return;
        }

        Model.UserId = UserId;
        Model.Token = Token;
    }

    private async Task HandleSubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await AuthService.ResetPasswordAsync(Model);

            if (result.IsSuccess)
            {
                IsComplete = true;
            }
            else
            {
                ErrorMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to reset password. The link may have expired.";
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
