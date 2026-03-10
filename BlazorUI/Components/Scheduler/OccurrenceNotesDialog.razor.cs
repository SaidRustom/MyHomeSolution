using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Scheduler;

public partial class OccurrenceNotesDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public string Notes { get; set; } = string.Empty;

    void Save() => DialogService.Close(Notes);

    void Cancel() => DialogService.Close(null);
}
