using BlazorUI.Models.Auth;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages.Auth;

public partial class Login
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string? ReturnUrl { get; set; }

    private LoginRequest Model { get; set; } = new();
    private bool IsLoading { get; set; }
    private string? ErrorMessage { get; set; }

    private async Task HandleLoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await AuthService.LoginAsync(Model);

            if (result.IsSuccess)
            {
                var target = string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl;
                Navigation.NavigateTo(target, forceLoad: false);
            }
            else
            {
                ErrorMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : result.Problem.Title;
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
