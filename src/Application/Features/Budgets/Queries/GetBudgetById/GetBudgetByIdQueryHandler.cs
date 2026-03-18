using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Common;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Application;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetById;

public sealed class GetBudgetByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetBudgetByIdQuery, BudgetDetailDto>
{
    public async Task<BudgetDetailDto> Handle(GetBudgetByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var budget = await dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Occurrences)
                .ThenInclude(o => o.OutgoingTransfers)
            .Include(b => b.Occurrences)
                .ThenInclude(o => o.IncomingTransfers)
            .Include(b => b.BillLinks)
                .ThenInclude(l => l.Bill)
            .Include(b => b.ParentBudget)
            .Where(b => !b.IsDeleted)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        // Check access: owner or shared
        var isOwner = budget.CreatedBy == userId;
        var hasShareAccess = await dbContext.EntityShares
            .AnyAsync(s => s.EntityType == EntityTypes.Budget
                && s.EntityId == budget.Id
                && s.SharedWithUserId == userId
                && !s.IsDeleted, cancellationToken);

        if (!isOwner && !hasShareAccess)
            throw new ForbiddenAccessException();

        // Resolve transfer budget names
        var allTransferOccurrenceIds = budget.Occurrences
            .SelectMany(o => o.OutgoingTransfers.Select(t => t.DestinationOccurrenceId))
            .Concat(budget.Occurrences.SelectMany(o => o.IncomingTransfers.Select(t => t.SourceOccurrenceId)))
            .Distinct()
            .ToList();

        var occurrenceBudgetMap = await dbContext.BudgetOccurrences
            .AsNoTracking()
            .Include(o => o.Budget)
            .Where(o => allTransferOccurrenceIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Budget.Name, cancellationToken);

        // Load child budgets
        var children = await dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Occurrences)
            .Where(b => !b.IsDeleted && b.ParentBudgetId == budget.Id)
            .ToListAsync(cancellationToken);

        var now = dateTimeProvider.UtcNow;

        var childDtos = children.Select(c =>
        {
            var currentOcc = c.Occurrences
                .FirstOrDefault(o => o.PeriodStart <= now && o.PeriodEnd >= now);
            return new BudgetChildDto
            {
                Id = c.Id,
                Name = c.Name,
                Amount = c.Amount,
                Category = c.Category,
                Period = c.Period,
                CurrentPeriodSpent = currentOcc?.SpentAmount ?? 0,
                CurrentPeriodRemaining = (currentOcc?.AllocatedAmount ?? c.Amount) - (currentOcc?.SpentAmount ?? 0)
            };
        }).ToList();

        // Resolve creator name
        string? createdByFullName = null;
        if (!string.IsNullOrEmpty(budget.CreatedBy))
        {
            var nameMap = await identityService.GetUserFullNamesByIdsAsync([budget.CreatedBy], cancellationToken);
            createdByFullName = nameMap.GetValueOrDefault(budget.CreatedBy);
        }

        // Load tasks linked to this budget
        var linkedTasks = await dbContext.HouseholdTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.DefaultBudgetId == budget.Id)
            .ToListAsync(cancellationToken);

        // Get bill IDs linked to this budget
        var budgetBillIds = budget.BillLinks
            .Where(l => !l.Bill.IsDeleted)
            .Select(l => l.Bill.Id)
            .ToHashSet();

        // Build task DTOs with their related bills
        var taskDtos = new List<BudgetTaskDto>();
        foreach (var t in linkedTasks)
        {
            // Find bills created from this task's occurrences that are linked to this budget
            var taskBills = await dbContext.Bills
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.RelatedEntityId == t.Id
                    && b.BudgetLink != null && b.BudgetLink.BudgetId == budget.Id)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new BudgetBillDto
                {
                    BillId = b.Id,
                    BillTitle = b.Title,
                    BillAmount = b.Amount,
                    BillDate = b.BillDate,
                    BillCategory = b.Category
                })
                .ToListAsync(cancellationToken);

            taskDtos.Add(new BudgetTaskDto
            {
                TaskId = t.Id,
                TaskTitle = t.Title,
                IsRecurring = t.IsRecurring,
                IsActive = t.IsActive,
                Bills = taskBills
            });
        }

        // Load shopping lists linked to this budget
        var linkedShoppingLists = await dbContext.ShoppingLists
            .AsNoTracking()
            .Where(sl => !sl.IsDeleted && sl.DefaultBudgetId == budget.Id)
            .ToListAsync(cancellationToken);

        var shoppingListDtos = new List<BudgetShoppingListDto>();
        foreach (var sl in linkedShoppingLists)
        {
            var slBills = await dbContext.Bills
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.RelatedEntityId == sl.Id
                    && b.BudgetLink != null && b.BudgetLink.BudgetId == budget.Id)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new BudgetBillDto
                {
                    BillId = b.Id,
                    BillTitle = b.Title,
                    BillAmount = b.Amount,
                    BillDate = b.BillDate,
                    BillCategory = b.Category
                })
                .ToListAsync(cancellationToken);

            shoppingListDtos.Add(new BudgetShoppingListDto
            {
                ShoppingListId = sl.Id,
                Title = sl.Title,
                IsCompleted = sl.IsCompleted,
                Bills = slBills
            });
        }

        return new BudgetDetailDto
        {
            Id = budget.Id,
            Name = budget.Name,
            Description = budget.Description,
            Amount = budget.Amount,
            Currency = budget.Currency,
            Category = budget.Category,
            Period = budget.Period,
            StartDate = budget.StartDate,
            EndDate = budget.EndDate,
            IsRecurring = budget.IsRecurring,
            ParentBudgetId = budget.ParentBudgetId,
            ParentBudgetName = budget.ParentBudget?.Name,
            Occurrences = budget.Occurrences
                .OrderByDescending(o => o.PeriodStart)
                .Select(o =>
                {
                    var allTransfers = o.OutgoingTransfers
                        .Select(t => new BudgetTransferDto
                        {
                            Id = t.Id,
                            SourceOccurrenceId = t.SourceOccurrenceId,
                            SourceBudgetName = budget.Name,
                            DestinationOccurrenceId = t.DestinationOccurrenceId,
                            DestinationBudgetName = occurrenceBudgetMap.GetValueOrDefault(t.DestinationOccurrenceId),
                            Amount = t.Amount,
                            Reason = t.Reason,
                            CreatedAt = t.CreatedAt
                        })
                        .Concat(o.IncomingTransfers.Select(t => new BudgetTransferDto
                        {
                            Id = t.Id,
                            SourceOccurrenceId = t.SourceOccurrenceId,
                            SourceBudgetName = occurrenceBudgetMap.GetValueOrDefault(t.SourceOccurrenceId),
                            DestinationOccurrenceId = t.DestinationOccurrenceId,
                            DestinationBudgetName = budget.Name,
                            Amount = t.Amount,
                            Reason = t.Reason,
                            CreatedAt = t.CreatedAt
                        }))
                        .OrderByDescending(t => t.CreatedAt)
                        .ToList();

                    var remaining = o.AllocatedAmount + o.CarryoverAmount - o.SpentAmount;
                    var totalAllocated = o.AllocatedAmount + o.CarryoverAmount;
                    var pct = totalAllocated > 0
                        ? Math.Round(o.SpentAmount / totalAllocated * 100, 2)
                        : 0;

                    return new BudgetOccurrenceDto
                    {
                        Id = o.Id,
                        PeriodStart = o.PeriodStart,
                        PeriodEnd = o.PeriodEnd,
                        AllocatedAmount = o.AllocatedAmount,
                        SpentAmount = o.SpentAmount,
                        CarryoverAmount = o.CarryoverAmount,
                        RemainingAmount = remaining,
                        PercentUsed = pct,
                        Notes = o.Notes,
                        Transfers = allTransfers
                    };
                }).ToList(),
            ChildBudgets = childDtos,
            LinkedBills = budget.BillLinks
                .Where(l => !l.Bill.IsDeleted)
                .OrderByDescending(l => l.Bill.BillDate)
                .Select(l => new BudgetBillDto
                {
                    BillId = l.Bill.Id,
                    BillTitle = l.Bill.Title,
                    BillAmount = l.Bill.Amount,
                    BillDate = l.Bill.BillDate,
                    BillCategory = l.Bill.Category
                }).ToList(),
            LinkedTasks = taskDtos,
            LinkedShoppingLists = shoppingListDtos,
            TotalSpent = budget.Occurrences.Sum(o => o.SpentAmount),
            TotalAllocated = budget.Occurrences.Sum(o => o.AllocatedAmount + o.CarryoverAmount),
            TotalRemaining = budget.Occurrences.Sum(o => o.AllocatedAmount + o.CarryoverAmount - o.SpentAmount),
            CreatedAt = budget.CreatedAt,
            CreatedByUserId = budget.CreatedBy,
            CreatedByFullName = createdByFullName,
            LastModifiedAt = budget.LastModifiedAt
        };
    }
}
