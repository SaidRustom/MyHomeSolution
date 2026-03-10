using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Settings;

public partial class DeleteAccountConfirmDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    private string _confirmText = string.Empty;
    private bool _showError;

    private bool IsConfirmValid =>
        string.Equals(_confirmText.Trim(), "DELETE", StringComparison.Ordinal);

    private void OnConfirmTextChanged(ChangeEventArgs e)
    {
        _confirmText = e.Value?.ToString() ?? string.Empty;
        _showError = false;
    }

    private void Cancel() => DialogService.Close(false);

    private void Confirm()
    {
        if (!IsConfirmValid)
        {
            _showError = true;
            return;
        }

        DialogService.Close(true);
    }
}
