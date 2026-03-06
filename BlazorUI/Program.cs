using BlazorUI;
using BlazorUI.Infrastructure;
using BlazorUI.Infrastructure.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddRadzenComponents();

builder.Services.AddSingleton<IStorageManager, StorageManager>();

builder.Services.AddAuthServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddRealtimeServices();

builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "MyApplicationTheme";
    options.Duration = TimeSpan.FromDays(365);
});

await builder.Build().RunAsync();
