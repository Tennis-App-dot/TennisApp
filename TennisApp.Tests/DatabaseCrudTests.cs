using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TennisApp.Tests.Data;
using TennisApp.Tests.Helpers;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Integration Tests สำหรับ Database CRUD — Court, Course, Trainer
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class DatabaseCrudTests
{
    private TestDatabaseHelper _db = null!;

    [TestInitialize]
    public void Setup()
    {
        _db = new TestDatabaseHelper();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db?.Dispose();
    }

    // ════════════════════════════════════════════════════════════════
    // Court CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("เพิ่มสนามแล้วค้นหาด้วย status = 1")]
    public async Task Court_AddAndGetByStatus()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");
        await _db.Courts.AddCourtAsync("03", "0");

        var available = await _db.Courts.GetCourtsByStatusAsync("1");
        Assert.AreEqual(2, available.Count);
        Assert.IsTrue(available.Any(c => c.CourtId == "01"));
        Assert.IsTrue(available.Any(c => c.CourtId == "02"));
    }

    [TestMethod]
    [Description("ค้นหาสนามปิดปรับปรุง")]
    public async Task Court_GetMaintenance()
    {
        await _db.Courts.AddCourtAsync("01", "0");
        await _db.Courts.AddCourtAsync("02", "1");

        var maintenance = await _db.Courts.GetCourtsByStatusAsync("0");
        // court "00" is default maintenance + our "01"
        Assert.IsTrue(maintenance.Any(c => c.CourtId == "01"));
    }

    [TestMethod]
    [Description("สนาม 00 ถูกสร้างอัตโนมัติ")]
    public async Task Court_DefaultCourt00_Exists()
    {
        var all = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.IsTrue(all.Any(c => c.CourtId == "00"), "สนาม 00 ต้องมีอยู่");
    }

    // ════════════════════════════════════════════════════════════════
    // Trainer CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("เพิ่ม Trainer → ต้องเจอใน Course table")]
    public async Task Trainer_AddTrainer()
    {
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ", "0812345678");
        // If no exception → success
    }

    [TestMethod]
    [Description("เพิ่ม Trainer ซ้ำ → ต้อง REPLACE ไม่ error")]
    public async Task Trainer_AddDuplicate_NoError()
    {
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ", "0812345678");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ Updated", "0899999999");
        // No exception = success
    }

    // ════════════════════════════════════════════════════════════════
    // Course CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("เพิ่ม Course → ต้องไม่ error")]
    public async Task Course_AddCourse()
    {
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult Class", 4, 1, 2200);
    }

    [TestMethod]
    [Description("เพิ่ม Course ซ้ำ → ต้อง REPLACE")]
    public async Task Course_AddDuplicate_NoError()
    {
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult Class", 4, 1, 2200);
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult Class Updated", 4, 1, 2500);
    }

    // ════════════════════════════════════════════════════════════════
    // PaidCourtReservation CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("เพิ่ม PaidReservation → ค้นหา → เจอ")]
    public async Task PaidReservation_AddAndGet()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        var reservation = new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 2.0,
            ReserveName = "สมชาย",
            Status = "booked"
        };

        var added = await _db.PaidReservations.AddAsync(reservation);
        Assert.IsTrue(added);

        var found = await _db.PaidReservations.GetByIdAsync("2025062001");
        Assert.IsNotNull(found);
        Assert.AreEqual("สมชาย", found.ReserveName);
        Assert.AreEqual(2.0, found.Duration);
        Assert.AreEqual("booked", found.Status);
    }

    [TestMethod]
    [Description("ค้นหา PaidReservation ที่ไม่มี → null")]
    public async Task PaidReservation_GetNotFound()
    {
        var found = await _db.PaidReservations.GetByIdAsync("9999999999");
        Assert.IsNull(found);
    }

    [TestMethod]
    [Description("ค้นหาด้วยวันที่ → ได้ list")]
    public async Task PaidReservation_GetByDate()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        var date = new DateTime(2025, 6, 20);

        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = date,
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "คนที่ 1",
            Status = "booked"
        });

        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062002",
            CourtId = "01",
            ReserveDate = date,
            ReserveTime = TimeSpan.FromHours(14),
            Duration = 1.0,
            ReserveName = "คนที่ 2",
            Status = "booked"
        });

        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062103",
            CourtId = "01",
            ReserveDate = date.AddDays(1), // คนละวัน
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "คนวันรุ่งขึ้น",
            Status = "booked"
        });

        var results = await _db.PaidReservations.GetByDateAsync(date);
        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    [Description("Update status จาก booked → in_use")]
    public async Task PaidReservation_UpdateStatus()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ทดสอบ",
            Status = "booked"
        });

        var startTime = DateTime.Now;
        var updated = await _db.PaidReservations.UpdateStatusAsync("2025062001", "in_use", actualStart: startTime);
        Assert.IsTrue(updated);

        var found = await _db.PaidReservations.GetByIdAsync("2025062001");
        Assert.AreEqual("in_use", found!.Status);
        Assert.IsNotNull(found.ActualStart);
    }

    [TestMethod]
    [Description("Update duration (extend)")]
    public async Task PaidReservation_UpdateDuration()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ทดสอบ",
            Status = "in_use"
        });

        await _db.PaidReservations.UpdateDurationAsync("2025062001", 2.5);

        var found = await _db.PaidReservations.GetByIdAsync("2025062001");
        Assert.AreEqual(2.5, found!.Duration);
    }

    // ════════════════════════════════════════════════════════════════
    // CourseCourtReservation CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("เพิ่ม CourseReservation → ค้นหา → เจอ")]
    public async Task CourseReservation_AddAndGet()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        var reservation = new TestCourseReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.0,
            ReserveName = "ครูเอ",
            Status = "booked"
        };

        var added = await _db.CourseReservations.AddAsync(reservation);
        Assert.IsTrue(added);

        var found = await _db.CourseReservations.GetByIdAsync("2025062001");
        Assert.IsNotNull(found);
        Assert.AreEqual("TA04", found.ClassId);
        Assert.AreEqual("T001", found.TrainerId);
    }

    [TestMethod]
    [Description("Update CourseReservation status")]
    public async Task CourseReservation_UpdateStatus()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.0,
            ReserveName = "ครูเอ",
            Status = "booked"
        });

        var startTime = DateTime.Now;
        await _db.CourseReservations.UpdateStatusAsync("2025062001", "in_use", actualStart: startTime);

        var found = await _db.CourseReservations.GetByIdAsync("2025062001");
        Assert.AreEqual("in_use", found!.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // PaidCourtUseLog CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Insert PaidUseLog → count")]
    public async Task PaidUseLog_InsertAndCount()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ทดสอบ",
            Status = "completed"
        });

        var logInserted = await _db.PaidUseLogs.InsertAsync("2025062001", "2025062001", DateTime.Now, 1.0, 400);
        Assert.IsTrue(logInserted);

        var count = await _db.PaidUseLogs.CountAllAsync();
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [Description("Get PaidUseLog by ReserveId")]
    public async Task PaidUseLog_GetByReserveId()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 2.0,
            ReserveName = "ทดสอบ",
            Status = "completed"
        });

        await _db.PaidUseLogs.InsertAsync("2025062001", "2025062001", DateTime.Now, 2.0, 800);

        var log = await _db.PaidUseLogs.GetByReserveIdAsync("2025062001");
        Assert.IsNotNull(log);
        Assert.AreEqual(2.0, log.Value.Duration);
        Assert.AreEqual(800, log.Value.Price);
        Assert.AreEqual("completed", log.Value.Status);
    }

    [TestMethod]
    [Description("Get PaidUseLog ที่ไม่มี → null")]
    public async Task PaidUseLog_GetNotFound()
    {
        var log = await _db.PaidUseLogs.GetByReserveIdAsync("9999999999");
        Assert.IsNull(log);
    }

    // ════════════════════════════════════════════════════════════════
    // CourseCourtUseLog CRUD
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Insert CourseUseLog → count")]
    public async Task CourseUseLog_InsertAndCount()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.0,
            ReserveName = "ครูเอ",
            Status = "completed"
        });

        var logInserted = await _db.CourseUseLogs.InsertAsync("2025062001", "2025062001", DateTime.Now, 1.0);
        Assert.IsTrue(logInserted);

        var count = await _db.CourseUseLogs.CountAllAsync();
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [Description("Get CourseUseLog by ReserveId")]
    public async Task CourseUseLog_GetByReserveId()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ClassId = "TA04",
            TrainerId = "T001",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(9),
            Duration = 1.5,
            ReserveName = "ครูเอ",
            Status = "completed"
        });

        await _db.CourseUseLogs.InsertAsync("2025062001", "2025062001", DateTime.Now, 1.5);

        var log = await _db.CourseUseLogs.GetByReserveIdAsync("2025062001");
        Assert.IsNotNull(log);
        Assert.AreEqual(1.5, log.Value.Duration);
        Assert.AreEqual("completed", log.Value.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // ClearAllData
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ClearAllData → ทุก table ว่างยกเว้น court 00")]
    public async Task ClearAllData_TablesEmpty()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "ครูเอ");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001",
            CourtId = "01",
            ReserveDate = new DateTime(2025, 6, 20),
            ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0,
            ReserveName = "ทดสอบ",
            Status = "booked"
        });

        _db.ClearAllData();

        // Court 00 ยังต้องอยู่
        var courts = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.IsTrue(courts.Any(c => c.CourtId == "00"));

        // ข้อมูลอื่นต้องหายหมด
        var available = await _db.Courts.GetCourtsByStatusAsync("1");
        Assert.AreEqual(0, available.Count);

        var reservation = await _db.PaidReservations.GetByIdAsync("2025062001");
        Assert.IsNull(reservation);
    }
}
