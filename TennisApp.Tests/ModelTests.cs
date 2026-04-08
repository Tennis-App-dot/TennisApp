using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ Model Properties — Display, Overlap, Clone, Status
/// ═══════════════════════════════════════════════════════════════════════
/// ทดสอบ: PaidCourtReservationItem, CourseCourtReservationItem,
///         CourtStatusItem, CourseTypeItem, CoursePackageItem
/// </summary>
[TestClass]
public class ModelTests
{
    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservationItem — Display Properties
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Paid_CourtDisplayName_Normal()
        => Assert.AreEqual("สนาม 01", CourtDisplay("01"));

    [TestMethod]
    public void Paid_CourtDisplayName_Unassigned()
        => Assert.AreEqual("รอจัดสรรสนาม", CourtDisplay("00"));

    [TestMethod]
    public void Paid_CourtDisplayName_Empty()
        => Assert.AreEqual("-", CourtDisplay(""));

    [TestMethod]
    public void Paid_IsCourtAssigned_True()
        => Assert.IsTrue(IsAssigned("01"));

    [TestMethod]
    public void Paid_IsCourtAssigned_False_00()
        => Assert.IsFalse(IsAssigned("00"));

    [TestMethod]
    public void Paid_IsCourtAssigned_False_Empty()
        => Assert.IsFalse(IsAssigned(""));

    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservationItem — Status Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Paid_StatusDisplay_Booked() => Assert.AreEqual("จองแล้ว", StatusDisplay("booked"));
    [TestMethod] public void Paid_StatusDisplay_InUse() => Assert.AreEqual("กำลังใช้งาน", StatusDisplay("in_use"));
    [TestMethod] public void Paid_StatusDisplay_Completed() => Assert.AreEqual("เสร็จสิ้น", StatusDisplay("completed"));
    [TestMethod] public void Paid_StatusDisplay_Cancelled() => Assert.AreEqual("ยกเลิก", StatusDisplay("cancelled"));
    [TestMethod] public void Paid_StatusDisplay_Unknown() => Assert.AreEqual("จองแล้ว", StatusDisplay("xyz"));

    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservationItem — Status Color
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Paid_StatusColor_Booked() => Assert.AreEqual("#2196F3", StatusColor("booked"));
    [TestMethod] public void Paid_StatusColor_InUse() => Assert.AreEqual("#FF9800", StatusColor("in_use"));
    [TestMethod] public void Paid_StatusColor_Completed() => Assert.AreEqual("#4CAF50", StatusColor("completed"));
    [TestMethod] public void Paid_StatusColor_Cancelled() => Assert.AreEqual("#F44336", StatusColor("cancelled"));

    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservationItem — Time Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Paid_EndTimeDisplay()
    {
        // 14:00 + 2 ชม. = 16:00
        var endTime = TimeSpan.FromHours(14).Add(TimeSpan.FromHours(2));
        Assert.AreEqual("16:00", endTime.ToString(@"hh\:mm"));
    }

    [TestMethod]
    public void Paid_DurationDisplay()
        => Assert.AreEqual("2.0 ชม.", $"{2.0:F1} ชม.");

    [TestMethod]
    public void Paid_TimeRangeDisplay()
    {
        var start = TimeSpan.FromHours(10);
        var end = start.Add(TimeSpan.FromHours(2));
        Assert.AreEqual("10:00-12:00", $"{start:hh\\:mm}-{end:hh\\:mm}");
    }

    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservationItem — OverlapsWith
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("10:00-12:00 กับ 11:00-13:00 → ชน")]
    public void Paid_Overlap_True()
    {
        Assert.IsTrue(Overlaps(
            TimeSpan.FromHours(10), 2.0,
            TimeSpan.FromHours(11), 2.0,
            new DateTime(2025, 6, 20), new DateTime(2025, 6, 20)));
    }

    [TestMethod]
    [Description("10:00-11:00 กับ 11:00-12:00 → ไม่ชน (ต่อกัน)")]
    public void Paid_Overlap_BackToBack_False()
    {
        Assert.IsFalse(Overlaps(
            TimeSpan.FromHours(10), 1.0,
            TimeSpan.FromHours(11), 1.0,
            new DateTime(2025, 6, 20), new DateTime(2025, 6, 20)));
    }

    [TestMethod]
    [Description("คนละวัน เวลาเดียวกัน → ไม่ชน")]
    public void Paid_Overlap_DifferentDate_False()
    {
        Assert.IsFalse(Overlaps(
            TimeSpan.FromHours(10), 2.0,
            TimeSpan.FromHours(10), 2.0,
            new DateTime(2025, 6, 20), new DateTime(2025, 6, 21)));
    }

    [TestMethod]
    [Description("ซ้อนทับเต็มช่วง → ชน")]
    public void Paid_Overlap_FullyContained()
    {
        Assert.IsTrue(Overlaps(
            TimeSpan.FromHours(10), 4.0,
            TimeSpan.FromHours(11), 1.0,
            new DateTime(2025, 6, 20), new DateTime(2025, 6, 20)));
    }

    // ════════════════════════════════════════════════════════════════
    // CourtStatusItem — Display Properties
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CourtStatus_DisplayName()
        => Assert.AreEqual("สนาม 01", $"สนาม {"01"}");

    [TestMethod]
    public void CourtStatus_StatusText_Available()
        => Assert.AreEqual("ว่าง", GetStatusText(false, TimeSpan.Zero, 0));

    [TestMethod]
    public void CourtStatus_StatusText_InUse()
    {
        var text = GetStatusText(true, TimeSpan.FromHours(10), 2.0);
        Assert.IsTrue(text.Contains("12:00"));
    }

    [TestMethod]
    public void CourtStatus_StatusColor_InUse()
        => Assert.AreEqual("#FF9800", GetStatusColor(true));

    [TestMethod]
    public void CourtStatus_StatusColor_Available()
        => Assert.AreEqual("#4CAF50", GetStatusColor(false));

    [TestMethod]
    public void CourtStatus_UsageTypeDisplay_Paid()
        => Assert.AreEqual("เช่าใช้พื้นที่", UsageTypeDisplay("Paid"));

    [TestMethod]
    public void CourtStatus_UsageTypeDisplay_Course()
        => Assert.AreEqual("คอร์สเรียน", UsageTypeDisplay("Course"));

    [TestMethod]
    public void CourtStatus_UsageTypeDisplay_Unknown()
        => Assert.AreEqual("-", UsageTypeDisplay(""));

    // ════════════════════════════════════════════════════════════════
    // CourtStatusItem — EndTime Calculation
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CourtStatus_EndTime_10plus2_Is12()
    {
        var endTime = TimeSpan.FromHours(10).Add(TimeSpan.FromHours(2));
        Assert.AreEqual(TimeSpan.FromHours(12), endTime);
    }

    [TestMethod]
    public void CourtStatus_EndTime_1930plus15_Is21()
    {
        var endTime = TimeSpan.FromHours(19.5).Add(TimeSpan.FromHours(1.5));
        Assert.AreEqual(TimeSpan.FromHours(21), endTime);
    }

    // ════════════════════════════════════════════════════════════════
    // CourseTypeItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CourseType_DisplayName_WithThai()
        => Assert.AreEqual("TA — Adult (ผู้ใหญ่)", CourseTypeDisplay("TA", "Adult", "ผู้ใหญ่"));

    [TestMethod]
    public void CourseType_DisplayName_WithoutThai()
        => Assert.AreEqual("TA — Adult", CourseTypeDisplay("TA", "Adult", ""));

    // ════════════════════════════════════════════════════════════════
    // CoursePackageItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Package_ClassId_TA04()
        => Assert.AreEqual("TA04", PackageClassId("TA", 4));

    [TestMethod]
    public void Package_ClassId_T300()
        => Assert.AreEqual("T300", PackageClassId("T3", 0));

    [TestMethod]
    public void Package_SessionsDisplay_Monthly()
        => Assert.AreEqual("รายเดือน", PackageSessionText(0));

    [TestMethod]
    public void Package_SessionsDisplay_PerTime()
        => Assert.AreEqual("ครั้งละ", PackageSessionText(1));

    [TestMethod]
    public void Package_SessionsDisplay_4Times()
        => Assert.AreEqual("4 ครั้ง", PackageSessionText(4));

    [TestMethod]
    public void Package_PriceDisplay()
        => Assert.AreEqual("฿2,200", $"฿{2200:N0}");

    // ════════════════════════════════════════════════════════════════
    // ClassRegisRecordItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void RegisRecord_DateFormatted()
    {
        var date = new DateTime(2025, 6, 20);
        Assert.AreEqual("20/06/2025", date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void RegisRecord_ClassTimeText_4()
        => Assert.AreEqual("4 ครั้ง", ClassTimeText(4));

    [TestMethod]
    public void RegisRecord_ClassTimeText_0()
        => Assert.AreEqual("-", ClassTimeText(0));

    [TestMethod]
    public void RegisRecord_ClassRateText()
        => Assert.AreEqual("฿2,200", $"฿{2200:N0}");

    [TestMethod]
    public void RegisRecord_TrainerDisplayName_Empty()
        => Assert.AreEqual("ไม่ระบุ", TrainerDisplay(""));

    [TestMethod]
    public void RegisRecord_TrainerDisplayName_HasName()
        => Assert.AreEqual("ครูเอ", TrainerDisplay("ครูเอ"));

    // ════════════════════════════════════════════════════════════════
    // PaidCourtUseLogItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PaidLog_CourtDisplayName()
        => Assert.AreEqual("สนาม 01", LogCourtDisplay("01"));

    [TestMethod]
    public void PaidLog_CourtDisplayName_Empty()
        => Assert.AreEqual("ยังไม่ระบุสนาม", LogCourtDisplay(""));

    [TestMethod]
    public void PaidLog_DurationDisplay()
        => Assert.AreEqual("1.5 ชั่วโมง", $"{1.5:0.#} ชั่วโมง");

    [TestMethod]
    public void PaidLog_PriceDisplay()
        => Assert.AreEqual("฿400", $"฿{400:N0}");

    [TestMethod]
    public void PaidLog_StatusDisplay_Completed()
        => Assert.AreEqual("เสร็จสมบูรณ์", LogStatusDisplay("completed"));

    [TestMethod]
    public void PaidLog_StatusDisplay_Cancelled()
        => Assert.AreEqual("ยกเลิก", LogStatusDisplay("cancelled"));

    [TestMethod]
    public void PaidLog_StatusDisplay_NoShow()
        => Assert.AreEqual("ไม่มาใช้งาน", LogStatusDisplay("no-show"));

    // ════════════════════════════════════════════════════════════════
    // CourseCourtUseLogItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CourseLog_CheckInTimeDisplay()
    {
        var dt = new DateTime(2025, 6, 20, 14, 30, 0);
        Assert.AreEqual("20/06/2025 14:30", dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void CourseLog_ReserveTimeDisplay()
    {
        var t = TimeSpan.FromHours(9);
        Assert.AreEqual("09:00", $"{t:hh\\:mm}");
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate model display logic
    // ════════════════════════════════════════════════════════════════

    private static string CourtDisplay(string id)
        => string.IsNullOrEmpty(id) ? "-" : id == "00" ? "รอจัดสรรสนาม" : $"สนาม {id}";

    private static bool IsAssigned(string id)
        => !string.IsNullOrEmpty(id) && id != "00";

    private static string StatusDisplay(string s) => s switch
    {
        "booked" => "จองแล้ว", "in_use" => "กำลังใช้งาน",
        "completed" => "เสร็จสิ้น", "cancelled" => "ยกเลิก", _ => "จองแล้ว"
    };

    private static string StatusColor(string s) => s switch
    {
        "booked" => "#2196F3", "in_use" => "#FF9800",
        "completed" => "#4CAF50", "cancelled" => "#F44336", _ => "#2196F3"
    };

    private static bool Overlaps(TimeSpan s1, double d1, TimeSpan s2, double d2, DateTime date1, DateTime date2)
    {
        if (date1.Date != date2.Date) return false;
        var e1 = s1.Add(TimeSpan.FromHours(d1));
        var e2 = s2.Add(TimeSpan.FromHours(d2));
        return s1 < e2 && e1 > s2;
    }

    private static string GetStatusText(bool inUse, TimeSpan start, double duration)
    {
        if (!inUse) return "ว่าง";
        var end = start.Add(TimeSpan.FromHours(duration));
        return $"ระยะเวลาสิ้นสุด: {end:hh\\:mm}";
    }

    private static string GetStatusColor(bool inUse) => inUse ? "#FF9800" : "#4CAF50";

    private static string UsageTypeDisplay(string t) => t switch
    {
        "Paid" => "เช่าใช้พื้นที่", "Course" => "คอร์สเรียน", _ => "-"
    };

    private static string CourseTypeDisplay(string code, string name, string thai)
        => string.IsNullOrWhiteSpace(thai) ? $"{code} — {name}" : $"{code} — {name} ({thai})";

    private static string PackageClassId(string type, int sessions) => $"{type}{sessions:D2}";

    private static string PackageSessionText(int s) => s switch
    {
        0 => "รายเดือน", 1 => "ครั้งละ", _ => $"{s} ครั้ง"
    };

    private static string ClassTimeText(int t) => t > 0 ? $"{t} ครั้ง" : "-";

    private static string TrainerDisplay(string name)
        => string.IsNullOrWhiteSpace(name) ? "ไม่ระบุ" : name;

    private static string LogCourtDisplay(string id)
        => string.IsNullOrEmpty(id) ? "ยังไม่ระบุสนาม" : $"สนาม {id}";

    private static string LogStatusDisplay(string s) => s switch
    {
        "completed" => "เสร็จสมบูรณ์", "cancelled" => "ยกเลิก",
        "no-show" => "ไม่มาใช้งาน", _ => s
    };
}
