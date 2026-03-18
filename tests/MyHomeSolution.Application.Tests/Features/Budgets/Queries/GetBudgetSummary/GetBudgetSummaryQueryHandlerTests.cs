using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetSummary;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgetSummary;

public sealed class GetBudgetSummaryQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetSummaryQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldReturnAggregatedSummary()
    {
        await SeedBudgetsWithSpendingAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetSummaryQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetSummaryQuery
        {
            FromDate = Now.AddDays(-30),
            ToDate = Now.AddDays(30)
        }, CancellationToken.None);

        result.TotalBudgets.Should().Be(2);
        result.TotalBudgeted.Should().Be(1500m); // 1000 + 500
        result.TotalSpent.Should().Be(700m);      // 500 + 200
        result.TotalRemaining.Should().Be(800m);
    }

    [Fact]
    public async Task Handle_ShouldGroupByCategory()
    {
        await SeedBudgetsWithSpendingAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetSummaryQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetSummaryQuery
        {
            FromDate = Now.AddDays(-30),
            ToDate = Now.AddDays(30)
        }, CancellationToken.None);

        result.ByCategory.Should().HaveCount(2);
        result.ByCategory.Should().Contain(c => c.Category == BudgetCategory.Groceries);
        result.ByCategory.Should().Contain(c => c.Category == BudgetCategory.Entertainment);
    }

    [Fact]
    public async Task Handle_ShouldIdentifyOverBudget()
    {
        await SeedBudgetsWithSpendingAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetSummaryQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetSummaryQuery
        {
            FromDate = Now.AddDays(-30),
            ToDate = Now.AddDays(30)
        }, CancellationToken.None);

        // Groceries: 500/1000 = 50% (on-track)
        // Entertainment: 200/500 = 40% (on-track)
        result.OverBudgetCount.Should().Be(0);
        result.OnTrackCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategory()
    {
        await SeedBudgetsWithSpendingAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetSummaryQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var result = await handler.Handle(new GetBudgetSummaryQuery
        {
            FromDate = Now.AddDays(-30),
            ToDate = Now.AddDays(30),
            Category = BudgetCategory.Groceries
        }, CancellationToken.None);

        result.TotalBudgets.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetBudgetSummaryQueryHandler(
            context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new GetBudgetSummaryQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task SeedBudgetsWithSpendingAsync()
    {
        using var context = _factory.CreateContext();

        var groceries = new Budget
        {
            Name = "Groceries",
            Amount = 1000m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        groceries.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = groceries.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 1000m,
            CarryoverAmount = 0m
        });

        var entertainment = new Budget
        {
            Name = "Entertainment",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Entertainment,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddDays(-15),
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        entertainment.Occurrences.Add(new BudgetOccurrence
        {
            BudgetId = entertainment.Id,
            PeriodStart = Now.AddDays(-15),
            PeriodEnd = Now.AddDays(15),
            AllocatedAmount = 500m,
            CarryoverAmount = 0m
        });

        context.Budgets.Add(groceries);
        context.Budgets.Add(entertainment);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
