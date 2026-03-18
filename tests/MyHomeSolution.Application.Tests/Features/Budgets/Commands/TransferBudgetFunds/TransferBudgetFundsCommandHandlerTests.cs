using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.TransferBudgetFunds;

public sealed class TransferBudgetFundsCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public TransferBudgetFundsCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldTransferFundsBetweenOccurrences()
    {
        var (source, destination) = await SeedTwoOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new TransferBudgetFundsCommandHandler(context, _currentUserService);

        var transferId = await handler.Handle(new TransferBudgetFundsCommand
        {
            SourceOccurrenceId = source.Id,
            DestinationOccurrenceId = destination.Id,
            Amount = 150m,
            Reason = "Rebalance"
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var srcOcc = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == source.Id);
        srcOcc.AllocatedAmount.Should().Be(350m); // 500 - 150

        var destOcc = await assertContext.BudgetOccurrences.FirstAsync(o => o.Id == destination.Id);
        destOcc.AllocatedAmount.Should().Be(650m); // 500 + 150

        var transfer = await assertContext.BudgetTransfers.FirstAsync(t => t.Id == transferId);
        transfer.Amount.Should().Be(150m);
        transfer.Reason.Should().Be("Rebalance");
        transfer.SourceOccurrenceId.Should().Be(source.Id);
        transfer.DestinationOccurrenceId.Should().Be(destination.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSourceNotFound()
    {
        var (_, destination) = await SeedTwoOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new TransferBudgetFundsCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new TransferBudgetFundsCommand
        {
            SourceOccurrenceId = Guid.NewGuid(),
            DestinationOccurrenceId = destination.Id,
            Amount = 100m
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenDestinationNotFound()
    {
        var (source, _) = await SeedTwoOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new TransferBudgetFundsCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new TransferBudgetFundsCommand
        {
            SourceOccurrenceId = source.Id,
            DestinationOccurrenceId = Guid.NewGuid(),
            Amount = 100m
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<(BudgetOccurrence Source, BudgetOccurrence Destination)> SeedTwoOccurrencesAsync()
    {
        using var context = _factory.CreateContext();
        var now = DateTimeOffset.UtcNow;

        var budget = new Budget
        {
            Name = "Test Budget",
            Amount = 1000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = now,
            IsRecurring = true,
            CreatedBy = "user-1"
        };

        var source = new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        };

        var destination = new BudgetOccurrence
        {
            BudgetId = budget.Id,
            PeriodStart = now.AddMonths(1),
            PeriodEnd = now.AddMonths(2),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        };

        budget.Occurrences.Add(source);
        budget.Occurrences.Add(destination);
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        return (source, destination);
    }

    public void Dispose() => _factory.Dispose();
}
