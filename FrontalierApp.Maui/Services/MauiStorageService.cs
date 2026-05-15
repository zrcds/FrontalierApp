using System.Text.Json;

namespace FrontalierApp.Services;

public class MauiStorageService : IStorageService
{
    public Task<T?> GetItemAsync<T>(string key)
    {
        var raw = Preferences.Default.Get<string?>(key, null);
        if (raw == null) return Task.FromResult<T?>(default);
        try { return Task.FromResult(JsonSerializer.Deserialize<T>(raw)); }
        catch { return Task.FromResult<T?>(default); }
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        Preferences.Default.Set(key, JsonSerializer.Serialize(value));
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        Preferences.Default.Remove(key);
        return Task.CompletedTask;
    }
}
