using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Infrastructure.Persistence
{
    public static class QueryExtensions
    {
        public static IQueryable<BudgetOccurrenceDto> SelectBudgetOccurrenceDto(
        this IQueryable<BudgetOccurrence> query)
        {
            return query.Select(o => new BudgetOccurrenceDto
            {
                Id = o.Id,
                AllocatedAmount = o.AllocatedAmount,
                CarryoverAmount = o.CarryoverAmount,
                SpentAmount = o.BillLinks
                    .Where(x => !x.Bill.IsDeleted)
                    .Sum(x => (decimal?)x.Bill.Amount) ?? 0m,
                RemainingAmount = o.AllocatedAmount + o.CarryoverAmount -
                    (o.BillLinks
                        .Where(x => !x.Bill.IsDeleted)
                        .Sum(x => (decimal?)x.Bill.Amount) ?? 0m),
            });
        }
    }
}
