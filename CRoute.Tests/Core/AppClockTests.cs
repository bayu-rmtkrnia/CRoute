using CRoute.Core;

namespace CRoute.Tests.Core;

public class AppClockTests
{
    [Fact]
    public void Zone_MemakaiOffsetWib()
    {
        Assert.Equal(TimeSpan.FromHours(7), AppClock.Zone.BaseUtcOffset);
        Assert.Contains(AppClock.Zone.Id, new[] { "Asia/Jakarta", "SE Asia Standard Time" });
    }

    [Fact]
    public void ToAppTime_Utc_MenggeserKeWib()
    {
        // 30 November 2026 pukul 17.00 UTC = 1 Desember 2026 pukul 00.00 WIB.
        var utc = new DateTime(2026, 11, 30, 17, 0, 0, DateTimeKind.Utc);

        DateTime result = AppClock.ToAppTime(utc);

        Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0), result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public void ToAppTime_Unspecified_DianggapSudahWib()
    {
        var value = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal(value, AppClock.ToAppTime(value));
    }
}
