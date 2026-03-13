using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Common;

public sealed record BillSplitDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public string? UserFullName { get; init; }
    public string? UserAvatarUrl { get; init; }
    public decimal Percentage { get; init; }
    public decimal Amount { get; init; }
    public SplitStatus Status { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public string? OwedToUserId { get; init; }
    public string? OwedToUserFullName { get; init; }
}
