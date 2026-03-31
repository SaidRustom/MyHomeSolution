using BlazorUI.Models.Dashboard;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Dashboard;

public partial class HomepageCustomizeDialog
{
    [Inject] DialogService DialogService { get; set; } = default!;
    [Inject] IDashboardService DashboardService { get; set; } = default!;
    [Inject] NotificationService Notifications { get; set; } = default!;

    [Parameter] public List<EditableWidget> Widgets { get; set; } = [];

    List<EditableWidget> _widgets = [];
    bool _isSaving;

    List<WidgetDefinition> AvailableToAdd =>
        WidgetTypes.All
            .Where(d => !_widgets.Any(w => w.WidgetType == d.Type))
            .ToList();

    protected override void OnInitialized()
    {
        _widgets = Widgets.Select(w => new EditableWidget
        {
            WidgetType = w.WidgetType,
            ColumnSpan = w.ColumnSpan,
            IsVisible = w.IsVisible,
            Settings = w.Settings
        }).ToList();
    }

    void MoveUp(int index)
    {
        if (index <= 0) return;
        (_widgets[index], _widgets[index - 1]) = (_widgets[index - 1], _widgets[index]);
    }

    void MoveDown(int index)
    {
        if (index >= _widgets.Count - 1) return;
        (_widgets[index], _widgets[index + 1]) = (_widgets[index + 1], _widgets[index]);
    }

    void RemoveWidget(int index) => _widgets.RemoveAt(index);

    void AddWidget(WidgetDefinition def)
    {
        _widgets.Add(new EditableWidget
        {
            WidgetType = def.Type,
            ColumnSpan = def.DefaultColumnSpan,
            IsVisible = true
        });
    }

    async Task Save()
    {
        _isSaving = true;

        var request = new SaveHomepageLayoutRequest
        {
            Widgets = _widgets.Select((w, i) => new SaveWidgetRequest
            {
                WidgetType = w.WidgetType,
                Position = i,
                ColumnSpan = w.ColumnSpan,
                IsVisible = w.IsVisible,
                Settings = w.Settings
            }).ToList()
        };

        var result = await DashboardService.SaveHomepageLayoutAsync(request);

        if (result.IsSuccess)
        {
            Notifications.Notify(NotificationSeverity.Success, "Layout saved", duration: 2000);
            DialogService.Close(result.Value);
        }
        else
        {
            Notifications.Notify(NotificationSeverity.Error, "Failed to save layout", duration: 3000);
        }

        _isSaving = false;
    }
}

public sealed class EditableWidget
{
    public required string WidgetType { get; set; }
    public int ColumnSpan { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
    public string? Settings { get; set; }
}
