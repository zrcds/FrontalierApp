using FrontalierApp.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontalierApp.Services;

public class SupabaseStorageService(HttpClient http, AuthService auth)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private void Auth(HttpRequestMessage req)
    {
        req.Headers.Add("apikey", SupabaseConfig.AnonKey);
        if (auth.Session != null)
            req.Headers.Add("Authorization", $"Bearer {auth.Session.AccessToken}");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req)
    {
        var resp = await http.SendAsync(req);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && await auth.RefreshAsync())
        {
            // Rebuild request (HttpRequestMessage can't be reused)
            var retry = Clone(req);
            Auth(retry);
            resp = await http.SendAsync(retry);
        }
        return resp;
    }

    private static HttpRequestMessage Clone(HttpRequestMessage req) => new(req.Method, req.RequestUri);

    public async Task<List<WorkDay>> FetchAllAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"{SupabaseConfig.Url}/rest/v1/workdays?select=id,date,type,is_half_day,note&order=date");
        Auth(req);
        var resp = await SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var rows = await resp.Content.ReadFromJsonAsync<List<SupabaseRow>>(JsonOpts) ?? [];
        return rows.Select(ToWorkDay).ToList();
    }

    public async Task UpsertAsync(WorkDay day)
    {
        var req = new HttpRequestMessage(HttpMethod.Post,
            $"{SupabaseConfig.Url}/rest/v1/workdays");
        Auth(req);
        req.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = JsonContent.Create(FromWorkDay(day, auth.UserId!));
        var resp = await SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpsertRangeAsync(IEnumerable<WorkDay> days)
    {
        var req = new HttpRequestMessage(HttpMethod.Post,
            $"{SupabaseConfig.Url}/rest/v1/workdays");
        Auth(req);
        req.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = JsonContent.Create(days.Select(d => FromWorkDay(d, auth.UserId!)).ToList());
        var resp = await SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete,
            $"{SupabaseConfig.Url}/rest/v1/workdays?id=eq.{id}");
        Auth(req);
        var resp = await SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    private static WorkDay ToWorkDay(SupabaseRow r) => new()
    {
        Id        = Guid.Parse(r.Id),
        Date      = DateOnly.ParseExact(r.Date, "yyyy-MM-dd", null),
        Type      = (DayType)r.Type,
        IsHalfDay = r.IsHalfDay,
        Note      = r.Note
    };

    private static SupabaseRow FromWorkDay(WorkDay d, string userId) => new(
        d.Id.ToString(), userId,
        d.Date.ToString("yyyy-MM-dd"),
        (int)d.Type, d.IsHalfDay, d.Note);
}

internal record SupabaseRow(
    [property: JsonPropertyName("id")]          string  Id,
    [property: JsonPropertyName("user_id")]     string  UserId,
    [property: JsonPropertyName("date")]        string  Date,
    [property: JsonPropertyName("type")]        int     Type,
    [property: JsonPropertyName("is_half_day")] bool    IsHalfDay,
    [property: JsonPropertyName("note")]        string? Note);
