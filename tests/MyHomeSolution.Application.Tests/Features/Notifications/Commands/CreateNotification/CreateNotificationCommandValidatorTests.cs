using FluentValidation.TestHelper;
using MyHomeSolution.Application.Features.Notifications.Commands.CreateNotification;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Tests.Features.Notifications.Commands.CreateNotification;

public sealed class CreateNotificationCommandValidatorTests
{
    private readonly CreateNotificationCommandValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenTitleIsEmpty()
    {
        var command = CreateValidCommand() with { Title = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void ShouldFail_WhenTitleExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Title = new string('A', 257) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 256 characters.");
    }

    [Fact]
    public void ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        var command = CreateValidCommand() with { Description = new string('A', 2001) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters.");
    }

    [Fact]
    public void ShouldPass_WhenDescriptionIsNull()
    {
        var command = CreateValidCommand() with { Description = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldFail_WhenToUserIdIsEmpty()
    {
        var command = CreateValidCommand() with { ToUserId = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ToUserId)
            .WithErrorMessage("Recipient user is required.");
    }

    [Fact]
    public void ShouldFail_WhenToUserIdExceedsMaxLength()
    {
        var command = CreateValidCommand() with { ToUserId = new string('A', 451) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ToUserId)
            .WithErrorMessage("ToUserId must not exceed 450 characters.");
    }

    [Fact]
    public void ShouldFail_WhenTypeIsInvalid()
    {
        var command = CreateValidCommand() with { Type = (NotificationType)999 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Invalid notification type.");
    }

    [Fact]
    public void ShouldFail_WhenRelatedEntityTypeExceedsMaxLength()
    {
        var command = CreateValidCommand() with { RelatedEntityType = new string('A', 257) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RelatedEntityType)
            .WithErrorMessage("Related entity type must not exceed 256 characters.");
    }

    [Fact]
    public void ShouldPass_WhenRelatedEntityTypeIsNull()
    {
        var command = CreateValidCommand() with { RelatedEntityType = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RelatedEntityType);
    }

    [Theory]
    [InlineData(NotificationType.General)]
    [InlineData(NotificationType.TaskAssigned)]
    [InlineData(NotificationType.TaskUpdated)]
    [InlineData(NotificationType.ShareReceived)]
    [InlineData(NotificationType.Mention)]
    public void ShouldPass_ForAllValidNotificationTypes(NotificationType type)
    {
        var command = CreateValidCommand() with { Type = type };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    private static CreateNotificationCommand CreateValidCommand() =>
        new()
        {
            Title = "Task assigned to you",
            Description = "You have been assigned the kitchen cleaning task.",
            Type = NotificationType.TaskAssigned,
            ToUserId = "recipient-user-id",
            RelatedEntityId = Guid.CreateVersion7(),
            RelatedEntityType = "HouseholdTask"
        };
}
