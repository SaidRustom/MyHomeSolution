using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Application.Features.Portfolio.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.PortfolioProfiles
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Profile {request.Id} not found.");

        profile.FullName = request.FullName;
        profile.Headline = request.Headline;
        profile.SubHeadline = request.SubHeadline;
        profile.Bio = request.Bio;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.Location = request.Location;
        profile.AvatarUrl = request.AvatarUrl;
        profile.ResumeUrl = request.ResumeUrl;
        profile.GitHubUrl = request.GitHubUrl;
        profile.LinkedInUrl = request.LinkedInUrl;
        profile.TwitterUrl = request.TwitterUrl;
        profile.WebsiteUrl = request.WebsiteUrl;
        profile.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
