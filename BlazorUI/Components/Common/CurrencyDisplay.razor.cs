using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Common;

public partial class CurrencyDisplay
{
    [Parameter, EditorRequired]
    public decimal Amount { get; set; }

    [Parameter]
    public string Currency { get; set; } = "$";

    [Parameter]
    public bool ColorCoded { get; set; }

    [Parameter]
    public bool ShowSign { get; set; }

    string FormattedAmount
    {
        get
        {
            var sign = ShowSign && Amount > 0 ? "+" : string.Empty;
            return $"{sign}{Currency}{Amount:N2}";
        }
    }

    string CssClass
    {
        get
        {
            if (!ColorCoded) return string.Empty;
            return Amount switch
            {
                > 0 => "rz-color-success",
                < 0 => "rz-color-danger",
                _ => "rz-color-secondary"
            };
        }
    }
}
