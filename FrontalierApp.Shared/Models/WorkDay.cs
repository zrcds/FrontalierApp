namespace FrontalierApp.Models;

public enum DayType
{
    Switzerland     = 0,
    TeleworkFrance  = 1,
    MissionAbroad   = 2,
    Vacation        = 3,   // was Holiday — same int value, localStorage-compatible
    SickLeave       = 4,
    PublicHoliday   = 5,
    CompanyHoliday  = 6,
    RegionalHoliday = 7,
    RnR             = 8,
}

public static class DayTypeExtensions
{
    // Days in the SS% and tax% totals. R&R counts as a working day, not as PTO: Ripple HR
    // keeps R&R in the total and only takes PTO out (confirmed 2026-10-07).
    public static bool IsWorkedDay(this DayType type) =>
        type is DayType.Switzerland or DayType.TeleworkFrance or DayType.MissionAbroad or DayType.RnR;

    // Days counted as telework in the SS% and tax% numerators. Ripple HR reports the share of
    // worked days not spent in the office, so an R&R day away from the office counts like
    // telework. At 2026-08-31: (25 telework + 2 R&R) / 139 = 19.4%, the complement of HR's
    // 80.6% in-office rate. An R&R day spent in the office is flagged WorkedInOffice.
    public static bool CountsAsTelework(this DayType type) =>
        type is DayType.TeleworkFrance or DayType.RnR;

    // Company days off that can still be spent in the office.
    public static bool CanBeWorkedInOffice(this DayType type) =>
        type is DayType.RnR or DayType.CompanyHoliday or DayType.RegionalHoliday;
}

public class WorkDay
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateOnly Date      { get; set; }
    public DayType  Type      { get; set; }
    public bool     IsHalfDay { get; set; }
    public string?  Note      { get; set; }

    // Came into the office on a company day off. The day keeps its type, so it still shows
    // in the R&R and company-day lists, but every count treats it as an office day.
    public bool     WorkedInOffice { get; set; }

    // The type the day counts as in every percentage and total.
    public DayType CountedType =>
        WorkedInOffice && Type.CanBeWorkedInOffice() ? DayType.Switzerland : Type;
}
