
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontalierApp.Services;

public class AuthSession
{
    [JsonPropertyName("access_token")]  public string  AccessToken  { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string  RefreshToken { get; set; } = "";
    [JsonPropertyName("user")]          public AuthUser? User        { get; set; }
}

public class AuthUser
{
    [JsonPropertyName("id")]    public string Id    { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
}

public class AuthService(HttpClient http, IStorageService localStorage)
{
    private const string SessionKey = "supabase_session";

    public AuthSession? Session   { get; private set; }
    public bool IsAuthenticated   => !string.IsNullOrEmpty(Session?.AccessToken);
    public string? UserId         => Session?.User?.Id;
    public string? UserEmail      => Session?.User?.Email;
    public event Action? Changed;

    public async Task InitAsync()
    {
        Session = await localStorage.GetItemAsync<AuthSession>(SessionKey);
    }

    public async Task<string?> SignInAsync(string email, string password)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{SupabaseConfig.Url}/auth/v1/token?grant_type=password");
            req.Headers.Add("apikey", SupabaseConfig.AnonKey);
            req.Content = JsonContent.Create(new { email, password });

            var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return await resp.Content.ReadAsStringAsync();

            Session = await resp.Content.ReadFromJsonAsync<AuthSession>();
            await localStorage.SetItemAsync(SessionKey, Session);
            Changed?.Invoke();
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    public async Task<bool> RefreshAsync()
    {
        if (string.IsNullOrEmpty(Session?.RefreshToken)) return false;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{SupabaseConfig.Url}/auth/v1/token?grant_type=refresh_token");
            req.Headers.Add("apikey", SupabaseConfig.AnonKey);
            req.Content = JsonContent.Create(new { refresh_token = Session.RefreshToken });

            var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) { await SignOutAsync(); return false; }

            Session = await resp.Content.ReadFromJsonAsync<AuthSession>();
            await localStorage.SetItemAsync(SessionKey, Session);
            Changed?.Invoke();
            return true;
        }
        catch { return false; }
    }

    public async Task SignOutAsync()
    {
        Session = null;
        await localStorage.RemoveItemAsync(SessionKey);
        Changed?.Invoke();
    }
}
