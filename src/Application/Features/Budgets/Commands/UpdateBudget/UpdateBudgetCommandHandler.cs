using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Commands.UpdateBudget;

public sealed class UpdateBudgetCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateBudgetCommand>
{
    public async Task Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var budget = await dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        if (request.ParentBudgetId.HasValue)
        {
            var parentExists = await dbContext.Budgets
                .AnyAsync(b => b.Id == request.ParentBudgetId.Value && !b.IsDeleted, cancellationToken);
            if (!parentExists)
                throw new NotFoundException(nameof(Budget), request.ParentBudgetId.Value);
        }

        budget.Name = request.Name;
        budget.Description = request.Description;
        budget.Amount = request.Amount;
        budget.Currency = request.Currency;
        budget.Category = request.Category;
        budget.Period = request.Period;
        budget.StartDate = request.StartDate;
        budget.EndDate = request.EndDate;
        budget.IsRecurring = request.IsRecurring;
        budget.ParentBudgetId = request.ParentBudgetId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
