using MediatR;
using MyHomeSolution.Application.Common.Authorization;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand : IRequest, IRequireEditAccess
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; }
    public DateOnly? DueDate { get; init; }
    public string? AssignedToUserId { get; init; }

    public string ResourceType => EntityTypes.HouseholdTask;
    public Guid ResourceId => Id;
}
