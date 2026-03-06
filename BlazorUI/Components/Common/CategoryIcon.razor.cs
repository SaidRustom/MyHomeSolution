using BlazorUI.Models.Enums;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class CategoryIcon
{
    [Parameter, EditorRequired]
    public string Category { get; set; } = string.Empty;

    [Parameter]
    public double Size { get; set; } = 24;

    string MaterialIcon => Category switch
    {
        nameof(BillCategory.Groceries) => "shopping_cart",
        nameof(BillCategory.Utilities) => "bolt",
        nameof(BillCategory.Rent) => "home",
        nameof(BillCategory.Maintenance) => "build",
        nameof(BillCategory.Supplies) => "inventory_2",
        nameof(BillCategory.Internet) => "wifi",
        nameof(BillCategory.Insurance) => "shield",
        nameof(BillCategory.Furniture) => "chair",
        nameof(BillCategory.Cleaning) => "cleaning_services",
        nameof(BillCategory.Other) => "more_horiz",
        nameof(BillCategory.General) => "receipt_long",
        // ShoppingList & Task categories can be added here in the future
        _ => "category"
    };

    string IconColor => Category switch
    {
        nameof(BillCategory.Groceries) => "#4CAF50",
        nameof(BillCategory.Utilities) => "#FF9800",
        nameof(BillCategory.Rent) => "#2196F3",
        nameof(BillCategory.Maintenance) => "#9C27B0",
        nameof(BillCategory.Supplies) => "#00BCD4",
        nameof(BillCategory.Internet) => "#3F51B5",
        nameof(BillCategory.Insurance) => "#607D8B",
        nameof(BillCategory.Furniture) => "#795548",
        nameof(BillCategory.Cleaning) => "#8BC34A",
        nameof(BillCategory.Other) => "#9E9E9E",
        nameof(BillCategory.General) => "#FF5722",
        _ => "var(--rz-text-color)"
    };
}
