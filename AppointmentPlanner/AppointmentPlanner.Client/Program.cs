using AppointmentPlanner.Data;
using AppointmentPlanner.Models;
using AppointmentPlanner.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add Syncfusion services
builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();

// Add authentication services
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add HTTP client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add application services
builder.Services.AddSingleton<AppointmentService, AppointmentService>();
builder.Services.AddSingleton<Appointment, Appointment>();

await builder.Build().RunAsync();
