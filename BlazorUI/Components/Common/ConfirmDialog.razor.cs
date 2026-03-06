using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Common;

public partial class ConfirmDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public string Title { get; set; } = "Confirm";

    [Parameter]
    public string Message { get; set; } = "Are you sure?";

    [Parameter]
    public string ConfirmText { get; set; } = "Confirm";

    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    [Parameter]
    public ButtonStyle ConfirmStyle { get; set; } = ButtonStyle.Primary;

    [Parameter]
    public string ConfirmIcon { get; set; } = "check";

    [Parameter]
    public bool IsBusy { get; set; }

    void Cancel() => DialogService.Close(false);

    void Confirm() => DialogService.Close(true);
}
