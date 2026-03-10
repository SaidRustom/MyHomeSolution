using System.Diagnostics;
using System.Text.Json;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;

namespace MyHomeSolution.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IServiceScopeFactory scopeFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled by the client");
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                validationEx.Message,
                (IDictionary<string, string[]>?)validationEx.Errors),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundEx.Message,
                (IDictionary<string, string[]>?)null),

            ForbiddenAccessException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenEx.Message,
                (IDictionary<string, string[]>?)null),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictEx.Message,
                (IDictionary<string, string[]>?)null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                (IDictionary<string, string[]>?)null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception — TraceId: {TraceId}", traceId);
        }

        if (context.Response.HasStarted)
        {
            logger.LogWarning("Response already started — cannot write error response for TraceId: {TraceId}", traceId);
            return;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problem = new ProblemResponse
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            TraceId = traceId,
            Errors = errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));

        // Persist the exception to the database for the admin dashboard
        await PersistExceptionAsync(context, exception, statusCode, traceId,
            statusCode < 500);
    }

    private async Task PersistExceptionAsync(
        HttpContext context, Exception exception, int statusCode,
        string traceId, bool isHandled)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var exceptionLogService = scope.ServiceProvider.GetRequiredService<IExceptionLogService>();

            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            await exceptionLogService.LogAsync(
                exception,
                thrownByService: "ExceptionHandlingMiddleware",
                requestPath: context.Request.Path,
                httpMethod: context.Request.Method,
                userId: userId,
                traceId: traceId,
                httpStatusCode: statusCode,
                isHandled: isHandled,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist exception log for TraceId: {TraceId}", traceId);
        }
    }

    private sealed class ProblemResponse
    {
        public string Type { get; init; } = default!;
        public string Title { get; init; } = default!;
        public int Status { get; init; }
        public string Detail { get; init; } = default!;
        public string TraceId { get; init; } = default!;
        public IDictionary<string, string[]>? Errors { get; init; }
    }
}
