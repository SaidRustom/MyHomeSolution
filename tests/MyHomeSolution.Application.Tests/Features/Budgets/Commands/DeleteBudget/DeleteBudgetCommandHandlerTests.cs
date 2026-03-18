using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Budgets.Commands.DeleteBudget;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Budgets.Commands.DeleteBudget;

public sealed class DeleteBudgetCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public DeleteBudgetCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteBudget()
    {
        var budget = await SeedBudgetAsync();

        using var context = _factory.CreateContext();
        var handler = new DeleteBudgetCommandHandler(_mediator, context, _currentUserService);

        await handler.Handle(new DeleteBudgetCommand(budget.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var deleted = await assertContext.Budgets
            .IgnoreQueryFilters()
            .FirstAsync(b => b.Id == budget.Id);

        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
        deleted.DeletedBy.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_ShouldDetachChildBudgets()
    {
        using var setupContext = _factory.CreateContext();
        var parent = new Budget
        {
            Name = "Parent",
            Amount = 3000m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        var child = new Budget
        {
            Name = "Child",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.Groceries,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            IsRecurring = true,
            ParentBudgetId = parent.Id,
            CreatedBy = "user-1"
        };
        setupContext.Budgets.Add(parent);
        setupContext.Budgets.Add(child);
        await setupContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new DeleteBudgetCommandHandler(_mediator, context, _currentUserService);

        await handler.Handle(new DeleteBudgetCommand(parent.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var childUpdated = await assertContext.Budgets.FirstAsync(b => b.Id == child.Id);
        childUpdated.ParentBudgetId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBudgetNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteBudgetCommandHandler(_mediator, context, _currentUserService);

        var act = () => handler.Handle(
            new DeleteBudgetCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Budget> SeedBudgetAsync()
    {
        using var context = _factory.CreateContext();
        var budget = new Budget
        {
            Name = "Test Budget",
            Amount = 500m,
            Currency = "CAD",
            Category = BudgetCategory.General,
            Period = BudgetPeriod.Monthly,
            StartDate = DateTimeOffset.UtcNow,
            IsRecurring = true,
            CreatedBy = "user-1"
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    public void Dispose() => _factory.Dispose();
}
