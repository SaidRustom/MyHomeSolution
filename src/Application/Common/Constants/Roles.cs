namespace MyHomeSolution.Application.Common.Constants;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All = [Administrator, Member];
}
