
using FrontalierApp.Models;

namespace FrontalierApp.Services;

public record VacationPeriod(DateOnly Start, DateOnly End, int Days);
public record PublicHolidayInfo(DateOnly Date, string NameFr, string NameEn);

public class TeleworkStats
{
    public int Year { get; set; }
    public double TotalWorkedDays { get; set; }
    public double TeleworkSSDays { get; set; }
    public double TeleworkTaxDays { get; set; }
    public int MissionDays { get; set; }

    public double SocialSecurityPercent => TotalWorkedDays > 0 ? TeleworkSSDays / TotalWorkedDays * 100 : 0;
    public double TaxPercent => TotalWorkedDays > 0 ? (TeleworkTaxDays + MissionDays) / TotalWorkedDays * 100 : 0;
    public double TeleworkPlusMissionTaxDays => TeleworkTaxDays + MissionDays;

    public bool IsApproachingCompanySSLimit => SocialSecurityPercent is >= 18 and < 20;
    public bool IsOverCompanySSLimit        => SocialSecurityPercent is > 20 and < 25;
    public bool NeedsA1Certificate          => SocialSecurityPercent is >= 25 and < 50;
    public bool IsAtFrenchSSRisk            => SocialSecurityPercent >= 50;
    public bool IsApproachingSSLimit => IsApproachingCompanySSLimit;
    public bool IsAtTaxRisk => TaxPercent >= 40;
    public bool IsApproachingTaxLimit => TaxPercent is >= 35 and < 40;
    public bool MissionsNearLimit => MissionDays is >= 8 and < 10;
    public bool MissionsAtLimit => MissionDays >= 10;
}

public class TeleworkService(IStorageService localStorage, AuthService auth, SupabaseStorageService supabase)
{
    private const string CmuKey = "cmu_preference";
    private List<WorkDay> _days = [];
    private bool _loaded;

    public void Reset() => _loaded = false;

    // ── Load ──────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        if (_loaded) return;
        if (!auth.IsAuthenticated) { _days = []; return; }

        _days = await supabase.FetchAllAsync();

        if (_days.Count == 0)
        {
            _days = GenerateSeedData();
            await supabase.UpsertRangeAsync(_days);
        }

        _loaded = true;
        await EnsurePublicHolidaysAsync();
    }

    private async Task EnsurePublicHolidaysAsync()
    {
        var years = _days.Select(d => d.Date.Year).Append(DateTime.Today.Year).Append(DateTime.Today.Year + 1).Distinct();
        var toUpsert = new List<WorkDay>();
        foreach (var y in years)
        {
            foreach (var h in GetGenevaHolidays(y))
            {
                if (h.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                var existing = _days.FirstOrDefault(d => d.Date == h.Date);
                if (existing == null)
                {
                    var day = new WorkDay { Date = h.Date, Type = DayType.PublicHoliday };
                    _days.Add(day);
                    toUpsert.Add(day);
                }
                else if (existing.Type != DayType.PublicHoliday)
                {
                    existing.Type = DayType.PublicHoliday;
                    toUpsert.Add(existing);
                }
            }
        }
        if (toUpsert.Count > 0)
            await supabase.UpsertRangeAsync(toUpsert);
    }

    // ── Seed ─────────────────────────────────────────────────────────

    private static List<WorkDay> GenerateSeedData()
    {
        var entries = new (string date, DayType type)[]
        {
            // January 2026 — Mon = TeleworkFrance, Tue-Fri = Switzerland
            ("2026-01-01", DayType.Switzerland),  ("2026-01-02", DayType.Switzerland),
            ("2026-01-05", DayType.TeleworkFrance),
            ("2026-01-06", DayType.Switzerland),  ("2026-01-07", DayType.Switzerland),
            ("2026-01-08", DayType.Switzerland),  ("2026-01-09", DayType.Switzerland),
            ("2026-01-12", DayType.TeleworkFrance),
            ("2026-01-13", DayType.Switzerland),  ("2026-01-14", DayType.Switzerland),
            ("2026-01-15", DayType.Switzerland),  ("2026-01-16", DayType.Switzerland),
            ("2026-01-19", DayType.TeleworkFrance),
            ("2026-01-20", DayType.Switzerland),  ("2026-01-21", DayType.Switzerland),
            ("2026-01-22", DayType.Switzerland),  ("2026-01-23", DayType.Switzerland),
            ("2026-01-26", DayType.TeleworkFrance),
            ("2026-01-27", DayType.Switzerland),  ("2026-01-28", DayType.Switzerland),
            ("2026-01-29", DayType.Switzerland),  ("2026-01-30", DayType.Switzerland),

            // February 2026
            ("2026-02-02", DayType.Switzerland),  ("2026-02-03", DayType.Switzerland),
            ("2026-02-04", DayType.Switzerland),  ("2026-02-05", DayType.TeleworkFrance),
            ("2026-02-06", DayType.Switzerland),
            ("2026-02-09", DayType.Vacation),     ("2026-02-10", DayType.Vacation),
            ("2026-02-11", DayType.Vacation),     ("2026-02-12", DayType.Vacation),
            ("2026-02-13", DayType.Vacation),     ("2026-02-16", DayType.Vacation),
            ("2026-02-17", DayType.Vacation),     ("2026-02-18", DayType.Vacation),
            ("2026-02-19", DayType.Vacation),     ("2026-02-20", DayType.Vacation),
            ("2026-02-23", DayType.TeleworkFrance),
            ("2026-02-24", DayType.Switzerland),  ("2026-02-25", DayType.Switzerland),
            ("2026-02-26", DayType.Switzerland),  ("2026-02-27", DayType.TeleworkFrance),

            // March 2026
            ("2026-03-02", DayType.Switzerland),  ("2026-03-03", DayType.Switzerland),
            ("2026-03-04", DayType.Switzerland),  ("2026-03-05", DayType.TeleworkFrance),
            ("2026-03-06", DayType.Switzerland),
            ("2026-03-09", DayType.Switzerland),  ("2026-03-10", DayType.Switzerland),
            ("2026-03-11", DayType.Switzerland),  ("2026-03-12", DayType.Switzerland),
            ("2026-03-13", DayType.RnR),             ("2026-03-16", DayType.TeleworkFrance),
            ("2026-03-17", DayType.Switzerland),  ("2026-03-18", DayType.Switzerland),
            ("2026-03-19", DayType.Switzerland),  ("2026-03-20", DayType.Switzerland),
            ("2026-03-23", DayType.TeleworkFrance),
            ("2026-03-24", DayType.Switzerland),  ("2026-03-25", DayType.Switzerland),
            ("2026-03-26", DayType.Switzerland),  ("2026-03-27", DayType.Switzerland),
            ("2026-03-30", DayType.Switzerland),  ("2026-03-31", DayType.Switzerland),

            // April 2026
            ("2026-04-01", DayType.Switzerland),  ("2026-04-02", DayType.Switzerland),
            ("2026-04-03", DayType.CompanyHoliday),
            ("2026-04-06", DayType.TeleworkFrance),
            ("2026-04-07", DayType.Vacation),     ("2026-04-08", DayType.Vacation),
            ("2026-04-09", DayType.Vacation),     ("2026-04-10", DayType.Vacation),
            ("2026-04-13", DayType.TeleworkFrance), ("2026-04-14", DayType.TeleworkFrance),
            ("2026-04-15", DayType.Switzerland),  ("2026-04-16", DayType.Switzerland),
            ("2026-04-17", DayType.Switzerland),  ("2026-04-20", DayType.TeleworkFrance),
            ("2026-04-21", DayType.Switzerland),  ("2026-04-22", DayType.Switzerland),
            ("2026-04-23", DayType.Switzerland),  ("2026-04-24", DayType.Switzerland),
            ("2026-04-27", DayType.TeleworkFrance),
            ("2026-04-28", DayType.Switzerland),  ("2026-04-29", DayType.Switzerland),
            ("2026-04-30", DayType.Switzerland),

            // May 2026 (confirmed check-ins up to 13/05)
            ("2026-05-01", DayType.Switzerland),  ("2026-05-04", DayType.TeleworkFrance),
            ("2026-05-05", DayType.Switzerland),  ("2026-05-06", DayType.Switzerland),
            ("2026-05-07", DayType.Switzerland),  ("2026-05-08", DayType.RnR),
            ("2026-05-11", DayType.TeleworkFrance),
            ("2026-05-12", DayType.Switzerland),  ("2026-05-13", DayType.Switzerland),

            // R&R days
            ("2026-09-18", DayType.RnR),

            // Company holidays
            ("2026-06-19", DayType.CompanyHoliday),
            ("2026-07-31", DayType.CompanyHoliday),

            // Christmas Eve + Ripple PTO week (Dec 26+27 are weekend)
            ("2026-12-24", DayType.CompanyHoliday),
            ("2026-12-28", DayType.CompanyHoliday), ("2026-12-29", DayType.CompanyHoliday),
            ("2026-12-30", DayType.CompanyHoliday), ("2026-12-31", DayType.CompanyHoliday),

            // July vacation
            ("2026-07-20", DayType.Vacation), ("2026-07-21", DayType.Vacation),
            ("2026-07-22", DayType.Vacation), ("2026-07-23", DayType.Vacation),
            ("2026-07-24", DayType.Vacation), ("2026-07-27", DayType.Vacation),
            ("2026-07-28", DayType.Vacation), ("2026-07-29", DayType.Vacation),
            ("2026-07-30", DayType.Vacation),
        };

        return entries.Select(e => new WorkDay
        {
            Date = DateOnly.ParseExact(e.date, "yyyy-MM-dd", null),
            Type = e.type
        }).ToList();
    }

    // ── CRUD ─────────────────────────────────────────────────────────

    public IEnumerable<WorkDay> GetForYear(int year) =>
        _days.Where(d => d.Date.Year == year).OrderByDescending(d => d.Date);

    public IEnumerable<WorkDay> GetForYearByType(int year, DayType type) =>
        _days.Where(d => d.Date.Year == year && d.Type == type).OrderBy(d => d.Date);

    public bool     HasDate(DateOnly date)  => _days.Any(d => d.Date == date);
    public WorkDay? GetByDate(DateOnly date) => _days.FirstOrDefault(d => d.Date == date);

    public async Task AddAsync(WorkDay day)
    {
        _days.Add(day);
        await supabase.UpsertAsync(day);
    }

    public async Task AddRangeAsync(IEnumerable<WorkDay> days)
    {
        var list = days.ToList();
        _days.AddRange(list);
        await supabase.UpsertRangeAsync(list);
    }

    public async Task UpdateAsync(WorkDay day)
    {
        var idx = _days.FindIndex(d => d.Id == day.Id);
        if (idx >= 0) _days[idx] = day;
        await supabase.UpsertAsync(day);
    }

    public async Task DeleteAsync(Guid id)
    {
        _days.RemoveAll(d => d.Id == id);
        await supabase.DeleteAsync(id);
    }

    public async Task<int> UpsertRangeAsync(DateOnly from, DateOnly to, DayType type)
    {
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var existing = _days.FirstOrDefault(x => x.Date == d);
            if (existing != null) existing.Type = type;
            else _days.Add(new WorkDay { Date = d, Type = type });
            count++;
        }
        var changed = _days.Where(d => d.Date >= from && d.Date <= to).ToList();
        await supabase.UpsertRangeAsync(changed);
        return count;
    }

    // ── Stats ─────────────────────────────────────────────────────────

    public TeleworkStats ComputeStats(int year, DateOnly? asOf = null)
    {
        var days = _days.Where(d => d.Date.Year == year && (asOf == null || d.Date <= asOf.Value)).ToList();

        double totalWorkedDays = days
            .Where(d => d.Type is DayType.Switzerland or DayType.TeleworkFrance or DayType.MissionAbroad)
            .Sum(d => d.IsHalfDay ? 0.5 : 1.0);

        double teleworkSSDays = days
            .Where(d => d.Type == DayType.TeleworkFrance)
            .Sum(d => d.IsHalfDay ? 0.5 : 1.0);

        double teleworkTaxDays = days.Count(d => d.Type == DayType.TeleworkFrance);
        int    missionDays     = days.Count(d => d.Type == DayType.MissionAbroad);

        return new TeleworkStats
        {
            Year = year, TotalWorkedDays = totalWorkedDays,
            TeleworkSSDays = teleworkSSDays, TeleworkTaxDays = teleworkTaxDays,
            MissionDays = missionDays
        };
    }

    public int GetRemainingUnloggedWorkdays(int year)
    {
        var start = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        var end   = new DateOnly(year, 12, 31);
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (!HasDate(d)) count++;
        }
        return count;
    }

    public DateOnly? GetLastFutureLoggedWorkday(int year)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _days
            .Where(d => d.Date.Year == year && d.Date > today &&
                        d.Type is DayType.Switzerland or DayType.TeleworkFrance or DayType.MissionAbroad)
            .Select(d => (DateOnly?)d.Date)
            .Max();
    }

    public IEnumerable<int> GetAvailableYears()
    {
        var years = _days.Select(d => d.Date.Year).Distinct().ToList();
        if (!years.Contains(DateTime.Today.Year)) years.Add(DateTime.Today.Year);
        if (!years.Contains(DateTime.Today.Year + 1)) years.Add(DateTime.Today.Year + 1);
        return years.OrderDescending();
    }

    // ── Vacation periods ──────────────────────────────────────────────

    public IEnumerable<VacationPeriod> GetVacationPeriods(int year)
    {
        var days = _days
            .Where(d => d.Date.Year == year && d.Type == DayType.Vacation)
            .OrderBy(d => d.Date).ToList();

        if (!days.Any()) yield break;

        var start = days[0].Date;
        var prev  = days[0].Date;
        int count = 1;

        for (int i = 1; i < days.Count; i++)
        {
            var cur = days[i].Date;
            if (cur == NextWorkDay(prev)) { prev = cur; count++; }
            else { yield return new VacationPeriod(start, prev, count); start = cur; prev = cur; count = 1; }
        }
        yield return new VacationPeriod(start, prev, count);
    }

    private static DateOnly NextWorkDay(DateOnly d)
    {
        d = d.AddDays(1);
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d;
    }

    // ── Geneva public holidays ────────────────────────────────────────

    public static IReadOnlyList<PublicHolidayInfo> GetGenevaHolidays(int year)
    {
        var easter = EasterSunday(year);
        var jeune  = GenevaFast(year);
        return
        [
            new(new DateOnly(year, 1, 1),   "Nouvel An",           "New Year's Day"),
            new(easter.AddDays(1),           "Lundi de Pâques",    "Easter Monday"),
            new(easter.AddDays(39),          "Ascension",          "Ascension Day"),
            new(easter.AddDays(50),          "Lundi de Pentecôte", "Whit Monday"),
            new(new DateOnly(year, 5, 1),   "Fête du Travail",    "Labour Day"),
            new(new DateOnly(year, 8, 1),   "Fête nationale",     "Swiss National Day"),
            new(jeune,                       "Jeûne genevois",     "Geneva Fast"),
            new(new DateOnly(year, 12, 25), "Noël",               "Christmas Day"),
        ];
    }

    private static DateOnly EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100;
        int d = b / 4, e = b % 4, f = (b + 8) / 25;
        int g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day   = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    private static DateOnly GenevaFast(int year)
    {
        // First Thursday after the first Sunday of September
        var d = new DateOnly(year, 9, 1);
        while (d.DayOfWeek != DayOfWeek.Sunday)   d = d.AddDays(1);
        while (d.DayOfWeek != DayOfWeek.Thursday) d = d.AddDays(1);
        return d;
    }

    // ── Preferences ───────────────────────────────────────────────────

    public async Task<bool> GetCmuPreferenceAsync()         => await localStorage.GetItemAsync<bool>(CmuKey);
    public async Task       SetCmuPreferenceAsync(bool v)   => await localStorage.SetItemAsync(CmuKey, v);

    // ── Missing days ──────────────────────────────────────────────────

    private const string LastOpenedKey = "last_opened";

    public async Task<List<DateOnly>> GetAndMarkOpenAsync()
    {
        var raw      = await localStorage.GetItemAsync<string>(LastOpenedKey);
        var lastOpen = DateOnly.TryParse(raw, out var d) ? d : DateOnly.FromDateTime(DateTime.Today).AddDays(-14);
        var today    = DateOnly.FromDateTime(DateTime.Today);

        await localStorage.SetItemAsync(LastOpenedKey, today.ToString("yyyy-MM-dd"));

        var missing = new List<DateOnly>();

        // Past days: only shown once per session (between last open and yesterday)
        for (var dt = lastOpen.AddDays(1); dt < today; dt = dt.AddDays(1))
        {
            if (dt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (!HasDate(dt)) missing.Add(dt);
        }

        // Today: always checked on every load
        if (today.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !HasDate(today))
            missing.Add(today);

        return missing;
    }
}
