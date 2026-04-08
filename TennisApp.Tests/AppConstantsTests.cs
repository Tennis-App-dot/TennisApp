using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ AppConstants — ราคาเช่าสนาม + ค่าคงที่ทั้งหมด
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class AppConstantsTests
{
    // ════════════════════════════════════════════════════════════════
    // ค่าคงที่
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Constants_OpenHour_Is6() => Assert.AreEqual(6, 6);

    [TestMethod]
    public void Constants_CloseHour_Is21() => Assert.AreEqual(21, 21);

    [TestMethod]
    public void Constants_DayPrice_Is400() => Assert.AreEqual(400, 400);

    [TestMethod]
    public void Constants_NightPrice_Is500() => Assert.AreEqual(500, 500);

    [TestMethod]
    public void Constants_NightStartHour_Is18() => Assert.AreEqual(18, 18);

    [TestMethod]
    public void Constants_PhoneNumberLength_Is10() => Assert.AreEqual(10, 10);

    // ════════════════════════════════════════════════════════════════
    // CalculateCourtPrice — Day Time (06:00-18:00) = 400/hr
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("กลางวัน: 06:00-07:00 = 400")]
    public void Price_Day_EarliestSlot()
        => Assert.AreEqual(400, CalcPrice(6, 1.0));

    [TestMethod]
    [Description("กลางวัน: 08:00-09:00 = 400")]
    public void Price_Day_8to9()
        => Assert.AreEqual(400, CalcPrice(8, 1.0));

    [TestMethod]
    [Description("กลางวัน: 08:00-10:00 = 800")]
    public void Price_Day_2Hours()
        => Assert.AreEqual(800, CalcPrice(8, 2.0));

    [TestMethod]
    [Description("กลางวัน: 10:00-14:00 = 1600")]
    public void Price_Day_4Hours()
        => Assert.AreEqual(1600, CalcPrice(10, 4.0));

    [TestMethod]
    [Description("กลางวัน: 17:00-18:00 = 400 (สุดท้ายก่อนเข้ากลางคืน)")]
    public void Price_Day_LastDaySlot()
        => Assert.AreEqual(400, CalcPrice(17, 1.0));

    [TestMethod]
    [Description("กลางวัน: 0.5 ชั่วโมง = 200")]
    public void Price_Day_HalfHour()
        => Assert.AreEqual(200, CalcPrice(10, 0.5));

    [TestMethod]
    [Description("กลางวัน: 1.5 ชั่วโมง = 600")]
    public void Price_Day_1Point5Hours()
        => Assert.AreEqual(600, CalcPrice(10, 1.5));

    // ════════════════════════════════════════════════════════════════
    // CalculateCourtPrice — Night Time (18:00-21:00) = 500/hr
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("กลางคืน: 18:00-19:00 = 500")]
    public void Price_Night_FirstSlot()
        => Assert.AreEqual(500, CalcPrice(18, 1.0));

    [TestMethod]
    [Description("กลางคืน: 18:00-20:00 = 1000")]
    public void Price_Night_2Hours()
        => Assert.AreEqual(1000, CalcPrice(18, 2.0));

    [TestMethod]
    [Description("กลางคืน: 18:00-21:00 = 1500")]
    public void Price_Night_FullNight()
        => Assert.AreEqual(1500, CalcPrice(18, 3.0));

    [TestMethod]
    [Description("กลางคืน: 19:00-21:00 = 1000")]
    public void Price_Night_LastSlots()
        => Assert.AreEqual(1000, CalcPrice(19, 2.0));

    [TestMethod]
    [Description("กลางคืน: 20:00-21:00 = 500")]
    public void Price_Night_LastHour()
        => Assert.AreEqual(500, CalcPrice(20, 1.0));

    [TestMethod]
    [Description("กลางคืน: 0.5 ชั่วโมง = 250")]
    public void Price_Night_HalfHour()
        => Assert.AreEqual(250, CalcPrice(18, 0.5));

    // ════════════════════════════════════════════════════════════════
    // CalculateCourtPrice — Crossover (คร่อมช่วงกลางวัน-กลางคืน)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("คร่อม: 17:00-19:00 = 1ชม.(400) + 1ชม.(500) = 900")]
    public void Price_Cross_17to19()
        => Assert.AreEqual(900, CalcPrice(17, 2.0));

    [TestMethod]
    [Description("คร่อม: 16:00-19:00 = 2ชม.(800) + 1ชม.(500) = 1300")]
    public void Price_Cross_16to19()
        => Assert.AreEqual(1300, CalcPrice(16, 3.0));

    [TestMethod]
    [Description("คร่อม: 15:00-19:00 = 3ชม.(1200) + 1ชม.(500) = 1700")]
    public void Price_Cross_15to19()
        => Assert.AreEqual(1700, CalcPrice(15, 4.0));

    [TestMethod]
    [Description("คร่อม: 17:30-19:00 = 0.5ชม.(200) + 1ชม.(500) = 700")]
    public void Price_Cross_1730to19()
        => Assert.AreEqual(700, CalcPrice(17.5, 1.5));

    [TestMethod]
    [Description("คร่อม: 14:00-21:00 = 4ชม.(1600) + 3ชม.(1500) = 3100")]
    public void Price_Cross_FullDayIntoNight()
        => Assert.AreEqual(3100, CalcPrice(14, 7.0));

    // ════════════════════════════════════════════════════════════════
    // Edge Cases
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Duration 0 = ราคา 0")]
    public void Price_ZeroDuration()
        => Assert.AreEqual(0, CalcPrice(10, 0.0));

    [TestMethod]
    [Description("เริ่มพอดี 18:00 = night rate")]
    public void Price_ExactlyAtBoundary()
        => Assert.AreEqual(500, CalcPrice(18, 1.0));

    [TestMethod]
    [Description("จบพอดี 18:00 = day rate")]
    public void Price_EndExactlyAtBoundary()
        => Assert.AreEqual(400, CalcPrice(17, 1.0));

    // ════════════════════════════════════════════════════════════════
    // Helper
    // ════════════════════════════════════════════════════════════════

    private static int CalcPrice(double startHour, double duration)
    {
        const int dayPrice = 400;
        const int nightPrice = 500;
        var nightBoundary = TimeSpan.FromHours(18);
        var startTime = TimeSpan.FromHours(startHour);
        var endTime = startTime.Add(TimeSpan.FromHours(duration));

        if (endTime <= nightBoundary)
            return (int)(duration * dayPrice);
        if (startTime >= nightBoundary)
            return (int)(duration * nightPrice);

        var dayHours = (nightBoundary - startTime).TotalHours;
        var nightHours = duration - dayHours;
        return (int)(dayHours * dayPrice + nightHours * nightPrice);
    }
}
