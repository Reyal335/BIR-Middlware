namespace Eis.Mock.Auth;

public static class ManilaClock
{
    private static readonly TimeZoneInfo ManilaZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

    /// <summary>Philippine time has no DST, so the offset is always +08:00.</summary>
    public static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    public static DateTime Now() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ManilaZone);
}
