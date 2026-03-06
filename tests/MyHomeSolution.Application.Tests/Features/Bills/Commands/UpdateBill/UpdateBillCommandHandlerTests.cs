using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Commands.UpdateBill;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.UpdateBill;

public sealed class UpdateBillCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public UpdateBillCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUpdateBillProperties()
    {
        var bill = await SeedBill(100m);

        using var context = _factory.CreateContext();
        var handler = new UpdateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new UpdateBillCommand
        {
            Id = bill.Id,
            Title = "Updated Groceries",
            Description = "Updated description",
            Amount = 150m,
            Currency = "EUR",
            Category = BillCategory.Supplies,
            BillDate = DateTimeOffset.UtcNow,
            Notes = "Updated notes"
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.Bills.FirstAsync(b => b.Id == bill.Id);
        updated.Title.Should().Be("Updated Groceries");
        updated.Amount.Should().Be(150m);
        updated.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_ShouldRecalculateSplitAmounts_WhenAmountChanges()
    {
        var bill = await SeedBill(100m);

        using var context = _factory.CreateContext();
        var handler = new UpdateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new UpdateBillCommand
        {
            Id = bill.Id,
            Title = "Groceries",
            Amount = 200m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.Bills.Include(b => b.Splits)
            .FirstAsync(b => b.Id == bill.Id);

        updated.Splits.Should().AllSatisfy(s => s.Amount.Should().Be(100m));
    }

    [Fact]
    public async Task Handle_ShouldPublishBillUpdatedEvent()
    {
        var bill = await SeedBill(100m);

        using var context = _factory.CreateContext();
        var handler = new UpdateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new UpdateBillCommand
        {
            Id = bill.Id,
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow
        };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillUpdatedEvent>(e => e.BillId == bill.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new UpdateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new UpdateBillCommand
        {
            Id = Guid.CreateVersion7(),
            Title = "Test",
            Amount = 10m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Bill> SeedBill(decimal amount)
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Groceries",
            Amount = amount,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id,
            UserId = "user-1",
            Percentage = 50m,
            Amount = amount / 2,
            Status = SplitStatus.Paid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id,
            UserId = "user-2",
            Percentage = 50m,
            Amount = amount / 2,
            Status = SplitStatus.Unpaid
        });
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
        return bill;
    }

    public void Dispose() => _factory.Dispose();
}
