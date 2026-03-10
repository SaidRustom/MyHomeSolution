namespace MyHomeSolution.Application.Common.Interfaces;

public sealed record ExceptionAnalysisResult(
    string Analysis,
    string? SuggestedPrompt);

public interface IExceptionAnalysisService
{
    Task<ExceptionAnalysisResult> AnalyseAsync(
        string exceptionType,
        string message,
        string? stackTrace,
        string? innerException,
        bool isValidationException,
        CancellationToken cancellationToken = default);
}
