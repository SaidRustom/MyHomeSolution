using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Tasks;

namespace BlazorUI.Services.Contracts;

public interface ITaskService
{
    Task<ApiResult<PaginatedList<TaskBriefDto>>> GetTasksAsync(
        int pageNumber = 1,
        int pageSize = 20,
        TaskCategory? category = null,
        TaskPriority? priority = null,
        bool? isRecurring = null,
        string? assignedToUserId = null,
        string? searchTerm = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool? notCompletedOnly = null,
        bool? assignedByMe = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<IReadOnlyCollection<TodayTaskDto>>> GetTodayTasksAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<TaskDetailDto>> GetTaskByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<ApiResult<Guid>> CreateTaskAsync(
        CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateTaskAsync(
        Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteTaskAsync(
        Guid id, CancellationToken cancellationToken = default);
}
