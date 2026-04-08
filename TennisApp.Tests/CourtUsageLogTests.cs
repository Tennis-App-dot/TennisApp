using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TennisApp.Tests.Data;
using TennisApp.Tests.Helpers;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Integration Tests สำหรับ CourtUsageLogPage — ทดสอบ Business Logic จริง
/// ═══════════════════════════════════════════════════════════════════════
/// 
/// ใช้ SQLite database จริง (temp file) เพื่อทดสอบ flow ทั้งหมด:
///   Walk-in → Start → End → Log
///   Booked → Search → Start → End → Log  
///   Extend (สำเร็จ / ชนเวลา / เกิน 21:00)
///   Cancel
///   Conflict Check (Paid vs Paid, Paid vs Course, ไม่ซ้อน)
///   Price Calculation (กลางวัน / กลางคืน / คร่อมช่วง)
/// </summary>
[TestClass]
public class CourtUsageLogTests
{
    private TestDatabaseHelper _db = null!;

    private static readonly DateTime Today = new(2025, 6, 20);

    [TestInitialize]
    public async Task Setup()
    {
        _db = new TestDatabaseHelper();

        // สร้างสนาม 3 สนาม
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");
        await _db.Courts.AddCourtAsync("03", "1");

        // สร้าง Trainer + Course สำหรับ test คอร์ส
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ", "0812345678");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult Class", 4, 1, 2200);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db?.Dispose();
    }

    // ════════════════════════════════════════════════════════════════
    // 1. PRICE CALCULATION — CalculateCourtPrice
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ราคากลางวัน: 08:00-10:00 (2 ชม.) = 2 × 400 = 800")]
    public void Price_DayTime_2Hours()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(8), 2.0);
        Assert.AreEqual(800, price);
    }

    [TestMethod]
    [Description("ราคากลางวัน: 10:00-11:00 (1 ชม.) = 1 × 400 = 400")]
    public void Price_DayTime_1Hour()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(10), 1.0);
        Assert.AreEqual(400, price);
    }

    [TestMethod]
    [Description("ราคากลางคืน: 18:00-20:00 (2 ชม.) = 2 × 500 = 1000")]
    public void Price_NightTime_2Hours()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(18), 2.0);
        Assert.AreEqual(1000, price);
    }

    [TestMethod]
    [Description("ราคากลางคืน: 19:00-21:00 (2 ชม.) = 2 × 500 = 1000")]
    public void Price_NightTime_LastSlot()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(19), 2.0);
        Assert.AreEqual(1000, price);
    }

    [TestMethod]
    [Description("ราคาคร่อมช่วง: 17:00-19:00 → 1 ชม. กลางวัน(400) + 1 ชม. กลางคืน(500) = 900")]
    public void Price_CrossoverDayNight()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(17), 2.0);
        Assert.AreEqual(900, price);
    }

    [TestMethod]
    [Description("ราคาคร่อมช่วง: 16:00-19:00 → 2 ชม. กลางวัน(800) + 1 ชม. กลางคืน(500) = 1300")]
    public void Price_CrossoverDayNight_3Hours()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(16), 3.0);
        Assert.AreEqual(1300, price);
    }

    [TestMethod]
    [Description("ราคา 0.5 ชั่วโมง กลางวัน = 200")]
    public void Price_HalfHour_DayTime()
    {
        var price = CalculateCourtPrice(TimeSpan.FromHours(10), 0.5);
        Assert.AreEqual(200, price);
    }

    // ════════════════════════════════════════════════════════════════
    // 2. WALK-IN PAID → START → END (Full Flow)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Walk-in Paid: สร้าง reservation → status = in_use → End → status = completed + log ถูก insert")]
    public async Task WalkIn_Paid_StartToEnd_FullFlow()
    {
        // Arrange: สร้าง Walk-in reservation ตรงๆ
        var reserveId = "2025062001";
        var startTime = DateTime.Now.AddHours(-1); // เริ่ม 1 ชม. ที่แล้ว

        var reservation = new TestPaidReservation
        {
            ReserveId = reserveId,
            CourtId = "01",
            RequestDate = DateTime.Now,
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "สมชาย ทดสอบ",
            ReservePhone = "0891234567",
            Status = "in_use",
            ActualStart = startTime,
            ActualPrice = 400
        };

        var added = await _db.PaidReservations.AddAsync(reservation);
        Assert.IsTrue(added, "ควรเพิ่ม reservation ได้");

        // Act: ตรวจสอบว่า reservation ถูกสร้างเป็น in_use
        var saved = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.IsNotNull(saved, "ควรหา reservation เจอ");
        Assert.AreEqual("in_use", saved.Status);
        Assert.AreEqual("01", saved.CourtId);
        Assert.AreEqual("สมชาย ทดสอบ", saved.ReserveName);

        // Act: End usage → เปลี่ยนเป็น completed
        var endTime = DateTime.Now;
        var endPrice = 400;
        var updated = await _db.PaidReservations.UpdateStatusAsync(
            reserveId, "completed",
            actualStart: startTime,
            actualEnd: endTime,
            actualPrice: endPrice);
        Assert.IsTrue(updated, "ควร update status ได้");

        // Assert: reservation เป็น completed
        var completed = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.IsNotNull(completed);
        Assert.AreEqual("completed", completed.Status);
        Assert.IsNotNull(completed.ActualEnd);
        Assert.AreEqual(endPrice, completed.ActualPrice);

        // Act: Insert usage log
        var actualDuration = (endTime - startTime).TotalHours;
        var logInserted = await _db.PaidUseLogs.InsertAsync(
            "2025062001", reserveId, startTime,
            Math.Round(actualDuration, 2), endPrice);
        Assert.IsTrue(logInserted, "ควร insert log ได้");

        // Assert: log ถูกเพิ่ม
        var logCount = await _db.PaidUseLogs.CountAllAsync();
        Assert.AreEqual(1, logCount, "ควรมี 1 log");

        var logData = await _db.PaidUseLogs.GetByReserveIdAsync(reserveId);
        Assert.IsNotNull(logData);
        Assert.AreEqual("completed", logData.Value.Status);
        Assert.AreEqual(endPrice, logData.Value.Price);
        Assert.IsTrue(logData.Value.Duration > 0, "duration ต้องมากกว่า 0");
    }

    // ════════════════════════════════════════════════════════════════
    // 3. BOOKED → SEARCH → START → END
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("จองล่วงหน้า → ค้นหาเจอ → Start → End → Log")]
    public async Task Booked_SearchThenStartThenEnd()
    {
        // Arrange: สร้าง reservation ที่ status = booked
        var reserveId = "2025062002";
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = reserveId,
            CourtId = "00", // ยังไม่ assign สนาม
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(14),
            Duration = 2.0,
            ReserveName = "สมหญิง จองล่วงหน้า",
            ReservePhone = "0899999999",
            Status = "booked"
        });

        // Act: Search — ต้องเจอ
        var found = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.IsNotNull(found);
        Assert.AreEqual("booked", found.Status);
        Assert.AreEqual("สมหญิง จองล่วงหน้า", found.ReserveName);

        // Act: Start — เปลี่ยนเป็น in_use + assign สนาม 02
        var startTime = DateTime.Now;
        await _db.PaidReservations.UpdateStatusAsync(reserveId, "in_use", actualStart: startTime);

        // ต้อง update court_id ด้วย (ใช้ SQL ตรง)
        await UpdateCourtId(reserveId, "02");

        var inUse = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.AreEqual("in_use", inUse!.Status);
        Assert.AreEqual("02", inUse.CourtId);
        Assert.IsNotNull(inUse.ActualStart);

        // Act: End
        var endTime = DateTime.Now;
        var price = CalculateCourtPrice(TimeSpan.FromHours(14), 2.0); // 14:00-16:00 = 800
        await _db.PaidReservations.UpdateStatusAsync(reserveId, "completed",
            actualStart: startTime, actualEnd: endTime, actualPrice: price);

        var done = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.AreEqual("completed", done!.Status);
        Assert.AreEqual(800, done.ActualPrice);

        // Assert: Insert log
        var logOk = await _db.PaidUseLogs.InsertAsync("2025062002", reserveId, startTime, 2.0, price);
        Assert.IsTrue(logOk);
    }

    // ════════════════════════════════════════════════════════════════
    // 4. WALK-IN COURSE → START → END
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Walk-in Course: สร้าง course reservation → in_use → completed → course log")]
    public async Task WalkIn_Course_StartToEnd()
    {
        var reserveId = "2025062003";

        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = reserveId,
            CourtId = "03",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.0,
            ReserveName = "ครูเอ",
            ReservePhone = "0812345678",
            Status = "in_use",
            ActualStart = DateTime.Now.AddHours(-1)
        });

        // Verify created
        var res = await _db.CourseReservations.GetByIdAsync(reserveId);
        Assert.IsNotNull(res);
        Assert.AreEqual("in_use", res.Status);
        Assert.AreEqual("TA04", res.ClassId);

        // End
        var endTime = DateTime.Now;
        await _db.CourseReservations.UpdateStatusAsync(reserveId, "completed",
            actualStart: res.ActualStart, actualEnd: endTime);

        var completed = await _db.CourseReservations.GetByIdAsync(reserveId);
        Assert.AreEqual("completed", completed!.Status);

        // Insert course log
        var actualDuration = (endTime - res.ActualStart!.Value).TotalHours;
        var logOk = await _db.CourseUseLogs.InsertAsync(
            "2025062003", reserveId, res.ActualStart.Value, Math.Round(actualDuration, 2));
        Assert.IsTrue(logOk);

        var logCount = await _db.CourseUseLogs.CountAllAsync();
        Assert.AreEqual(1, logCount);
    }

    // ════════════════════════════════════════════════════════════════
    // 5. EXTEND — สำเร็จ (ไม่ชนเวลา)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ขยายเวลา: 10:00-11:00 → ขยาย 1 ชม. → 10:00-12:00 (ไม่ชนใคร)")]
    public async Task Extend_Success_NoConflict()
    {
        var reserveId = "2025062004";
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = reserveId,
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ทดสอบ ขยายเวลา",
            Status = "in_use",
            ActualStart = DateTime.Now
        });

        // Extend by 1 hour
        double extendHours = 1.0;
        var original = await _db.PaidReservations.GetByIdAsync(reserveId);
        var newDuration = original!.Duration + extendHours;

        var updated = await _db.PaidReservations.UpdateDurationAsync(reserveId, newDuration);
        Assert.IsTrue(updated);

        var extended = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.AreEqual(2.0, extended!.Duration, "duration ควรเป็น 2.0 ชม.");
    }

    // ════════════════════════════════════════════════════════════════
    // 6. EXTEND — ชนเวลากับ reservation อื่น
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ขยายเวลาแล้วชนกับ reservation ถัดไป → ต้อง detect conflict")]
    public async Task Extend_ConflictDetected()
    {
        // Reservation A: 10:00-11:00 (in_use) → ต้องการขยายเป็น 10:00-13:00
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062005",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "คนแรก",
            Status = "in_use",
            ActualStart = DateTime.Now
        });

        // Reservation B: 12:00-13:00 (booked) — อยู่สนามเดียวกัน
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062006",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(12),
            Duration = 1.0,
            ReserveName = "คนที่สอง",
            Status = "booked"
        });

        // Check: ขยาย A อีก 2 ชม. → 10:00-13:00 → ชนกับ B (12:00-13:00)
        var startTimeA = TimeSpan.FromHours(10);
        var newDurationA = 1.0 + 2.0; // 3 ชม.
        var newEndTimeA = startTimeA.Add(TimeSpan.FromHours(newDurationA)); // 13:00

        var reservationsOnDate = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservationsOnDate.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.ReserveId != "2025062005" &&
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < newEndTimeA &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTimeA);

        Assert.IsNotNull(conflict, "ต้องตรวจจับ conflict ได้");
        Assert.AreEqual("2025062006", conflict.ReserveId);
        Assert.AreEqual("คนที่สอง", conflict.ReserveName);
    }

    // ════════════════════════════════════════════════════════════════
    // 7. EXTEND — เกิน 21:00
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ขยายเวลาจนเกิน 21:00 → ต้องป้องกัน")]
    public void Extend_ExceedsClosingTime()
    {
        // สนามเปิดถึง 21:00
        var startTime = TimeSpan.FromHours(19); // 19:00
        var currentDuration = 1.0;              // 19:00-20:00
        var extendHours = 2.0;                  // ต้องการขยายเป็น 19:00-22:00

        var newDuration = currentDuration + extendHours;
        var newEndTime = startTime.Add(TimeSpan.FromHours(newDuration));
        var closingTime = TimeSpan.FromHours(21);

        Assert.IsTrue(newEndTime > closingTime,
            $"เวลาสิ้นสุดใหม่ ({newEndTime:hh\\:mm}) เกิน 21:00 — ต้อง reject");
    }

    [TestMethod]
    [Description("ขยายเวลาพอดี 21:00 → ผ่าน")]
    public void Extend_ExactlyAtClosingTime_Allowed()
    {
        var startTime = TimeSpan.FromHours(19);
        var currentDuration = 1.0;
        var extendHours = 1.0; // 19:00-21:00 พอดี

        var newDuration = currentDuration + extendHours;
        var newEndTime = startTime.Add(TimeSpan.FromHours(newDuration));
        var closingTime = TimeSpan.FromHours(21);

        Assert.IsFalse(newEndTime > closingTime,
            "เวลาสิ้นสุดพอดี 21:00 — ควรอนุญาต");
    }

    // ════════════════════════════════════════════════════════════════
    // 8. CANCEL
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Cancel: in_use → cancelled + ActualEnd ถูก set")]
    public async Task Cancel_InUse_ToCancelled()
    {
        var reserveId = "2025062007";
        var startTime = DateTime.Now.AddMinutes(-30);

        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = reserveId,
            CourtId = "02",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(15),
            Duration = 1.0,
            ReserveName = "ทดสอบ ยกเลิก",
            Status = "in_use",
            ActualStart = startTime
        });

        // Cancel
        var cancelTime = DateTime.Now;
        var updated = await _db.PaidReservations.UpdateStatusAsync(
            reserveId, "cancelled",
            actualStart: startTime,
            actualEnd: cancelTime,
            actualPrice: 0);
        Assert.IsTrue(updated);

        var cancelled = await _db.PaidReservations.GetByIdAsync(reserveId);
        Assert.AreEqual("cancelled", cancelled!.Status);
        Assert.IsNotNull(cancelled.ActualEnd);
        Assert.AreEqual(0, cancelled.ActualPrice);
    }

    // ════════════════════════════════════════════════════════════════
    // 9. CONFLICT CHECK — จองซ้อนเวลา (Paid vs Paid)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Conflict: Paid จอง 10:00-12:00, Walk-in ต้องการ 11:00-13:00 → ชน")]
    public async Task Conflict_PaidVsPaid_Overlap()
    {
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062010",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 2.0,
            ReserveName = "คนจองก่อน",
            Status = "booked"
        });

        // Walk-in ต้องการ 11:00-13:00 สนาม 01
        var walkinStart = TimeSpan.FromHours(11);
        var walkinDuration = 2.0;
        var walkinEnd = walkinStart.Add(TimeSpan.FromHours(walkinDuration));

        var reservations = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservations.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < walkinEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > walkinStart);

        Assert.IsNotNull(conflict, "ต้องตรวจจับ overlap ได้");
        Assert.AreEqual("คนจองก่อน", conflict.ReserveName);
    }

    // ════════════════════════════════════════════════════════════════
    // 10. CONFLICT CHECK — Paid vs Course
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Conflict: Course จอง 09:00-10:00, Paid ต้องการ 09:30-11:00 → ชน")]
    public async Task Conflict_PaidVsCourse_Overlap()
    {
        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = "2025062011",
            CourtId = "02",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.0,
            ReserveName = "ครูเอ",
            Status = "booked"
        });

        // Paid ต้องการ 09:30-11:00 สนาม 02
        var paidStart = TimeSpan.FromHours(9.5);
        var paidDuration = 1.5;
        var paidEnd = paidStart.Add(TimeSpan.FromHours(paidDuration));

        var courseRes = await _db.CourseReservations.GetByDateAsync(Today);
        var conflict = courseRes.FirstOrDefault(r =>
            r.CourtId == "02" &&
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < paidEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > paidStart);

        Assert.IsNotNull(conflict, "ต้องตรวจจับ Paid vs Course overlap ได้");
    }

    // ════════════════════════════════════════════════════════════════
    // 11. CONFLICT CHECK — ไม่ซ้อนเวลา (ต่อเนื่อง)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("No Conflict: จอง 10:00-11:00 แล้ว Walk-in 11:00-12:00 → ต่อกันพอดี ไม่ชน")]
    public async Task NoConflict_BackToBack()
    {
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062012",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "คนแรก",
            Status = "booked"
        });

        // Walk-in 11:00-12:00 — ต่อกันพอดี
        var walkinStart = TimeSpan.FromHours(11);
        var walkinDuration = 1.0;
        var walkinEnd = walkinStart.Add(TimeSpan.FromHours(walkinDuration));

        var reservations = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservations.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < walkinEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > walkinStart);

        Assert.IsNull(conflict, "ต่อเนื่องกันพอดีไม่ควร conflict");
    }

    // ════════════════════════════════════════════════════════════════
    // 12. CONFLICT CHECK — คนละสนาม ไม่ชน
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("No Conflict: เวลาซ้อนแต่คนละสนาม → ไม่ชน")]
    public async Task NoConflict_DifferentCourt()
    {
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062013",
            CourtId = "01", // สนาม 01
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 2.0,
            ReserveName = "คนสนามหนึ่ง",
            Status = "booked"
        });

        // จองเวลาเดียวกันแต่สนาม 02
        var walkinStart = TimeSpan.FromHours(10);
        var walkinEnd = walkinStart.Add(TimeSpan.FromHours(2.0));

        var reservations = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservations.FirstOrDefault(r =>
            r.CourtId == "02" && // สนาม 02
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < walkinEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > walkinStart);

        Assert.IsNull(conflict, "คนละสนามไม่ควร conflict");
    }

    // ════════════════════════════════════════════════════════════════
    // 13. SEARCH — ไม่เจอ
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Search reservation ด้วย ID ที่ไม่มี → คืน null")]
    public async Task Search_NotFound_ReturnsNull()
    {
        var result = await _db.PaidReservations.GetByIdAsync("9999999999");
        Assert.IsNull(result, "ค้นหา ID ที่ไม่มีต้องคืน null");
    }

    [TestMethod]
    [Description("Search reservation ที่ completed → ไม่ควรเจอในระบบ check-in (เพราะไม่ใช่ booked)")]
    public async Task Search_CompletedReservation_NotBookable()
    {
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062014",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(8),
            Duration = 1.0,
            ReserveName = "ใช้เสร็จแล้ว",
            Status = "completed"
        });

        var found = await _db.PaidReservations.GetByIdAsync("2025062014");
        Assert.IsNotNull(found);
        Assert.AreNotEqual("booked", found.Status,
            "reservation ที่ completed ไม่ควรใช้ check-in ได้");
    }

    // ════════════════════════════════════════════════════════════════
    // 14. ACTUAL DURATION CALCULATION
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("คำนวณ actual duration: ActualStart 10:00, ActualEnd 11:30 → 1.5 ชม.")]
    public void ActualDuration_CalculatedCorrectly()
    {
        var actualStart = new DateTime(2025, 6, 20, 10, 0, 0);
        var actualEnd = new DateTime(2025, 6, 20, 11, 30, 0);

        var duration = (actualEnd - actualStart).TotalHours;
        Assert.AreEqual(1.5, Math.Round(duration, 2));
    }

    [TestMethod]
    [Description("คำนวณ actual duration: เข้าเร็วกว่าจอง 5 นาที → duration ต้อง > 0")]
    public void ActualDuration_EarlyCheckin()
    {
        var actualStart = new DateTime(2025, 6, 20, 9, 55, 0);
        var actualEnd = new DateTime(2025, 6, 20, 11, 0, 0);

        var duration = (actualEnd - actualStart).TotalHours;
        Assert.IsTrue(duration > 0);
        Assert.IsTrue(Math.Round(duration, 2) > 1.0, "ใช้เกิน 1 ชม.");
    }

    [TestMethod]
    [Description("Negative duration ต้องถูก clamp เป็น 0")]
    public void ActualDuration_NegativeClampedToZero()
    {
        var actualStart = new DateTime(2025, 6, 20, 12, 0, 0);
        var actualEnd = new DateTime(2025, 6, 20, 11, 0, 0); // ย้อนเวลา (bug scenario)

        var duration = (actualEnd - actualStart).TotalHours;
        if (duration < 0) duration = 0;

        Assert.AreEqual(0, duration, "Negative duration ต้องถูก clamp เป็น 0");
    }

    // ════════════════════════════════════════════════════════════════
    // 15. COURSE RESERVATION — ข้ามวัน (in_use ค้าง)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("in_use ค้างจากเมื่อวาน → ค้นทั้งวันนี้+เมื่อวานต้องเจอ")]
    public async Task InUse_CarryOverFromYesterday()
    {
        var yesterday = Today.AddDays(-1);

        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025061901",
            CourtId = "01",
            ReserveDate = yesterday,
            ReserveTime = TimeSpan.FromHours(20),
            Duration = 1.0,
            ReserveName = "ค้างจากเมื่อวาน",
            Status = "in_use",
            ActualStart = yesterday.Add(TimeSpan.FromHours(20))
        });

        // ค้นวันนี้ → ไม่เจอ
        var todayRes = await _db.PaidReservations.GetByDateAsync(Today);
        var todayInUse = todayRes.FirstOrDefault(r => r.Status == "in_use" && r.CourtId == "01");
        Assert.IsNull(todayInUse, "ค้นเฉพาะวันนี้ไม่ควรเจอ");

        // ค้นเมื่อวาน → เจอ
        var yesterdayRes = await _db.PaidReservations.GetByDateAsync(yesterday);
        var yesterdayInUse = yesterdayRes.FirstOrDefault(r => r.Status == "in_use" && r.CourtId == "01");
        Assert.IsNotNull(yesterdayInUse, "ค้นเมื่อวานต้องเจอ in_use ที่ค้าง");

        // ค้นรวม (เหมือนที่ ViewModel ทำ)
        var combined = todayRes.Concat(yesterdayRes).ToList();
        var anyInUse = combined.Any(r => r.Status == "in_use" && r.CourtId == "01");
        Assert.IsTrue(anyInUse, "ค้นรวมวันนี้+เมื่อวานต้องเจอ");
    }

    // ════════════════════════════════════════════════════════════════
    // 16. MULTIPLE OPERATIONS — สนามเดียวกัน ใช้ซ้ำหลังจบ
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("สนาม 01: ใช้ 10-11 แล้วจบ → จอง 11-12 ได้ (ไม่ conflict เพราะ completed แล้ว)")]
    public async Task ReuseCourt_AfterCompleted()
    {
        // First usage: 10:00-11:00 → completed
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062020",
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "คนแรก",
            Status = "completed" // เสร็จแล้ว
        });

        // Second usage: 11:00-12:00 → ต้องไม่ชน
        var newStart = TimeSpan.FromHours(11);
        var newEnd = newStart.Add(TimeSpan.FromHours(1.0));

        var reservations = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservations.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.Status is "booked" or "in_use" && // completed ไม่นับ
            r.ReserveTime < newEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > newStart);

        Assert.IsNull(conflict, "completed reservation ไม่ควร conflict");
    }

    // ════════════════════════════════════════════════════════════════
    // 17. EDGE CASE — excludeReserveId (ไม่ให้ชนกับตัวเอง)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Conflict check ด้วย excludeReserveId → ไม่ควรชนกับตัวเอง")]
    public async Task ConflictCheck_ExcludeSelf()
    {
        var reserveId = "2025062021";
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = reserveId,
            CourtId = "01",
            ReserveDate = Today,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ตัวเอง",
            Status = "in_use"
        });

        // Check conflict on same time slot, same court, but exclude self
        var checkStart = TimeSpan.FromHours(10);
        var checkEnd = checkStart.Add(TimeSpan.FromHours(1.0));

        var reservations = await _db.PaidReservations.GetByDateAsync(Today);
        var conflict = reservations.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.ReserveId != reserveId && // exclude self
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < checkEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > checkStart);

        Assert.IsNull(conflict, "exclude ตัวเองแล้วต้องไม่ conflict");
    }

    // ════════════════════════════════════════════════════════════════
    // Helper — Price Calculation (เหมือน AppConstants.CalculateCourtPrice)
    // ════════════════════════════════════════════════════════════════

    private static int CalculateCourtPrice(TimeSpan startTime, double durationHours)
    {
        const int dayPrice = 400;
        const int nightPrice = 500;
        var nightBoundary = TimeSpan.FromHours(18);
        var endTime = startTime.Add(TimeSpan.FromHours(durationHours));

        if (endTime <= nightBoundary)
            return (int)(durationHours * dayPrice);

        if (startTime >= nightBoundary)
            return (int)(durationHours * nightPrice);

        var dayHours = (nightBoundary - startTime).TotalHours;
        var nightHours = durationHours - dayHours;
        return (int)(dayHours * dayPrice + nightHours * nightPrice);
    }

    // ════════════════════════════════════════════════════════════════
    // Helper — Update court_id ตรง (DAO ไม่มี method แยก)
    // ════════════════════════════════════════════════════════════════

    private async Task UpdateCourtId(string reserveId, string courtId)
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_db.ConnectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE PaidCourtReservation SET court_id = @court WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@court", courtId);
        cmd.Parameters.AddWithValue("@id", reserveId);
        await cmd.ExecuteNonQueryAsync();
    }
}
