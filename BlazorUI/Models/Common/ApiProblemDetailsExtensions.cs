namespace BlazorUI.Models.Common;

public static class ApiProblemDetailsExtensions
{
    /// <summary>
    /// Builds a user-friendly error message from the problem details,
    /// including individual validation error messages when present.
    /// </summary>
    public static string ToUserMessage(this ApiProblemDetails problem)
    {
        if (problem.Errors is { Count: > 0 })
        {
            return string.Join(" ", problem.Errors.SelectMany(e => e.Value));
        }

        return problem.Detail is { Length: > 0 }
            ? problem.Detail
            : problem.Title;
    }
}
