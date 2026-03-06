using FluentAssertions;
using MyHomeSolution.Infrastructure.Hubs;

namespace MyHomeSolution.Infrastructure.Tests.Hubs;

public sealed class TaskHubTests
{
    [Fact]
    public void FormatGroupName_ShouldReturnExpectedFormat()
    {
        var taskId = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

        var result = TaskHub.FormatGroupName(taskId);

        result.Should().Be("task-01020304-0506-0708-090a-0b0c0d0e0f10");
    }

    [Fact]
    public void FormatGroupName_ShouldProduceDifferentNames_ForDifferentTasks()
    {
        var taskId1 = Guid.CreateVersion7();
        var taskId2 = Guid.CreateVersion7();

        var group1 = TaskHub.FormatGroupName(taskId1);
        var group2 = TaskHub.FormatGroupName(taskId2);

        group1.Should().NotBe(group2);
    }

    [Fact]
    public void FormatGroupName_ShouldBeConsistentForSameTaskId()
    {
        var taskId = Guid.CreateVersion7();

        var result1 = TaskHub.FormatGroupName(taskId);
        var result2 = TaskHub.FormatGroupName(taskId);

        result1.Should().Be(result2);
    }
}
