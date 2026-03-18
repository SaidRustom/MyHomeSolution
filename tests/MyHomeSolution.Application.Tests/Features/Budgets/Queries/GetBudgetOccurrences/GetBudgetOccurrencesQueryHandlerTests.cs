using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Queries.GetBudgetOccurrences;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Queries.GetBudgetOccurrences;

public sealed class GetBudgetOccurrencesQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public GetBudgetOccurrencesQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnOccurrences()
    {
        var budget = await SeedBudgetWithOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetOccurrencesQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetBudgetOccurrencesQuery
        {
            BudgetId = budget.Id
        }, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange()
    {
        var budget = await SeedBudgetWithOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetOccurrencesQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetBudgetOccurrencesQuery
        {
            BudgetId = budget.Id,
            FromDate = Now.AddMonths(-1),
            ToDate = Now
        }, CancellationToken.None);

        result.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_ShouldCalculateRemainingAndPercent()
    {
        var budget = await SeedBudgetWithOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetOccurrencesQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetBudgetOccurrencesQuery
        {
            BudgetId = budget.Id
        }, CancellationToken.None);

        var occ = result[0]; // Most recent first
        occ.RemainingAmount.Should().Be(occ.AllocatedAmount + occ.CarryoverAmount - occ.SpentAmount);
        if (occ.AllocatedAmount + occ.CarryoverAmount > 0)
        {
            occ.PercentUsed.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBudgetNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new GetBudgetOccurrencesQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(new GetBudgetOccurrencesQuery
        {
            BudgetId = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotOwnerOrShared()
    {
        _currentUserService.UserId.Returns("user-999");
        var budget = await SeedBudgetWithOccurrencesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetBudgetOccurrencesQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(new GetBudgetOccurrencesQuery
        {
            BudgetId = budget.Id
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private async Task<Budget> SeedBudgetWithOccurrencesAsync()
    {
        using var context = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Test Budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = Now.AddMonths(-3),
            IsRecurring = true,
            CreatedBy = "user-1"
        };

        for (var i = 2; i >= 0; i--)
        {
            budget.Occurrences.Add(new BudgetOccurrence
            {
                BudgetId = budget.Id,
                PeriodStart = Now.AddMonths(-i),
                PeriodEnd = Now.AddMonths(-i + 1).AddTicks(-1),
                AllocatedAmount = 500m,
                CarryoverAmount = i == 2 ? 0m : 50m
            });
        }

        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    public void Dispose() => _factory.Dispose();
}
