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
}

public class WorkDay
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateOnly Date      { get; set; }
    public DayType  Type      { get; set; }
    public bool     IsHalfDay { get; set; }
    public string?  Note      { get; set; }
}
