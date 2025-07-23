using Microsoft.JSInterop;
using System.Text.Json;

namespace AppointmentPlanner.Client.Services;

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (InvalidOperationException)
        {
            // This occurs during server-side rendering when JavaScript interop is not available
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, _jsonOptions));
        }
        catch (InvalidOperationException)
        {
            // This occurs during server-side rendering when JavaScript interop is not available
            // Silently ignore the operation
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (InvalidOperationException)
        {
            // This occurs during server-side rendering when JavaScript interop is not available
            // Silently ignore the operation
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (InvalidOperationException)
        {
            // This occurs during server-side rendering when JavaScript interop is not available
            // Silently ignore the operation
        }
    }
}
