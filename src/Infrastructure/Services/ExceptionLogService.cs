using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Infrastructure.Services;

public sealed class ExceptionLogService(
    IApplicationDbContext dbContext,
    IExceptionAnalysisService analysisService,
    IHostEnvironment hostEnvironment,
    IDateTimeProvider dateTimeProvider,
    ILogger<ExceptionLogService> logger)
    : IExceptionLogService
{
    public async Task LogAsync(
        Exception exception,
        string thrownByService,
        string? requestPath = null,
        string? httpMethod = null,
        string? userId = null,
        string? traceId = null,
        int? httpStatusCode = null,
        bool isHandled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            var (className, methodName) = ParseOrigin(exception);
            var isValidation = exception is Application.Common.Exceptions.ValidationException;

            var severity = ClassifySeverity(exception, httpStatusCode);

            var log = new ExceptionLog
            {
                OccurredAt = dateTimeProvider.UtcNow,
                ExceptionType = exceptionType,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = FlattenInnerExceptions(exception),
                ThrownByService = thrownByService,
                ClassName = className,
                MethodName = methodName,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                UserId = userId,
                TraceId = traceId,
                HttpStatusCode = httpStatusCode,
                Severity = severity,
                IsHandled = isHandled,
                Environment = hostEnvironment.EnvironmentName
            };

            // Persist immediately so the record exists even if AI analysis fails
            dbContext.ExceptionLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Fire-and-forget AI analysis (don't let it block the response pipeline)
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await analysisService.AnalyseAsync(
                        exceptionType,
                        exception.Message,
                        exception.StackTrace,
                        log.InnerException,
                        isValidation,
                        CancellationToken.None);

                    log.AiAnalysis = result.Analysis;
                    log.AiSuggestedPrompt = result.SuggestedPrompt;
                    log.IsAiAnalysed = true;
                    log.AiAnalysedAt = dateTimeProvider.UtcNow;

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "AI analysis failed for ExceptionLog {Id}", log.Id);
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Exception logging must never crash the application
            logger.LogError(ex, "Failed to persist exception log");
        }
    }

    public async Task<Guid?> LogAndReturnIdAsync(
        Exception exception,
        string thrownByService,
        string? requestPath = null,
        string? httpMethod = null,
        string? userId = null,
        string? traceId = null,
        int? httpStatusCode = null,
        bool isHandled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            var (className, methodName) = ParseOrigin(exception);
            var severity = ClassifySeverity(exception, httpStatusCode);

            var log = new ExceptionLog
            {
                OccurredAt = dateTimeProvider.UtcNow,
                ExceptionType = exceptionType,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = FlattenInnerExceptions(exception),
                ThrownByService = thrownByService,
                ClassName = className,
                MethodName = methodName,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                UserId = userId,
                TraceId = traceId,
                HttpStatusCode = httpStatusCode,
                Severity = severity,
                IsHandled = isHandled,
                Environment = hostEnvironment.EnvironmentName
            };

            dbContext.ExceptionLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Fire-and-forget AI analysis
            _ = Task.Run(async () =>
            {
                try
                {
                    var isValidation = exception is Application.Common.Exceptions.ValidationException;
                    var result = await analysisService.AnalyseAsync(
                        exceptionType,
                        exception.Message,
                        exception.StackTrace,
                        log.InnerException,
                        isValidation,
                        CancellationToken.None);

                    log.AiAnalysis = result.Analysis;
                    log.AiSuggestedPrompt = result.SuggestedPrompt;
                    log.IsAiAnalysed = true;
                    log.AiAnalysedAt = dateTimeProvider.UtcNow;

                    await dbContext.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "AI analysis failed for ExceptionLog {Id}", log.Id);
                }
            }, CancellationToken.None);

            return log.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist exception log");
            return null;
        }
    }

    private static ExceptionSeverity ClassifySeverity(Exception exception, int? httpStatusCode)
    {
        return exception switch
        {
            Application.Common.Exceptions.ValidationException => ExceptionSeverity.Low,
            Application.Common.Exceptions.NotFoundException => ExceptionSeverity.Low,
            Application.Common.Exceptions.ForbiddenAccessException => ExceptionSeverity.Medium,
            Application.Common.Exceptions.ConflictException => ExceptionSeverity.Medium,
            OperationCanceledException => ExceptionSeverity.Low,
            _ => httpStatusCode switch
            {
                >= 500 => ExceptionSeverity.Critical,
                >= 400 => ExceptionSeverity.Medium,
                _ => ExceptionSeverity.High
            }
        };
    }

    private static (string? ClassName, string? MethodName) ParseOrigin(Exception exception)
    {
        if (string.IsNullOrWhiteSpace(exception.StackTrace))
            return (null, null);

        var match = StackFramePattern.Match(exception.StackTrace);
        if (!match.Success)
            return (null, null);

        var fullMethod = match.Groups[1].Value;
        var lastDot = fullMethod.LastIndexOf('.');
        if (lastDot <= 0)
            return (null, fullMethod);

        return (fullMethod[..lastDot], fullMethod[(lastDot + 1)..]);
    }

    private static string? FlattenInnerExceptions(Exception exception)
    {
        if (exception.InnerException is null)
            return null;

        var parts = new List<string>();
        var inner = exception.InnerException;
        var depth = 0;

        while (inner is not null && depth < 5)
        {
            parts.Add($"[{inner.GetType().FullName}] {inner.Message}");
            if (!string.IsNullOrWhiteSpace(inner.StackTrace))
                parts.Add(inner.StackTrace);
            inner = inner.InnerException;
            depth++;
        }

        return string.Join(System.Environment.NewLine + "--- Inner Exception ---" + System.Environment.NewLine, parts);
    }

    private static readonly Regex StackFramePattern = new(@"at\s+(.+?)\(", RegexOptions.Compiled);
}
