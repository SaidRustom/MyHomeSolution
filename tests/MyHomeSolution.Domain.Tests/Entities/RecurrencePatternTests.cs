using FluentAssertions;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Tests.Entities;

public sealed class RecurrencePatternTests
{
    [Theory]
    [InlineData(RecurrenceType.Daily, 1, "2025-01-15", "2025-01-16")]
    [InlineData(RecurrenceType.Daily, 3, "2025-01-15", "2025-01-18")]
    [InlineData(RecurrenceType.Weekly, 1, "2025-01-15", "2025-01-22")]
    [InlineData(RecurrenceType.Weekly, 2, "2025-01-15", "2025-01-29")]
    [InlineData(RecurrenceType.Monthly, 1, "2025-01-15", "2025-02-15")]
    [InlineData(RecurrenceType.Monthly, 3, "2025-01-15", "2025-04-15")]
    [InlineData(RecurrenceType.Yearly, 1, "2025-01-15", "2026-01-15")]
    [InlineData(RecurrenceType.Yearly, 2, "2025-01-15", "2027-01-15")]
    public void GetNextOccurrenceDate_ShouldReturnCorrectDate(
        RecurrenceType type, int interval, string fromDateStr, string expectedDateStr)
    {
        var fromDate = DateOnly.Parse(fromDateStr);
        var expectedDate = DateOnly.Parse(expectedDateStr);
        var pattern = CreatePattern(type, interval);

        var result = pattern.GetNextOccurrenceDate(fromDate);

        result.Should().Be(expectedDate);
    }

    [Fact]
    public void GetNextOccurrenceDate_ShouldThrow_WhenRecurrenceTypeIsUnknown()
    {
        var pattern = CreatePattern((RecurrenceType)999, 1);

        var act = () => pattern.GetNextOccurrenceDate(new DateOnly(2025, 1, 15));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown recurrence type*");
    }

    [Fact]
    public void GetNextAssigneeUserId_ShouldReturnNull_WhenNoAssignees()
    {
        var pattern = CreatePattern(RecurrenceType.Daily, 1);

        var result = pattern.GetNextAssigneeUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void GetNextAssigneeUserId_ShouldReturnFirstAssignee_WhenIndexIsMinusOne()
    {
        var pattern = CreatePatternWithAssignees("user-a", "user-b", "user-c");

        var result = pattern.GetNextAssigneeUserId();

        result.Should().Be("user-a");
    }

    [Fact]
    public void GetNextAssigneeUserId_ShouldRotateThroughAssignees()
    {
        var pattern = CreatePatternWithAssignees("user-a", "user-b", "user-c");

        pattern.GetNextAssigneeUserId().Should().Be("user-a");
        pattern.AdvanceAssigneeIndex();

        pattern.GetNextAssigneeUserId().Should().Be("user-b");
        pattern.AdvanceAssigneeIndex();

        pattern.GetNextAssigneeUserId().Should().Be("user-c");
        pattern.AdvanceAssigneeIndex();

        // Should wrap around
        pattern.GetNextAssigneeUserId().Should().Be("user-a");
    }

    [Fact]
    public void GetNextAssigneeUserId_ShouldRespectAssigneeOrder()
    {
        var pattern = CreatePattern(RecurrenceType.Daily, 1);
        pattern.Assignees =
        [
            new RecurrenceAssignee { UserId = "user-z", Order = 2 },
            new RecurrenceAssignee { UserId = "user-a", Order = 0 },
            new RecurrenceAssignee { UserId = "user-m", Order = 1 }
        ];

        var result = pattern.GetNextAssigneeUserId();

        result.Should().Be("user-a");
    }

    [Fact]
    public void AdvanceAssigneeIndex_ShouldNotThrow_WhenNoAssignees()
    {
        var pattern = CreatePattern(RecurrenceType.Daily, 1);

        var act = () => pattern.AdvanceAssigneeIndex();

        act.Should().NotThrow();
        pattern.LastAssigneeIndex.Should().Be(-1);
    }

    [Fact]
    public void AdvanceAssigneeIndex_ShouldWrapAround()
    {
        var pattern = CreatePatternWithAssignees("user-a", "user-b");

        pattern.AdvanceAssigneeIndex(); // 0
        pattern.AdvanceAssigneeIndex(); // 1
        pattern.AdvanceAssigneeIndex(); // wraps to 0

        pattern.LastAssigneeIndex.Should().Be(0);
    }

    [Fact]
    public void AdvanceAssigneeIndex_ShouldIncrementCorrectly()
    {
        var pattern = CreatePatternWithAssignees("user-a", "user-b", "user-c");

        pattern.AdvanceAssigneeIndex();
        pattern.LastAssigneeIndex.Should().Be(0);

        pattern.AdvanceAssigneeIndex();
        pattern.LastAssigneeIndex.Should().Be(1);

        pattern.AdvanceAssigneeIndex();
        pattern.LastAssigneeIndex.Should().Be(2);
    }

    private static RecurrencePattern CreatePattern(RecurrenceType type, int interval) =>
        new()
        {
            Type = type,
            Interval = interval,
            StartDate = new DateOnly(2025, 1, 1)
        };

    private static RecurrencePattern CreatePatternWithAssignees(params string[] userIds)
    {
        var pattern = CreatePattern(RecurrenceType.Daily, 1);
        pattern.Assignees = userIds
            .Select((uid, i) => new RecurrenceAssignee
            {
                RecurrencePatternId = pattern.Id,
                UserId = uid,
                Order = i
            })
            .ToList();
        return pattern;
    }
}
