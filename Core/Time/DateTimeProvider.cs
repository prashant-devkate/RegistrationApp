namespace RegistrationApp.Core.Time;

/// <summary>
/// Centralized time provider. All persisted, user-facing timestamps in the
/// application are stored in India Standard Time (IST, UTC+05:30).
/// </summary>
public static class DateTimeProvider
{
    private static readonly TimeZoneInfo IndiaTimeZone = ResolveIndiaTimeZone();

    /// <summary>
    /// Current time in India Standard Time (UTC+05:30).
    /// Kind is <see cref="DateTimeKind.Unspecified"/> because it represents a local IST wall-clock time.
    /// </summary>
    public static DateTime IstNow =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);

    private static TimeZoneInfo ResolveIndiaTimeZone()
    {
        // Windows uses "India Standard Time"; Linux/macOS use the IANA id "Asia/Kolkata".
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next identifier.
            }
        }

        // Final fallback: fixed +05:30 offset (IST has no daylight saving).
        return TimeZoneInfo.CreateCustomTimeZone(
            "IST",
            TimeSpan.FromMinutes(330),
            "India Standard Time",
            "India Standard Time");
    }
}
