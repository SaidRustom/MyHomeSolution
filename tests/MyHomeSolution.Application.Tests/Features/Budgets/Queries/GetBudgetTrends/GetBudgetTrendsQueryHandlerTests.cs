using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetTrends;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgetTrends;

public sealed class GetBudgetTrendsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetTrendsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldReturnHistoricalTrends()
    {
        await SeedHistoricalBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTrendsQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTrendsQuery
        {
            Periods = 3,
            AsOfDate = Now
        }, CancellationToken.None);

        result.Periods.Should().HaveCount(3);
        result.AverageSpentPerPeriod.Should().BeGreaterThan(0);
        result.AverageBudgetedPerPeriod.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_ShouldFilterByBudgetId()
    {
        var budgetId = await SeedHistoricalBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTrendsQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTrendsQuery
        {
            BudgetId = budgetId,
            Periods = 3,
            AsOfDate = Now
        }, CancellationToken.None);

        result.Periods.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTrendDirection()
    {
        await SeedHistoricalBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetTrendsQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTrendsQuery
        {
            Periods = 3,
            AsOfDate = Now
        }, CancellationToken.None);

        result.TrendDirection.Should().BeOneOf("increasing", "decreasing", "stable");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyWhenNoOccurrences()
    {
        using var context = _factory.CreateContext();
        var handler = new GetBudgetTrendsQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetTrendsQuery
        {
            Periods = 6
        }, CancellationToken.None);

        result.Periods.Should().BeEmpty();
        result.AverageSpentPerPeriod.Should().Be(0);
    }

    private async Task<Guid> SeedHistoricalBudgetAsync()
    {
        using var context = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Monthly Groceries",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddMonths(-4),
            IsRecurring = true,
            CreatedBy = "user-1"
        };

        // Create 3 completed monthly occurrences
        for (var i = 3; i >= 1; i--)
        {
            budget.Occurrences.Add(new BudgetOccurrence
            {
                BudgetId = budget.Id,
                PeriodStart = Now.AddMonths(-i),
                PeriodEnd = Now.AddMonths(-i + 1).AddTicks(-1),
                AllocatedAmount = 500m,
                CarryoverAmount = 0m
            });
        }

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget.Id;
    }

    public void Dispose() => _factory.Dispose();
}
