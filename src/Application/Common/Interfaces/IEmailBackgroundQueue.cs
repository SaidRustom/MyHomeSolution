namespace MyHomeSolution.Application.Common.Interfaces;

public sealed record EmailMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody);

public interface IEmailBackgroundQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken);
}
