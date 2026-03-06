using FluentAssertions;
using MyHomeSolution.Infrastructure.Hubs;

namespace MyHomeSolution.Infrastructure.Tests.Hubs;

public sealed class NotificationHubTests
{
    [Fact]
    public void FormatGroupName_ShouldReturnExpectedFormat()
    {
        var userId = "user-abc-123";

        var result = NotificationHub.FormatGroupName(userId);

        result.Should().Be("user-user-abc-123");
    }

    [Fact]
    public void FormatGroupName_ShouldProduceDifferentNames_ForDifferentUsers()
    {
        var group1 = NotificationHub.FormatGroupName("user-1");
        var group2 = NotificationHub.FormatGroupName("user-2");

        group1.Should().NotBe(group2);
    }

    [Fact]
    public void FormatGroupName_ShouldBeConsistentForSameUserId()
    {
        var userId = "consistent-user";

        var result1 = NotificationHub.FormatGroupName(userId);
        var result2 = NotificationHub.FormatGroupName(userId);

        result1.Should().Be(result2);
    }
}
