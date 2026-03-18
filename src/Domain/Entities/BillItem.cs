using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public sealed class BillItem : BaseEntity
{
    public Guid BillId { get; set; }
    public string Name { get; set; } = default!;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid? ShoppingListId { get; set; }

    public Bill Bill { get; set; } = default!;
}
