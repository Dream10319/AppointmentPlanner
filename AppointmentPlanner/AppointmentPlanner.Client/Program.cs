global using Blazored.LocalStorage;
global using Microsoft.AspNetCore.Components.Authorization;
using AppointmentPlanner.Client.Services;
using AppointmentPlanner.Shared.Models;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();
builder.Services.AddSingleton<AppointmentService, AppointmentService>();
builder.Services.AddSingleton<Appointment, Appointment>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();
