using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IReceiptAnalysisService
{
    Task<ReceiptAnalysisResult> AnalyzeAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<ReceiptAnalysisResult> AnalyzeAsync(
        Stream imageStream,
        string contentType,
        IReadOnlyList<string> existingItemNames,
        CancellationToken cancellationToken = default);
}
