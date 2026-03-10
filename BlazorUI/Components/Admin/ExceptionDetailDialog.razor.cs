using BlazorUI.Models.Common;
using BlazorUI.Models.Exceptions;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace BlazorUI.Components.Admin;

public partial class ExceptionDetailDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Inject]
    IJSRuntime Js { get; set; } = default!;

    [Inject]
    IExceptionLogService ExceptionLogService { get; set; } = default!;

    [Inject]
    NotificationService NotificationService { get; set; } = default!;

    [Parameter, EditorRequired]
    public ExceptionLogDetailDto Exception { get; set; } = default!;

    bool ShowStackTrace { get; set; }
    bool ShowInnerException { get; set; }
    bool IsAnalysing { get; set; }

    string SeverityBadgeStyle => $"background: color-mix(in srgb, {Exception.SeverityColor} 15%, transparent); color: {Exception.SeverityColor}; padding: 0.25rem 0.75rem; border-radius: 1rem; font-size: 0.75rem; font-weight: 600;";

    void Close() => DialogService.Close();

    async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    async Task AnalyseWithAiAsync()
    {
        IsAnalysing = true;

        try
        {
            var result = await ExceptionLogService.AnalyseExceptionAsync(Exception.Id);

            if (result.IsSuccess)
            {
                Exception = result.Value;

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Analysis Complete",
                    Detail = "AI analysis has been completed successfully.",
                    Duration = 4000
                });
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Analysis Failed",
                    Detail = result.Problem.ToUserMessage(),
                    Duration = 6000
                });
            }
        }
        finally
        {
            IsAnalysing = false;
        }
    }
}
