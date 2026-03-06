
using BlazorUI.Components.Common;
using BlazorUI.Models.Users;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace BlazorUI.Pages;

public partial class Settings
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private UserDetailDto? UserProfile { get; set; }
    private UpdateProfileFormModel ProfileModel { get; set; } = new();
    private ChangePasswordFormModel PasswordModel { get; set; } = new();

    private bool IsLoadingProfile { get; set; } = true;
    private bool IsSavingProfile { get; set; }
    private bool IsChangingPassword { get; set; }
    private bool IsUploadingAvatar { get; set; }

    private string? ProfileError { get; set; }
    private string? ProfileUpdateMessage { get; set; }
    private bool ProfileUpdateSuccess { get; set; }
    private string? PasswordChangeMessage { get; set; }
    private bool PasswordChangeSuccess { get; set; }
    private string? AvatarUploadMessage { get; set; }
    private string? AvatarPreviewUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        IsLoadingProfile = true;
        ProfileError = null;

        try
        {
            var result = await UserService.GetCurrentUserAsync();

            if (result.IsSuccess)
            {
                UserProfile = result.Value;
                ProfileModel = new UpdateProfileFormModel
                {
                    FirstName = result.Value.FirstName,
                    LastName = result.Value.LastName,
                    Email = result.Value.Email,
                    AvatarUrl = result.Value.AvatarUrl
                };

                if (!string.IsNullOrWhiteSpace(result.Value.AvatarUrl))
                {
                    AvatarPreviewUrl = await UserService.GetAvatarDataUrlAsync();
                }
            }
            else
            {
                ProfileError = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to load profile.";
            }
        }
        catch
        {
            ProfileError = "Unable to load profile. Please try again.";
        }
        finally
        {
            IsLoadingProfile = false;
        }
    }

    private async Task HandleProfileUpdateAsync()
    {
        IsSavingProfile = true;
        ProfileUpdateMessage = null;

        try
        {
            var request = new UpdateUserProfileRequest
            {
                FirstName = ProfileModel.FirstName,
                LastName = ProfileModel.LastName,
                Email = ProfileModel.Email,
                AvatarUrl = ProfileModel.AvatarUrl
            };

            var result = await UserService.UpdateCurrentUserAsync(request);

            if (result.IsSuccess)
            {
                ProfileUpdateSuccess = true;
                ProfileUpdateMessage = "Profile updated successfully.";
                await LoadProfileAsync();
            }
            else
            {
                ProfileUpdateSuccess = false;
                ProfileUpdateMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to update profile.";
            }
        }
        catch
        {
            ProfileUpdateSuccess = false;
            ProfileUpdateMessage = "Unable to update profile. Please try again.";
        }
        finally
        {
            IsSavingProfile = false;
        }
    }

    private async Task HandleChangePasswordAsync()
    {
        IsChangingPassword = true;
        PasswordChangeMessage = null;

        try
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = PasswordModel.CurrentPassword,
                NewPassword = PasswordModel.NewPassword
            };

            var result = await UserService.ChangePasswordAsync(request);

            if (result.IsSuccess)
            {
                PasswordChangeSuccess = true;
                PasswordChangeMessage = "Password changed successfully.";
                PasswordModel = new ChangePasswordFormModel();
            }
            else
            {
                PasswordChangeSuccess = false;
                PasswordChangeMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to change password. Please check your current password.";
            }
        }
        catch
        {
            PasswordChangeSuccess = false;
            PasswordChangeMessage = "Unable to change password. Please try again.";
        }
        finally
        {
            IsChangingPassword = false;
        }
    }

    private async Task OnAvatarCapturedAsync(ImageCaptureResult capture)
    {
        IsUploadingAvatar = true;
        AvatarUploadMessage = null;
        StateHasChanged();

        try
        {
            using var stream = new MemoryStream(capture.Data);
            var result = await UserService.UploadAvatarAsync(stream, capture.FileName, capture.ContentType);

            if (result.IsSuccess)
            {
                AvatarUploadMessage = "Photo updated successfully!";
                await LoadProfileAsync();
            }
            else
            {
                AvatarUploadMessage = result.Problem.Detail is { Length: > 0 }
                    ? result.Problem.Detail
                    : "Failed to upload photo.";
            }
        }
        catch
        {
            AvatarUploadMessage = "Unable to upload image.";
        }
        finally
        {
            IsUploadingAvatar = false;
            StateHasChanged();
        }
    }

    private string GetInitials()
    {
        var first = ProfileModel.FirstName?.Length > 0 ? ProfileModel.FirstName[0].ToString().ToUpper() : "";
        var last = ProfileModel.LastName?.Length > 0 ? ProfileModel.LastName[0].ToString().ToUpper() : "";
        return $"{first}{last}";
    }
}
