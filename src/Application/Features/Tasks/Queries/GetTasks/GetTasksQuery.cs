using MediatR;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Features.Tasks.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTasks;

public sealed record GetTasksQuery : IRequest<PaginatedList<TaskBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public TaskCategory? Category { get; init; }
    public TaskPriority? Priority { get; init; }
    public bool? IsRecurring { get; init; }
    public string? AssignedToUserId { get; init; }
    public string? SearchTerm { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool? NotCompletedOnly { get; init; }
}
