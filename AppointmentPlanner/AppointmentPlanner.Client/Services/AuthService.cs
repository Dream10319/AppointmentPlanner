using AppointmentPlanner.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient http, ILocalStorageService localStorage, JwtAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> Login(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { Email = email, Password = password });
        if (!response.IsSuccessStatusCode)
            return false;

        var content = await response.Content.ReadFromJsonAsync<LoginResult>();
        await _localStorage.SetItemAsync("authToken", content.token);
        _authStateProvider.NotifyUserAuthentication(content.token);

        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", content.token);
        return true;
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _authStateProvider.NotifyUserLogout();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> Register(string email, string password, string role)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new { Email = email, Password = password, Role = role });
        return response.IsSuccessStatusCode;
    }
}

public class LoginResult { public string token { get; set; } }
