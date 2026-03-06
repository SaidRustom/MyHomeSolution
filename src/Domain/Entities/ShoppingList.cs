using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class ShoppingList : BaseAuditableEntity, IBillable
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ShoppingListCategory Category { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ShoppingItem> Items { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
