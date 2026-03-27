using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class PortfolioAdminService(HttpClient httpClient)
    : ApiServiceBase(httpClient), IPortfolioAdminService
{
    private const string BasePath = "api/admin/portfolio";

    public Task<ApiResult<AdminPortfolioDto>> GetAdminPortfolioAsync(CancellationToken cancellationToken = default)
        => GetAsync<AdminPortfolioDto>(BasePath, cancellationToken);

    public Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"{BasePath}/profile", request, cancellationToken);

    public Task<ApiResult<Guid>> UpsertExperienceAsync(UpsertExperienceRequest request, CancellationToken cancellationToken = default)
        => PostAsync<Guid>($"{BasePath}/experiences", request, cancellationToken);

    public Task<ApiResult> DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"{BasePath}/experiences/{id}", cancellationToken);

    public Task<ApiResult<Guid>> UpsertProjectAsync(UpsertProjectRequest request, CancellationToken cancellationToken = default)
        => PostAsync<Guid>($"{BasePath}/projects", request, cancellationToken);

    public Task<ApiResult> DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"{BasePath}/projects/{id}", cancellationToken);

    public Task<ApiResult<Guid>> UpsertSkillAsync(UpsertSkillRequest request, CancellationToken cancellationToken = default)
        => PostAsync<Guid>($"{BasePath}/skills", request, cancellationToken);

    public Task<ApiResult> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"{BasePath}/skills/{id}", cancellationToken);
}
