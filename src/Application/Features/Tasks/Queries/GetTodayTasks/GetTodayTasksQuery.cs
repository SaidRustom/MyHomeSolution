using MediatR;
using MyHomeSolution.Application.Features.Tasks.Common;

namespace MyHomeSolution.Application.Features.Tasks.Queries.GetTodayTasks;

public sealed record GetTodayTasksQuery : IRequest<IReadOnlyCollection<TodayTaskDto>>;
