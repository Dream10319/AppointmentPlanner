using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using AppointmentPlanner.Client.Models;

namespace AppointmentPlanner.Client.Services;

public interface IAuthenticationService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task LogoutAsync();
    Task<UserInfoDto?> GetCurrentUserAsync();
    Task<AuthResponseDto> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<AuthResponseDto> UpdateProfileAsync(UpdateProfileDto updateProfileDto);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthenticationService(
        HttpClient httpClient,
        AuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result?.IsSuccess == true && !string.IsNullOrEmpty(result.Token))
            {
                await _localStorage.SetItemAsync("authToken", result.Token);
                await _localStorage.SetItemAsync("user", result.User);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
            }

            return result ?? new AuthResponseDto { IsSuccess = false, Message = "Registration failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result?.IsSuccess == true && !string.IsNullOrEmpty(result.Token))
            {
                await _localStorage.SetItemAsync("authToken", result.Token);
                await _localStorage.SetItemAsync("user", result.User);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
            }

            return result ?? new AuthResponseDto { IsSuccess = false, Message = "Login failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during login",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", forgotPasswordDto);
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>() ??
                   new AuthResponseDto { IsSuccess = false, Message = "Password reset request failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during password reset request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", resetPasswordDto);
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>() ??
                   new AuthResponseDto { IsSuccess = false, Message = "Password reset failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during password reset",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("user");
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/user/profile");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthResponseDto> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "User not authenticated" };
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync("api/user/change-password", changePasswordDto);
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>() ??
                   new AuthResponseDto { IsSuccess = false, Message = "Password change failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during password change",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResponseDto> UpdateProfileAsync(UpdateProfileDto updateProfileDto)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return new AuthResponseDto { IsSuccess = false, Message = "User not authenticated" };
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PutAsJsonAsync("api/user/profile", updateProfileDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result?.IsSuccess == true && result.User != null)
            {
                await _localStorage.SetItemAsync("user", result.User);
            }

            return result ?? new AuthResponseDto { IsSuccess = false, Message = "Profile update failed" };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred during profile update",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
