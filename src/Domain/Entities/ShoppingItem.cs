using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public sealed class ShoppingItem : BaseEntity
{
    public Guid ShoppingListId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public bool IsChecked { get; set; }
    public DateTimeOffset? CheckedAt { get; set; }
    public string? CheckedByUserId { get; set; }
    public int SortOrder { get; set; }

    public ShoppingList ShoppingList { get; set; } = default!;
}
