using Microsoft.AspNetCore.Identity;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Infrastructure.Identity;

internal static class IdentityResultExtensions
{
    public static IdentityResultDto ToResultDto(this IdentityResult result)
    {
        return result.Succeeded
            ? IdentityResultDto.Success()
            : IdentityResultDto.Failure(result.Errors.Select(e => e.Description));
    }
}
