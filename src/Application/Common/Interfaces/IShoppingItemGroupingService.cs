using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Common.Interfaces;

public interface IShoppingItemGroupingService
{
    Task<ShoppingItemGroupResult> GroupItemsAsync(
        IReadOnlyList<string> itemNames,
        CancellationToken cancellationToken = default);
}
