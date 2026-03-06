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

public sealed class BillReceiptAddedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public BillReceiptAddedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyParticipantsExceptUploader()
    {
        var bill = await SeedBill();

        using var context = _factory.CreateContext();
        var handler = new BillReceiptAddedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillReceiptAddedEvent(bill.Id, bill.Title, "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
        notifications.First().ToUserId.Should().Be("user-2");
        notifications.First().Type.Should().Be(NotificationType.BillReceiptAdded);
        notifications.First().Title.Should().Contain("Receipt added");
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenBillNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new BillReceiptAddedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new BillReceiptAddedEvent(Guid.CreateVersion7(), "Missing", "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    private async Task<Bill> SeedBill()
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
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-1",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Paid
        });
        bill.Splits.Add(new BillSplit
        {
            BillId = bill.Id, UserId = "user-2",
            Percentage = 50m, Amount = 50m, Status = SplitStatus.Unpaid
        });
        context.Bills.Add(bill);
        await context.SaveChangesAsync();
        return bill;
    }

    public void Dispose() => _factory.Dispose();
}
