using BlazorUI.Models.Common;
using BlazorUI.Models.Enums;
using BlazorUI.Models.Tasks;
using BlazorUI.Services.Contracts;
using BlazorUI.Services.Infrastructure;

namespace BlazorUI.Services;

public sealed class TaskService(HttpClient httpClient)
    : ApiServiceBase(httpClient), ITaskService
{
    private const string BasePath = "api/tasks";

    public Task<ApiResult<PaginatedList<TaskBriefDto>>> GetTasksAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueryString(
            ("pageNumber", pageNumber.ToString()),
            ("pageSize", pageSize.ToString()),
            ("category", category?.ToString()),
            ("priority", priority?.ToString()),
            ("isRecurring", isRecurring?.ToString()),
            ("assignedToUserId", assignedToUserId),
            ("searchTerm", searchTerm),
            ("fromDate", fromDate?.ToString("O")),
            ("toDate", toDate?.ToString("O")),
            ("notCompletedOnly", notCompletedOnly?.ToString()));

        return GetAsync<PaginatedList<TaskBriefDto>>($"{BasePath}{query}", cancellationToken);
    }

    public Task<ApiResult<IReadOnlyCollection<TodayTaskDto>>> GetTodayTasksAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IReadOnlyCollection<TodayTaskDto>>($"{BasePath}/today", cancellationToken);
    }

    public Task<ApiResult<TaskDetailDto>> GetTaskByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<TaskDetailDto>($"{BasePath}/{id}", cancellationToken);
    }

    public Task<ApiResult<Guid>> CreateTaskAsync(
        CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<Guid>(BasePath, request, cancellationToken);
    }

    public Task<ApiResult> UpdateTaskAsync(
        Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        return PutAsync($"{BasePath}/{id}", request, cancellationToken);
    }

    public Task<ApiResult> DeleteTaskAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync($"{BasePath}/{id}", cancellationToken);
    }
}
