using BlazorUI.Models.AuditLogs;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using BlazorUI.Models.Common;
using Radzen;

namespace BlazorUI.Components.Common;

public partial class EntityHistoryDialog
{
    [Inject]
    IAuditService AuditService { get; set; } = default!;

    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public required string EntityName { get; set; }

    [Parameter]
    public required string EntityId { get; set; }

    [Parameter]
    public string? EntityTitle { get; set; }

    bool IsLoading { get; set; }
    string? Error { get; set; }
    IReadOnlyList<AuditLogDto> History { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadHistoryAsync();
    }

    async Task LoadHistoryAsync()
    {
        IsLoading = true;
        Error = null;

        var result = await AuditService.GetEntityHistoryAsync(EntityName, EntityId);

        if (result.IsSuccess)
        {
            History = result.Value;
        }
        else
        {
            Error = result.Problem.ToUserMessage();
        }

        IsLoading = false;
    }

    void Close()
    {
        DialogService.Close();
    }

    static string GetActionIcon(AuditActionType actionType) => actionType switch
    {
        AuditActionType.Create => "add",
        AuditActionType.Update => "edit",
        AuditActionType.Delete => "delete",
        _ => "info"
    };

    static string GetActionLabel(AuditActionType actionType) => actionType switch
    {
        AuditActionType.Create => "created this entity",
        AuditActionType.Update => "made changes",
        AuditActionType.Delete => "deleted this entity",
        _ => "performed an action"
    };

    static string GetDotClass(AuditActionType actionType) => actionType switch
    {
        AuditActionType.Create => "dot-create",
        AuditActionType.Update => "dot-update",
        AuditActionType.Delete => "dot-delete",
        _ => "dot-update"
    };

    static string FormatTimestamp(DateTimeOffset timestamp)
    {
        var local = timestamp.LocalDateTime;
        var diff = DateTime.Now - local;

        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";

        return local.ToString("MMM dd, yyyy h:mm tt");
    }

    static string FormatPropertyName(string propertyName)
    {
        // Convert PascalCase to Title Case with spaces
        var result = System.Text.RegularExpressions.Regex.Replace(
            propertyName, "([a-z])([A-Z])", "$1 $2");

        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    static string TruncateValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        return value.Length > 100 ? value[..97] + "…" : value;
    }
}
