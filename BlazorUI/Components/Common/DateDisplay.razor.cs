using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class DateDisplay
{
    [Parameter, EditorRequired]
    public DateTimeOffset? Date { get; set; }

    [Parameter]
    public string Format { get; set; } = "MMM dd, yyyy";

    [Parameter]
    public bool ShowRelative { get; set; }

    [Parameter]
    public bool ShowTime { get; set; }

    string FormattedDate
    {
        get
        {
            if (Date is null) return "—";

            if (ShowRelative)
                return GetRelativeTime(Date.Value);

            var fmt = ShowTime ? $"{Format} h:mm tt" : Format;
            return Date.Value.LocalDateTime.ToString(fmt);
        }
    }

    static string GetRelativeTime(DateTimeOffset date)
    {
        var diff = DateTimeOffset.Now - date;

        return diff.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)diff.TotalMinutes}m ago",
            < 86400 => $"{(int)diff.TotalHours}h ago",
            < 604800 => $"{(int)diff.TotalDays}d ago",
            _ => date.LocalDateTime.ToString("MMM dd, yyyy")
        };
    }
}
