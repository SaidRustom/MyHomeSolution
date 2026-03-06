using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class BillSplitPaidNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public BillSplitPaidNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyBillPayer_WhenOtherUserPays()
    {
        var (bill, split) = await SeedBillWithSplit();

        using var context = _factory.CreateContext();
        var handler = new BillSplitPaidNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillSplitPaidEvent(bill.Id, split.Id, bill.Title, "user-2", 50m),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync();
        notification.Should().NotBeNull();
        notification!.ToUserId.Should().Be("user-1");
        notification.FromUserId.Should().Be("user-2");
        notification.Type.Should().Be(NotificationType.BillSplitPaid);
        notification.Title.Should().Contain("Payment received");
    }

    [Fact]
    public async Task Handle_ShouldNotNotify_WhenPayerPaysOwnSplit()
    {
        var (bill, _) = await SeedBillWithSplit();

        using var context = _factory.CreateContext();
        var handler = new BillSplitPaidNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillSplitPaidEvent(bill.Id, Guid.CreateVersion7(), bill.Title, "user-1", 50m),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        var (bill, split) = await SeedBillWithSplit();

        using var context = _factory.CreateContext();
        var handler = new BillSplitPaidNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillSplitPaidEvent(bill.Id, split.Id, bill.Title, "user-2", 50m),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "user-1",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("Payment received")),
            Arg.Any<CancellationToken>());
    }

    private async Task<(Bill Bill, BillSplit Split)> SeedBillWithSplit()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Groceries",
            Amount = 100m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        var split = new BillSplit
        {
            BillId = bill.Id, UserId = "user-2",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Unpaid
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
