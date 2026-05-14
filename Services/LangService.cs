using Blazored.LocalStorage;

namespace FrontalierApp.Services;

public class LangService(ILocalStorageService localStorage)
{
    private const string LangKey = "app_lang";
    private string _lang = "en";
    private bool _initialized;

    public string Lang => _lang;
    public event Action? Changed;

    public async Task InitAsync()
    {
        if (_initialized) return;
        _lang = await localStorage.GetItemAsync<string>(LangKey) ?? "en";
        _initialized = true;
    }

    public async Task SetAsync(string lang)
    {
        _lang = lang;
        await localStorage.SetItemAsync(LangKey, lang);
        Changed?.Invoke();
    }

    public string T(string key) => Strings.Get(_lang, key);
    public string T(string key, params object[] args) => string.Format(T(key), args);
}
