using BlazorUI.Models.Common;
using BlazorUI.Models.Portfolio;

namespace BlazorUI.Services.Contracts;

public interface IPortfolioAdminService
{
    Task<ApiResult<AdminPortfolioDto>> GetAdminPortfolioAsync(CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<Guid>> UpsertExperienceAsync(UpsertExperienceRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResult<Guid>> UpsertProjectAsync(UpsertProjectRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResult<Guid>> UpsertSkillAsync(UpsertSkillRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default);
}
