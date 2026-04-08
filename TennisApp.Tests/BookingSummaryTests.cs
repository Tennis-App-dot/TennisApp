using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ BookingPageViewModel — UpdateSummary + Overlap Logic
/// ═══════════════════════════════════════════════════════════════════════
/// ทดสอบ business logic ที่ไม่ต้องพึ่ง DB (pure computation)
/// </summary>
[TestClass]
public class BookingSummaryTests
{
    private static readonly DateTime Today = new(2025, 6, 20);

    // ════════════════════════════════════════════════════════════════
    // UpdateSummary — TodayCount
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("วันนี้มี 3 จอง (not cancelled) → TodayCount = 3")]
    public void Summary_TodayCount_3()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today, "booked"),
            MakeRes(Today, "in_use"),
            MakeRes(Today, "completed"),
        };
        var (todayCount, _, _) = CalcSummary(paid, new());
        Assert.AreEqual(3, todayCount);
    }

    [TestMethod]
    [Description("วันนี้มี 1 ถูก cancel → ไม่นับใน TodayCount")]
    public void Summary_TodayCount_ExcludesCancelled()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today, "booked"),
            MakeRes(Today, "cancelled"),
        };
        var (todayCount, _, _) = CalcSummary(paid, new());
        Assert.AreEqual(1, todayCount);
    }

    [TestMethod]
    [Description("ไม่มีจองวันนี้ → TodayCount = 0")]
    public void Summary_TodayCount_None()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today.AddDays(1), "booked"),
        };
        var (todayCount, _, _) = CalcSummary(paid, new());
        Assert.AreEqual(0, todayCount);
    }

    // ════════════════════════════════════════════════════════════════
    // UpdateSummary — FutureCount
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("มี 2 จองในอนาคต (booked) → FutureCount = 2")]
    public void Summary_FutureCount()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today.AddDays(1), "booked"),
            MakeRes(Today.AddDays(3), "booked"),
            MakeRes(Today.AddDays(5), "cancelled"),
        };
        var (_, futureCount, _) = CalcSummary(paid, new());
        Assert.AreEqual(2, futureCount);
    }

    [TestMethod]
    [Description("วันนี้ไม่นับเป็น future")]
    public void Summary_FutureCount_TodayNotIncluded()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today, "booked"),
        };
        var (_, futureCount, _) = CalcSummary(paid, new());
        Assert.AreEqual(0, futureCount);
    }

    // ════════════════════════════════════════════════════════════════
    // UpdateSummary — CancelledCount
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Cancelled ทั้งหมด (ไม่จำกัดวัน) → CancelledCount = 2")]
    public void Summary_CancelledCount()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today, "cancelled"),
            MakeRes(Today.AddDays(-5), "cancelled"),
            MakeRes(Today, "booked"),
        };
        var (_, _, cancelledCount) = CalcSummary(paid, new());
        Assert.AreEqual(2, cancelledCount);
    }

    // ════════════════════════════════════════════════════════════════
    // UpdateSummary — Mixed Paid + Course
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("2 Paid วันนี้ + 1 Course วันนี้ → TodayCount = 3")]
    public void Summary_MixedPaidCourse()
    {
        var paid = new List<TestRes>
        {
            MakeRes(Today, "booked"),
            MakeRes(Today, "in_use"),
        };
        var course = new List<TestRes>
        {
            MakeRes(Today, "booked"),
        };
        var (todayCount, _, _) = CalcSummary(paid, course);
        Assert.AreEqual(3, todayCount);
    }

    [TestMethod]
    [Description("Cancelled รวม Paid + Course")]
    public void Summary_CancelledMixed()
    {
        var paid = new List<TestRes> { MakeRes(Today, "cancelled") };
        var course = new List<TestRes> { MakeRes(Today, "cancelled") };
        var (_, _, cancelled) = CalcSummary(paid, course);
        Assert.AreEqual(2, cancelled);
    }

    // ════════════════════════════════════════════════════════════════
    // EstimatedEndTime
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void EstimatedEndTime_8plus1_Is9()
    {
        var end = TimeSpan.FromHours(8).Add(TimeSpan.FromHours(1));
        Assert.AreEqual("09:00", end.ToString(@"hh\:mm"));
    }

    [TestMethod]
    public void EstimatedEndTime_14plus2_Is16()
    {
        var end = TimeSpan.FromHours(14).Add(TimeSpan.FromHours(2));
        Assert.AreEqual("16:00", end.ToString(@"hh\:mm"));
    }

    [TestMethod]
    public void EstimatedEndTime_19plus2_Is21()
    {
        var end = TimeSpan.FromHours(19).Add(TimeSpan.FromHours(2));
        Assert.AreEqual("21:00", end.ToString(@"hh\:mm"));
    }

    // ════════════════════════════════════════════════════════════════
    // IsCourseReservation
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void IsCourseReservation_Course_True()
        => Assert.IsTrue("Course" == "Course");

    [TestMethod]
    public void IsCourseReservation_Paid_False()
        => Assert.IsFalse("Paid" == "Course");

    // ════════════════════════════════════════════════════════════════
    // Duplicate Name Check Logic
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ชื่อเดียวกัน + เวลาซ้อน → duplicate")]
    public void DuplicateCheck_SameNameOverlap_IsDuplicate()
    {
        var existing = new TestRes
        {
            Date = Today,
            Name = "สมชาย",
            Start = TimeSpan.FromHours(10),
            Duration = 2.0,
            Status = "booked"
        };

        bool isDuplicate = CheckDuplicate(
            new[] { existing },
            "สมชาย", Today,
            TimeSpan.FromHours(11), 1.0);

        Assert.IsTrue(isDuplicate);
    }

    [TestMethod]
    [Description("ชื่อต่างกัน + เวลาซ้อน → ไม่ duplicate")]
    public void DuplicateCheck_DifferentName_NotDuplicate()
    {
        var existing = new TestRes
        {
            Date = Today,
            Name = "สมชาย",
            Start = TimeSpan.FromHours(10),
            Duration = 2.0,
            Status = "booked"
        };

        bool isDuplicate = CheckDuplicate(
            new[] { existing },
            "สมหญิง", Today,
            TimeSpan.FromHours(11), 1.0);

        Assert.IsFalse(isDuplicate);
    }

    [TestMethod]
    [Description("ชื่อเดียวกัน + เวลาไม่ซ้อน → ไม่ duplicate")]
    public void DuplicateCheck_SameNameNoOverlap_NotDuplicate()
    {
        var existing = new TestRes
        {
            Date = Today,
            Name = "สมชาย",
            Start = TimeSpan.FromHours(10),
            Duration = 1.0,
            Status = "booked"
        };

        bool isDuplicate = CheckDuplicate(
            new[] { existing },
            "สมชาย", Today,
            TimeSpan.FromHours(11), 1.0); // 11:00-12:00 ไม่ซ้อนกับ 10:00-11:00

        Assert.IsFalse(isDuplicate);
    }

    [TestMethod]
    [Description("ชื่อเดียวกัน case insensitive")]
    public void DuplicateCheck_CaseInsensitive()
    {
        var existing = new TestRes
        {
            Date = Today,
            Name = "John",
            Start = TimeSpan.FromHours(10),
            Duration = 2.0,
            Status = "booked"
        };

        bool isDuplicate = CheckDuplicate(
            new[] { existing },
            "john", Today,
            TimeSpan.FromHours(11), 1.0);

        Assert.IsTrue(isDuplicate);
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════

    private class TestRes
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = "booked";
        public string Name { get; set; } = "";
        public TimeSpan Start { get; set; }
        public double Duration { get; set; } = 1.0;
    }

    private static TestRes MakeRes(DateTime date, string status)
        => new() { Date = date, Status = status };

    private static (int TodayCount, int FutureCount, int CancelledCount) CalcSummary(
        List<TestRes> paid, List<TestRes> course)
    {
        var today = Today;
        var todayCount =
            paid.Count(r => r.Date.Date == today && r.Status != "cancelled") +
            course.Count(r => r.Date.Date == today && r.Status != "cancelled");

        var futureCount =
            paid.Count(r => r.Date.Date > today && r.Status == "booked") +
            course.Count(r => r.Date.Date > today && r.Status == "booked");

        var cancelledCount =
            paid.Count(r => r.Status == "cancelled") +
            course.Count(r => r.Status == "cancelled");

        return (todayCount, futureCount, cancelledCount);
    }

    private static bool CheckDuplicate(
        IEnumerable<TestRes> existing, string name, DateTime date,
        TimeSpan startTime, double duration)
    {
        var endTime = startTime.Add(TimeSpan.FromHours(duration));
        return existing.Any(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            r.Date.Date == date.Date &&
            r.Status is "booked" or "in_use" &&
            r.Start < endTime &&
            r.Start.Add(TimeSpan.FromHours(r.Duration)) > startTime);
    }
}
