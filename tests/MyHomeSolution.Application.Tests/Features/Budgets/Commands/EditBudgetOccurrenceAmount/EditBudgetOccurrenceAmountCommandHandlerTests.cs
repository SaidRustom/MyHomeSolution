using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.EditBudgetOccurrenceAmount;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.EditBudgetOccurrenceAmount;

public sealed class EditBudgetOccurrenceAmountCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public EditBudgetOccurrenceAmountCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUpdateOccurrenceAmount()
    {
        var (_, occurrence) = await SeedBudgetWithOccurrenceAsync();

        using var context = _factory.CreateContext();
        var handler = new EditBudgetOccurrenceAmountCommandHandler(context, _currentUserService);

        await handler.Handle(new EditBudgetOccurrenceAmountCommand
        {
            OccurrenceId = occurrence.Id,
            NewAmount = 750m,
            Notes = "Adjusted for holiday season"
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == occurrence.Id);
        updated.AllocatedAmount.Should().Be(750m);
        updated.Notes.Should().Be("Adjusted for holiday season");
    }

    [Fact]
    public async Task Handle_ShouldCreateTransferWhenIncreasing()
    {
        var (_, occurrence1) = await SeedBudgetWithOccurrenceAsync();
        var occurrence2 = await SeedSecondOccurrenceAsync(occurrence1.BudgetId);

        using var context = _factory.CreateContext();
        var handler = new EditBudgetOccurrenceAmountCommandHandler(context, _currentUserService);

        await handler.Handle(new EditBudgetOccurrenceAmountCommand
        {
            OccurrenceId = occurrence1.Id,
            NewAmount = 700m,          // Increase by 200
            TransferOccurrenceId = occurrence2.Id,
            TransferReason = "Rebalancing"
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var source = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == occurrence2.Id);
        source.AllocatedAmount.Should().Be(300m); // 500 - 200

        var dest = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == occurrence1.Id);
        dest.AllocatedAmount.Should().Be(700m);

        var transfer = await assertContext.BudgetTransfers.FirstOrDefaultAsync();
        transfer.Should().NotBeNull();
        transfer!.Amount.Should().Be(200m);
        transfer.SourceOccurrenceId.Should().Be(occurrence2.Id);
        transfer.DestinationOccurrenceId.Should().Be(occurrence1.Id);
        transfer.Reason.Should().Be("Rebalancing");
    }

    [Fact]
    public async Task Handle_ShouldCreateTransferWhenDecreasing()
    {
        var (_, occurrence1) = await SeedBudgetWithOccurrenceAsync();
        var occurrence2 = await SeedSecondOccurrenceAsync(occurrence1.BudgetId);

        using var context = _factory.CreateContext();
        var handler = new EditBudgetOccurrenceAmountCommandHandler(context, _currentUserService);

        await handler.Handle(new EditBudgetOccurrenceAmountCommand
        {
            OccurrenceId = occurrence1.Id,
            NewAmount = 300m,          // Decrease by 200
            TransferOccurrenceId = occurrence2.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var source = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == occurrence1.Id);
        source.AllocatedAmount.Should().Be(300m);

        var dest = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == occurrence2.Id);
        dest.AllocatedAmount.Should().Be(700m); // 500 + 200

        var transfer = await assertContext.BudgetTransfers.FirstOrDefaultAsync();
        transfer.Should().NotBeNull();
        transfer!.Amount.Should().Be(200m);
        transfer.SourceOccurrenceId.Should().Be(occurrence1.Id);
        transfer.DestinationOccurrenceId.Should().Be(occurrence2.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOccurrenceNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new EditBudgetOccurrenceAmountCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new EditBudgetOccurrenceAmountCommand
        {
            OccurrenceId = Guid.NewGuid(),
            NewAmount = 100m
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTransferOccurrenceNotFound()
    {
        var (_, occurrence) = await SeedBudgetWithOccurrenceAsync();

        using var context = _factory.CreateContext();
        var handler = new EditBudgetOccurrenceAmountCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new EditBudgetOccurrenceAmountCommand
        {
            OccurrenceId = occurrence.Id,
            NewAmount = 700m,
            TransferOccurrenceId = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<(Budget Budget, BudgetOccurrence Occurrence)> SeedBudgetWithOccurrenceAsync()
    {
        using var context = _factory.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var budget = new Budget
        {
            Name = "Test",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = now,
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        var occurrence = new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        };
        budget.Occurrences.Add(occurrence);
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return (budget, occurrence);
    }

    private async Task<BudgetOccurrence> SeedSecondOccurrenceAsync(Guid budgetId)
    {
        using var context = _factory.CreateContext();
        var now = DateTimeOffset.UtcNow;
        var occurrence = new BudgetOccurrence
        {
            BudgetId = budgetId,
            PeriodStart = now.AddMonths(1),
            PeriodEnd = now.AddMonths(2),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        };
        context.BudgetOccurrences.Add(occurrence);
        await context.SaveChangesAsync();
        return occurrence;
    }

    public void Dispose() => _factory.Dispose();
}
