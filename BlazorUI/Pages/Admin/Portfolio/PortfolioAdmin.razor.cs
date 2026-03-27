using BlazorUI.Components.Admin.Portfolio;
using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

#pragma warning disable CS8603

namespace BlazorUI.Pages.Admin.Portfolio;

public partial class PortfolioAdmin
{
    [Inject]
    IPortfolioAdminService PortfolioAdminService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    NotificationService Notifications { get; set; } = default!;

    AdminPortfolioDto? Data { get; set; }
    bool IsLoading { get; set; }
    ApiProblemDetails? Error { get; set; }
    int SelectedTabIndex { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        IsLoading = true;
        Error = null;
        StateHasChanged();

        var result = await PortfolioAdminService.GetAdminPortfolioAsync();

        if (result.IsSuccess)
            Data = result.Value;
        else
            Error = result.Problem;

        IsLoading = false;
    }

    // ── Profile ──────────────────────────────────────────────────────────

    async Task OpenEditProfileDialog()
    {
        if (Data?.Profile is null) return;

        var model = new UpdateProfileRequest
        {
            Id = Data.Profile.Id,
            FullName = Data.Profile.FullName,
            Headline = Data.Profile.Headline,
            SubHeadline = Data.Profile.SubHeadline,
            Bio = Data.Profile.Bio,
            Email = Data.Profile.Email,
            Phone = Data.Profile.Phone,
            Location = Data.Profile.Location,
            AvatarUrl = Data.Profile.AvatarUrl,
            ResumeUrl = Data.Profile.ResumeUrl,
            GitHubUrl = Data.Profile.GitHubUrl,
            LinkedInUrl = Data.Profile.LinkedInUrl,
            TwitterUrl = Data.Profile.TwitterUrl,
            WebsiteUrl = Data.Profile.WebsiteUrl,
            IsActive = Data.Profile.IsActive
        };

        var result = await DialogService.OpenAsync<ProfileFormDialog>("Edit Profile",
            new Dictionary<string, object> { ["Model"] = model },
            new DialogOptions { Width = "700px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    // ── Experiences ──────────────────────────────────────────────────────

    async Task OpenAddExperienceDialog()
    {
        var model = new UpsertExperienceRequest();
        var result = await DialogService.OpenAsync<ExperienceFormDialog>("Add Experience",
            new Dictionary<string, object> { ["Model"] = model },
            new DialogOptions { Width = "700px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task OpenEditExperienceDialog(AdminPortfolioExperienceDto exp)
    {
        var model = new UpsertExperienceRequest
        {
            Id = exp.Id,
            Company = exp.Company,
            Role = exp.Role,
            Description = exp.Description,
            LogoUrl = exp.LogoUrl,
            CompanyUrl = exp.CompanyUrl,
            Technologies = exp.Technologies,
            StartDate = exp.StartDate,
            EndDate = exp.EndDate,
            IsCurrent = exp.IsCurrent,
            SortOrder = exp.SortOrder,
            IsVisible = exp.IsVisible
        };

        var result = await DialogService.OpenAsync<ExperienceFormDialog>("Edit Experience",
            new Dictionary<string, object> { ["Model"] = model, ["IsEdit"] = true },
            new DialogOptions { Width = "700px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task ToggleExperienceVisibility(AdminPortfolioExperienceDto exp)
    {
        var request = new UpsertExperienceRequest
        {
            Id = exp.Id,
            Company = exp.Company,
            Role = exp.Role,
            Description = exp.Description,
            LogoUrl = exp.LogoUrl,
            CompanyUrl = exp.CompanyUrl,
            Technologies = exp.Technologies,
            StartDate = exp.StartDate,
            EndDate = exp.EndDate,
            IsCurrent = exp.IsCurrent,
            SortOrder = exp.SortOrder,
            IsVisible = !exp.IsVisible
        };

        var result = await PortfolioAdminService.UpsertExperienceAsync(request);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = exp.IsVisible ? "Experience Hidden" : "Experience Visible",
                Detail = $"'{exp.Role} at {exp.Company}' visibility updated.",
                Duration = 3000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    async Task ConfirmDeleteExperience(AdminPortfolioExperienceDto exp)
    {
        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to delete '{exp.Role} at {exp.Company}'? This action cannot be undone.",
            "Delete Experience",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirmed != true) return;

        var result = await PortfolioAdminService.DeleteExperienceAsync(exp.Id);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Experience Deleted",
                Detail = $"'{exp.Role} at {exp.Company}' has been removed.",
                Duration = 4000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    // ── Projects ─────────────────────────────────────────────────────────

    async Task OpenAddProjectDialog()
    {
        var model = new UpsertProjectRequest();
        var result = await DialogService.OpenAsync<ProjectFormDialog>("Add Project",
            new Dictionary<string, object> { ["Model"] = model },
            new DialogOptions { Width = "700px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task OpenEditProjectDialog(AdminPortfolioProjectDto proj)
    {
        var model = new UpsertProjectRequest
        {
            Id = proj.Id,
            Title = proj.Title,
            ShortDescription = proj.ShortDescription,
            LongDescription = proj.LongDescription,
            ImageUrl = proj.ImageUrl,
            LiveUrl = proj.LiveUrl,
            GitHubUrl = proj.GitHubUrl,
            Technologies = proj.Technologies,
            Category = proj.Category,
            SortOrder = proj.SortOrder,
            IsFeatured = proj.IsFeatured,
            IsVisible = proj.IsVisible
        };

        var result = await DialogService.OpenAsync<ProjectFormDialog>("Edit Project",
            new Dictionary<string, object> { ["Model"] = model, ["IsEdit"] = true },
            new DialogOptions { Width = "700px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task ToggleProjectVisibility(AdminPortfolioProjectDto proj)
    {
        var request = new UpsertProjectRequest
        {
            Id = proj.Id,
            Title = proj.Title,
            ShortDescription = proj.ShortDescription,
            LongDescription = proj.LongDescription,
            ImageUrl = proj.ImageUrl,
            LiveUrl = proj.LiveUrl,
            GitHubUrl = proj.GitHubUrl,
            Technologies = proj.Technologies,
            Category = proj.Category,
            SortOrder = proj.SortOrder,
            IsFeatured = proj.IsFeatured,
            IsVisible = !proj.IsVisible
        };

        var result = await PortfolioAdminService.UpsertProjectAsync(request);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = proj.IsVisible ? "Project Hidden" : "Project Visible",
                Detail = $"'{proj.Title}' visibility updated.",
                Duration = 3000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    async Task ConfirmDeleteProject(AdminPortfolioProjectDto proj)
    {
        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to delete '{proj.Title}'? This action cannot be undone.",
            "Delete Project",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirmed != true) return;

        var result = await PortfolioAdminService.DeleteProjectAsync(proj.Id);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Project Deleted",
                Detail = $"'{proj.Title}' has been removed.",
                Duration = 4000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    // ── Skills ───────────────────────────────────────────────────────────

    async Task OpenAddSkillDialog()
    {
        var model = new UpsertSkillRequest();
        var result = await DialogService.OpenAsync<SkillFormDialog>("Add Skill",
            new Dictionary<string, object> { ["Model"] = model },
            new DialogOptions { Width = "550px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task OpenEditSkillDialog(AdminPortfolioSkillDto skill)
    {
        var model = new UpsertSkillRequest
        {
            Id = skill.Id,
            Name = skill.Name,
            Category = skill.Category,
            ProficiencyLevel = skill.ProficiencyLevel,
            IconClass = skill.IconClass,
            SortOrder = skill.SortOrder,
            IsVisible = skill.IsVisible
        };

        var result = await DialogService.OpenAsync<SkillFormDialog>("Edit Skill",
            new Dictionary<string, object> { ["Model"] = model, ["IsEdit"] = true },
            new DialogOptions { Width = "550px", CloseDialogOnOverlayClick = false });

        if (result is true)
            await LoadDataAsync();
    }

    async Task ToggleSkillVisibility(AdminPortfolioSkillDto skill)
    {
        var request = new UpsertSkillRequest
        {
            Id = skill.Id,
            Name = skill.Name,
            Category = skill.Category,
            ProficiencyLevel = skill.ProficiencyLevel,
            IconClass = skill.IconClass,
            SortOrder = skill.SortOrder,
            IsVisible = !skill.IsVisible
        };

        var result = await PortfolioAdminService.UpsertSkillAsync(request);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = skill.IsVisible ? "Skill Hidden" : "Skill Visible",
                Detail = $"'{skill.Name}' visibility updated.",
                Duration = 3000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }

    async Task ConfirmDeleteSkill(AdminPortfolioSkillDto skill)
    {
        var confirmed = await DialogService.Confirm(
            $"Are you sure you want to delete '{skill.Name}'? This action cannot be undone.",
            "Delete Skill",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirmed != true) return;

        var result = await PortfolioAdminService.DeleteSkillAsync(skill.Id);
        if (result.IsSuccess)
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Skill Deleted",
                Detail = $"'{skill.Name}' has been removed.",
                Duration = 4000
            });
            await LoadDataAsync();
        }
        else
        {
            Notifications.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = result.Problem.ToUserMessage(),
                Duration = 5000
            });
        }
    }
}
