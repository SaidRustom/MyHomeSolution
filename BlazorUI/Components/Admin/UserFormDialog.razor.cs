using BlazorUI.Models.Users;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorUI.Components.Admin;

public partial class UserFormDialog
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    [Parameter]
    public CreateUserFormModel Model { get; set; } = new();

    [Parameter]
    public bool IsBusy { get; set; }

    void OnSubmit() => DialogService.Close(Model);

    void Cancel() => DialogService.Close(null);
}
