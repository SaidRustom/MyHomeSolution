using System.Security.Claims;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorUI.Layout
{
    public partial class MainLayout
    {
        [Inject]
        NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        CookieThemeService CookieThemeService { get; set; } = default!;

        [Inject]
        IAuthService AuthService { get; set; } = default!;

        [Inject]
        IUserService UserService { get; set; } = default!;

        [CascadingParameter]
        private Task<AuthenticationState> AuthState { get; set; } = default!;

        private string? UserDisplayName { get; set; }
        private string? UserEmail { get; set; }
        private string? AvatarDataUrl { get; set; }
        private string UserInitials { get; set; } = string.Empty;

        bool sidebarExpanded = false;

        protected override async Task OnParametersSetAsync()
        {
            var state = await AuthState;
            var user = state.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                UserEmail = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("email")?.Value;

                var name = user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst("name")?.Value;

                UserDisplayName = !string.IsNullOrWhiteSpace(name) ? name : UserEmail;

                UserInitials = GetInitials(UserDisplayName);

                _ = LoadAvatarAsync();
            }
        }

        private async Task LoadAvatarAsync()
        {
            try
            {
                var profileResult = await UserService.GetCurrentUserAsync();
                if (profileResult.IsSuccess && !string.IsNullOrWhiteSpace(profileResult.Value.AvatarUrl))
                {
                    AvatarDataUrl = await UserService.GetAvatarDataUrlAsync();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch
            {
                // Non-critical: avatar display is best-effort
            }
        }

        private static string GetInitials(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "?";

            var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : displayName[..1].ToUpperInvariant();
        }

        void NavigateTo(string url)
        {
            NavigationManager.NavigateTo(url);
            sidebarExpanded = false;
        }

        async Task HandleLogoutAsync()
        {
            await AuthService.LogoutAsync();
            NavigationManager.NavigateTo("/login", forceLoad: true);
        }
    }
}
