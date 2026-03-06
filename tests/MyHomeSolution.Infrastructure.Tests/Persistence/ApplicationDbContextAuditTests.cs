using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using MyHomeSolution.Infrastructure.Tests.Testing;
using NSubstitute;

namespace MyHomeSolution.Infrastructure.Tests.Persistence;

public sealed class ApplicationDbContextAuditTests : IDisposable
{
    private readonly InfrastructureTestDbContextFactory _factory = new();

    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedAt_WhenEntityIsAdded()
    {
        var fixedTime = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(fixedTime);

        using var context = _factory.CreateContext(dateTimeProvider: dateTimeProvider);
        var task = new HouseholdTask
        {
            Title = "Audit test",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        context.HouseholdTasks.Add(task);

        await context.SaveChangesAsync();

        using var assertContext = _factory.CreateContext();
        var saved = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        saved.CreatedAt.Should().Be(fixedTime);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetCreatedBy_WhenEntityIsAdded()
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns("audit-user-42");

        using var context = _factory.CreateContext(currentUserService: currentUserService);
        var task = new HouseholdTask
        {
            Title = "Created by test",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        context.HouseholdTasks.Add(task);

        await context.SaveChangesAsync();

        using var assertContext = _factory.CreateContext();
        var saved = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        saved.CreatedBy.Should().Be("audit-user-42");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetLastModifiedFields_WhenEntityIsModified()
    {
        // Arrange: seed a task
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Original",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        // Act: modify with specific user/time
        var modifiedTime = new DateTimeOffset(2025, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(modifiedTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns("modifier-user");

        using var updateContext = _factory.CreateContext(currentUserService, dateTimeProvider);
        var tracked = await updateContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        tracked.Title = "Modified";
        await updateContext.SaveChangesAsync();

        // Assert
        using var assertContext = _factory.CreateContext();
        var saved = await assertContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        saved.LastModifiedAt.Should().Be(modifiedTime);
        saved.LastModifiedBy.Should().Be("modifier-user");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityIsAdded()
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Audit log test",
            Priority = TaskPriority.Medium,
            Category = TaskCategory.Cleaning
        };
        context.HouseholdTasks.Add(task);

        await context.SaveChangesAsync();

        using var assertContext = _factory.CreateContext();
        var auditLog = await assertContext.AuditLogs
            .FirstOrDefaultAsync(a =>
                a.EntityId == task.Id.ToString()
                && a.ActionType == AuditActionType.Create);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateAuditLogWithHistory_WhenEntityIsModified()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Before",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        // Act
        using var updateContext = _factory.CreateContext();
        var tracked = await updateContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        tracked.Title = "After";
        await updateContext.SaveChangesAsync();

        // Assert
        using var assertContext = _factory.CreateContext();
        var updateAuditLog = await assertContext.AuditLogs
            .Include(a => a.HistoryEntries)
            .FirstOrDefaultAsync(a =>
                a.EntityId == task.Id.ToString()
                && a.ActionType == AuditActionType.Update);

        updateAuditLog.Should().NotBeNull();
        updateAuditLog!.HistoryEntries.Should().Contain(h =>
            h.PropertyName == "Title"
            && h.OldValue == "Before"
            && h.NewValue == "After");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldHandleSoftDelete_WhenEntityIsDeleted()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "To delete",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        var deleteTime = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(deleteTime);
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns("deleting-user");

        // Act
        using var deleteContext = _factory.CreateContext(currentUserService, dateTimeProvider);
        var tracked = await deleteContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        deleteContext.HouseholdTasks.Remove(tracked);
        await deleteContext.SaveChangesAsync();

        // Assert: entity should be soft-deleted, not physically removed
        using var assertContext = _factory.CreateContext();
        var deleted = await assertContext.HouseholdTasks
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == task.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().Be(deleteTime);
        deleted.DeletedBy.Should().Be("deleting-user");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateDeleteAuditLog()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Delete audit test",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        // Act
        using var deleteContext = _factory.CreateContext();
        var tracked = await deleteContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        deleteContext.HouseholdTasks.Remove(tracked);
        await deleteContext.SaveChangesAsync();

        // Assert
        using var assertContext = _factory.CreateContext();
        var deleteLog = await assertContext.AuditLogs
            .FirstOrDefaultAsync(a =>
                a.EntityId == task.Id.ToString()
                && a.ActionType == AuditActionType.Delete);
        deleteLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotCreateAuditHistory_ForUnchangedProperties()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Unchanged",
            Description = "Same desc",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General
        };
        seedContext.HouseholdTasks.Add(task);
        await seedContext.SaveChangesAsync();

        // Act: modify only the title
        using var updateContext = _factory.CreateContext();
        var tracked = await updateContext.HouseholdTasks.FirstAsync(t => t.Id == task.Id);
        tracked.Title = "Changed";
        await updateContext.SaveChangesAsync();

        // Assert
        using var assertContext = _factory.CreateContext();
        var updateLog = await assertContext.AuditLogs
            .Include(a => a.HistoryEntries)
            .FirstOrDefaultAsync(a =>
                a.EntityId == task.Id.ToString()
                && a.ActionType == AuditActionType.Update);

        updateLog.Should().NotBeNull();
        updateLog!.HistoryEntries.Should().ContainSingle(h => h.PropertyName == "Title");
        updateLog.HistoryEntries.Should().NotContain(h => h.PropertyName == "Description");
    }

    public void Dispose() => _factory.Dispose();
}
