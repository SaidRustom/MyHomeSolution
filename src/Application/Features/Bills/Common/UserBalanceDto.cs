namespace MyHomeSolution.Application.Features.Bills.Common;

public sealed record UserBalanceDto
{
    public required string UserId { get; init; }
    public required string CounterpartyUserId { get; init; }
    public decimal NetBalance { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal TotalOwing { get; init; }
}
