namespace MyHomeSolution.Application.Common.Models;

public sealed record IdentityResultDto(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static IdentityResultDto Success() => new(true, []);

    public static IdentityResultDto Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList());
}
