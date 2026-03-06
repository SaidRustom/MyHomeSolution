using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin;

public partial class AssignRoleDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public string UserName { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<string> ExistingRoles { get; set; } = [];

    string? SelectedRole { get; set; }

    IEnumerable<string> AvailableRoles =>
        AllRoles.Where(r => !ExistingRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

    static readonly string[] AllRoles = ["Administrator", "User"];

    void Confirm() => DialogService.Close(SelectedRole);

    void Cancel() => DialogService.Close(null);
}
