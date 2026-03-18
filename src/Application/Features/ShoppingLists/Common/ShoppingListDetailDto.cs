using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.ShoppingLists.Common;

public sealed record ShoppingListDetailDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public ShoppingListCategory Category { get; init; }
    public DateOnly? DueDate { get; init; }
    public Guid? DefaultBudgetId { get; init; }
    public bool IsCompleted { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public IReadOnlyList<ShoppingItemDto> Items { get; init; } = []; 
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? CreatedByFullName { get; init; }
    public DateTimeOffset? LastModifiedAt { get; init; }
}
