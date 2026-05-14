using Blazored.LocalStorage;
using FrontalierApp;
using FrontalierApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped(sp => new HttpClient());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SupabaseStorageService>();
builder.Services.AddScoped<TeleworkService>();
builder.Services.AddScoped<LangService>();

await builder.Build().RunAsync();
