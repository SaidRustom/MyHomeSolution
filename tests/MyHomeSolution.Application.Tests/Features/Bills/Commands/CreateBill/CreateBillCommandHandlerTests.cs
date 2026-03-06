using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Commands.CreateBill;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public CreateBillCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldCreateBill_WithEqualSplits()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            Splits =
            [
                new BillSplitRequest { UserId = "user-1" },
                new BillSplitRequest { UserId = "user-2" }
            ]
        };

        var billId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills.Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == billId);

        bill.Should().NotBeNull();
        bill!.Title.Should().Be("Groceries");
        bill.Amount.Should().Be(100m);
        bill.PaidByUserId.Should().Be("user-1");
        bill.Splits.Should().HaveCount(2);
        bill.Splits.Should().AllSatisfy(s => s.Percentage.Should().Be(50m));
        bill.Splits.Should().AllSatisfy(s => s.Amount.Should().Be(50m));
    }

    [Fact]
    public async Task Handle_ShouldCreateBill_WithCustomPercentages()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Rent",
            Amount = 1000m,
            Currency = "USD",
            Category = BillCategory.Rent,
            BillDate = DateTimeOffset.UtcNow,
            Splits =
            [
                new BillSplitRequest { UserId = "user-1", Percentage = 60m },
                new BillSplitRequest { UserId = "user-2", Percentage = 40m }
            ]
        };

        var billId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills.Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == billId);

        bill.Should().NotBeNull();
        var split1 = bill!.Splits.First(s => s.UserId == "user-1");
        var split2 = bill.Splits.First(s => s.UserId == "user-2");
        split1.Amount.Should().Be(600m);
        split2.Amount.Should().Be(400m);
    }

    [Fact]
    public async Task Handle_ShouldMarkPayerSplitAsPaid()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Internet",
            Amount = 60m,
            Currency = "USD",
            Category = BillCategory.Internet,
            BillDate = DateTimeOffset.UtcNow,
            Splits =
            [
                new BillSplitRequest { UserId = "user-1" },
                new BillSplitRequest { UserId = "user-2" }
            ]
        };

        var billId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills.Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == billId);

        var payerSplit = bill!.Splits.First(s => s.UserId == "user-1");
        var otherSplit = bill.Splits.First(s => s.UserId == "user-2");
        payerSplit.Status.Should().Be(SplitStatus.Paid);
        otherSplit.Status.Should().Be(SplitStatus.Unpaid);
    }

    [Fact]
    public async Task Handle_ShouldPublishBillCreatedEvent()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Supplies",
            Amount = 50m,
            Currency = "USD",
            Category = BillCategory.Supplies,
            BillDate = DateTimeOffset.UtcNow,
            Splits = [new BillSplitRequest { UserId = "user-1" }]
        };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillCreatedEvent>(e => e.Title == "Supplies" && e.Amount == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Test",
            Amount = 10m,
            BillDate = DateTimeOffset.UtcNow,
            Splits = [new BillSplitRequest { UserId = "user-1" }]
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldCreateBill_WithThreeWayEqualSplit()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateBillCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateBillCommand
        {
            Title = "Dinner",
            Amount = 90m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            Splits =
            [
                new BillSplitRequest { UserId = "user-1" },
                new BillSplitRequest { UserId = "user-2" },
                new BillSplitRequest { UserId = "user-3" }
            ]
        };

        var billId = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var bill = await assertContext.Bills.Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == billId);

        bill!.Splits.Should().HaveCount(3);
        bill.Splits.Should().AllSatisfy(s => s.Percentage.Should().Be(33.33m));
        bill.Splits.Should().AllSatisfy(s => s.Amount.Should().Be(30m));
    }

    public void Dispose() => _factory.Dispose();
}
