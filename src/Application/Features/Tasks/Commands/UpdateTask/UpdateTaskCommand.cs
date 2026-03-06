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
    public bool IsRecurring { get; init; }
    public RecurrenceType? RecurrenceType { get; init; }
    public int? Interval { get; init; }
    public DateOnly? RecurrenceStartDate { get; init; }
    public DateOnly? RecurrenceEndDate { get; init; }
    public List<string>? AssigneeUserIds { get; init; }
    public bool AutoCreateBill { get; init; }
    public decimal? DefaultBillAmount { get; init; }
    public string? DefaultBillCurrency { get; init; }
    public BillCategory? DefaultBillCategory { get; init; }
    public string? DefaultBillTitle { get; init; }

    public string ResourceType => EntityTypes.HouseholdTask;
    public Guid ResourceId => Id;
}
