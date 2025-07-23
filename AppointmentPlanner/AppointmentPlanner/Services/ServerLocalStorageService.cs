using AppointmentPlanner.Client.Services;

namespace AppointmentPlanner.Services;

/// <summary>
/// Server-side implementation of ILocalStorageService that returns default values
/// This is used during server-side pre-rendering when JavaScript interop is not available
/// </summary>
public class ServerLocalStorageService : ILocalStorageService
{
    public Task<T?> GetItemAsync<T>(string key)
    {
        // During server-side rendering, we don't have access to localStorage
        // Return default value to prevent JavaScript interop errors
        return Task.FromResult(default(T));
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        // During server-side rendering, we can't set localStorage
        // Return completed task to prevent errors
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        // During server-side rendering, we can't remove from localStorage
        // Return completed task to prevent errors
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        // During server-side rendering, we can't clear localStorage
        // Return completed task to prevent errors
        return Task.CompletedTask;
    }
}
