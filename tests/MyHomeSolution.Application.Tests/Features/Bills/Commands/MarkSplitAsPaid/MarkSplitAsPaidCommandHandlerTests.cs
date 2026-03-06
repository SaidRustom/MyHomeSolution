using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Commands.MarkSplitAsPaid;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.Bills.Commands.MarkSplitAsPaid;

public sealed class MarkSplitAsPaidCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public MarkSplitAsPaidCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-2");
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldMarkSplitAsPaid()
    {
        var (bill, split) = await SeedBillWithUnpaidSplit();

        using var context = _factory.CreateContext();
        var handler = new MarkSplitAsPaidCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(
            new MarkSplitAsPaidCommand(bill.Id, split.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.BillSplits.FirstAsync(s => s.Id == split.Id);
        updated.Status.Should().Be(SplitStatus.Paid);
        updated.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldPublishBillSplitPaidEvent()
    {
        var (bill, split) = await SeedBillWithUnpaidSplit();

        using var context = _factory.CreateContext();
        var handler = new MarkSplitAsPaidCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(
            new MarkSplitAsPaidCommand(bill.Id, split.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<BillSplitPaidEvent>(e =>
                e.BillId == bill.Id &&
                e.SplitId == split.Id &&
                e.PaidByUserId == "user-2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserIsNotSplitOwnerOrPayer()
    {
        _currentUserService.UserId.Returns("user-3");
        var (bill, split) = await SeedBillWithUnpaidSplit();

        using var context = _factory.CreateContext();
        var handler = new MarkSplitAsPaidCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(
            new MarkSplitAsPaidCommand(bill.Id, split.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowPayerToMarkSplitAsPaid()
    {
        _currentUserService.UserId.Returns("user-1"); // the payer
        var (bill, split) = await SeedBillWithUnpaidSplit();

        using var context = _factory.CreateContext();
        var handler = new MarkSplitAsPaidCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(
            new MarkSplitAsPaidCommand(bill.Id, split.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.BillSplits.FirstAsync(s => s.Id == split.Id);
        updated.Status.Should().Be(SplitStatus.Paid);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new MarkSplitAsPaidCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(
            new MarkSplitAsPaidCommand(Guid.CreateVersion7(), Guid.CreateVersion7()),
            CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<(Bill Bill, BillSplit Split)> SeedBillWithUnpaidSplit()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Test Bill",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
        };
        var split = new BillSplit
        {
            BillId = bill.Id,
            UserId = "user-2",
            Percentage = 50m,
            Amount = 50m,
            Status = SplitStatus.Unpaid
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-1",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        bill.Splits.Add(split);
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
        return (bill, split);
    }

    public void Dispose() => _factory.Dispose();
}
