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

public sealed class BillCreatedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public BillCreatedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyAllSplitParticipantsExceptPayer()
    {
        var bill = await SeedBillWithSplits();

        using var context = _factory.CreateContext();
        var handler = new BillCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Should().AllSatisfy(n =>
        {
            n.Type.Should().Be(NotificationType.BillCreated);
            n.FromUserId.Should().Be("user-1");
            n.Title.Should().Contain("New bill");
        });
        notifications.Select(n => n.ToUserId).Should().Contain("user-2");
        notifications.Select(n => n.ToUserId).Should().Contain("user-3");
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        var bill = await SeedBillWithSplits();

        using var context = _factory.CreateContext();
        var handler = new BillCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, "user-1"),
            CancellationToken.None);

        await _realtimeService.Received(2).SendUserNotificationAsync(
            Arg.Any<string>(),
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("New bill")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new BillCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillCreatedEvent(Guid.CreateVersion7(), "Missing", 100m, "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldIncludeAmountInDescription()
    {
        var bill = await SeedBillWithSplits();

        using var context = _factory.CreateContext();
        var handler = new BillCreatedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstAsync();
        notification.Description.Should().NotBeNullOrEmpty();
    }

    private async Task<Bill> SeedBillWithSplits()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Dinner",
            Amount = 90m,
            Currency = "USD",
            Category = BillCategory.General,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1",
            CreatedBy = "user-1"
        };
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-1",
            Percentage = 33.33m, Amount = 30m, Status = SplitStatus.Paid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-2",
            Percentage = 33.33m, Amount = 30m, Status = SplitStatus.Unpaid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-3",
            Percentage = 33.33m, Amount = 30m, Status = SplitStatus.Unpaid
        });
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
        return bill;
    }

    public void Dispose() => _factory.Dispose();
}
