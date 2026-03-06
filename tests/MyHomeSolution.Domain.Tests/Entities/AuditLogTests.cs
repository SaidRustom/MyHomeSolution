using FluentAssertions;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Tests.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var auditLog = new AuditLog(
            "HouseholdTask",
            "some-entity-id",
            "user-123",
            AuditActionType.Create,
            timestamp);

        auditLog.EntityName.Should().Be("HouseholdTask");
        auditLog.EntityId.Should().Be("some-entity-id");
        auditLog.UserId.Should().Be("user-123");
        auditLog.ActionType.Should().Be(AuditActionType.Create);
        auditLog.Timestamp.Should().Be(timestamp);
        auditLog.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldAllowNullUserId()
    {
        var auditLog = new AuditLog(
            "HouseholdTask",
            "some-entity-id",
            null,
            AuditActionType.Update,
            DateTimeOffset.UtcNow);

        auditLog.UserId.Should().BeNull();
    }

    [Fact]
    public void HistoryEntries_ShouldBeEmptyByDefault()
    {
        var auditLog = new AuditLog();

        auditLog.HistoryEntries.Should().BeEmpty();
    }

    [Fact]
    public void AuditHistoryEntry_Constructor_ShouldSetAuditLogId()
    {
        var auditLog = new AuditLog(
            "TaskOccurrence",
            "entity-id",
            "user-1",
            AuditActionType.Update,
            DateTimeOffset.UtcNow);

        var entry = new AuditHistoryEntry(auditLog);

        entry.AuditLogId.Should().Be(auditLog.Id);
        entry.Id.Should().NotBeEmpty();
    }
}
