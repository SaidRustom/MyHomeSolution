using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Commands.DeleteBudget;

public sealed class DeleteBudgetCommandHandler(
    IMediator mediator,
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var budget = await dbContext.Budgets
            .Include(b => b.ChildBudgets)
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        // Detach child budgets from this parent
        foreach (var child in budget.ChildBudgets)
        {
            child.ParentBudgetId = null;
        }

        var parent = await dbContext.BudgetOccurrences
            .IgnoreQueryFilters()
            .Where(b => b.BudgetId == budget.ParentBudgetId && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        var active = await dbContext.BudgetOccurrences
            .IgnoreQueryFilters()
            .Where(b => b.BudgetId == budget.Id && b.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (parent != null && active != null && active.Balance != 0)
        {
            await mediator.Send(new TransferBudgetFundsCommand
            {
                SourceOccurrenceId = active.Id,
                DestinationOccurrenceId = parent.Id,
                Amount = active.Balance,
                Reason = $"Automatic transfer from child budget {budget.Name} to parent on delete"
            });
        }

        budget.IsDeleted = true;
        budget.DeletedAt = DateTimeOffset.UtcNow;
        budget.DeletedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
