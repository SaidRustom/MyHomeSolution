using MediatR;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpdateProfile;

public sealed record UpdateProfileCommand : IRequest
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string SubHeadline { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Location { get; init; }
    public string? AvatarUrl { get; init; }
    public string? ResumeUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? TwitterUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public bool IsActive { get; init; }
}
