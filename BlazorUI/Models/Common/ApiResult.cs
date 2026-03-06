using System.Diagnostics.CodeAnalysis;

namespace BlazorUI.Models.Common;

public sealed class ApiResult<T>
{
    public T? Value { get; }
    public ApiProblemDetails? Problem { get; }
    public int StatusCode { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; }

    private ApiResult(T value, int statusCode)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = statusCode;
    }

    private ApiResult(ApiProblemDetails problem, int statusCode)
    {
        Problem = problem;
        IsSuccess = false;
        StatusCode = statusCode;
    }

    public static ApiResult<T> Success(T value, int statusCode = 200) => new(value, statusCode);
    public static ApiResult<T> Failure(ApiProblemDetails problem, int statusCode) => new(problem, statusCode);
}

public sealed class ApiResult
{
    public ApiProblemDetails? Problem { get; }
    public int StatusCode { get; }

    [MemberNotNullWhen(false, nameof(Problem))]
    public bool IsSuccess { get; }

    private ApiResult(int statusCode)
    {
        IsSuccess = true;
        StatusCode = statusCode;
    }

    private ApiResult(ApiProblemDetails problem, int statusCode)
    {
        Problem = problem;
        IsSuccess = false;
        StatusCode = statusCode;
    }

    public static ApiResult Success(int statusCode = 204) => new(statusCode);
    public static ApiResult Failure(ApiProblemDetails problem, int statusCode) => new(problem, statusCode);
}
