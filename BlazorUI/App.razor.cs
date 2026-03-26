
using BlazorUI.Infrastructure.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;

namespace BlazorUI
{
    public partial class App
    {
        [Inject]
        private IStorageManager Storage { get; set; }

        [Inject]
        private ThemeService ThemeService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var theme = await Storage.GetAsync<string>("theme");

            if (theme != null)
                ThemeService.SetTheme(theme, false);
            else
                await Storage.SetAsync("theme", "software", TimeSpan.FromDays(30));
        }
    }
}
