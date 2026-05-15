using Blazored.LocalStorage;

namespace FrontalierApp.Services;

public class LocalStorageAdapter(ILocalStorageService localStorage) : IStorageService
{
    public async Task<T?> GetItemAsync<T>(string key) => await localStorage.GetItemAsync<T>(key);
    public Task SetItemAsync<T>(string key, T value) => localStorage.SetItemAsync(key, value).AsTask();
    public Task RemoveItemAsync(string key)          => localStorage.RemoveItemAsync(key).AsTask();
}
