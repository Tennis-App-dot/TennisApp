using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TennisApp.Tests.Data;
using TennisApp.Tests.Helpers;

namespace TennisApp.Tests;

[TestClass]
public class DatabaseAdvancedTests
{
    private TestDatabaseHelper _db = null!;
    static readonly DateTime Today = new(2025, 6, 20);

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

    // Court availability check via SQL
    [TestMethod]
    public async Task CourtAvailable_NoBookings_IsAvailable()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        var avail = await _db.PaidReservations.IsCourtAvailableAsync("01", Today, TimeSpan.FromHours(10), 1.0);
        Assert.IsTrue(avail);
    }

    [TestMethod]
    public async Task CourtAvailable_HasBooking_NotAvailable()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001", CourtId = "01",
            ReserveDate = Today, ReserveTime = TimeSpan.FromHours(10),
            Duration = 2.0, ReserveName = "Existing", Status = "booked"
        });
        var avail = await _db.PaidReservations.IsCourtAvailableAsync("01", Today, TimeSpan.FromHours(11), 1.0);
        Assert.IsFalse(avail);
    }

    [TestMethod]
    public async Task CourtAvailable_BackToBack_IsAvailable()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001", CourtId = "01",
            ReserveDate = Today, ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0, ReserveName = "First", Status = "booked"
        });
        var avail = await _db.PaidReservations.IsCourtAvailableAsync("01", Today, TimeSpan.FromHours(11), 1.0);
        Assert.IsTrue(avail);
    }

    // Delete reservation
    [TestMethod]
    public async Task PaidReservation_Delete()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation
        {
            ReserveId = "2025062001", CourtId = "01",
            ReserveDate = Today, ReserveTime = TimeSpan.FromHours(10),
            Duration = 1.0, ReserveName = "ToDelete", Status = "booked"
        });

        var deleted = await _db.PaidReservations.DeleteAsync("2025062001");
        Assert.IsTrue(deleted);

        var found = await _db.PaidReservations.GetByIdAsync("2025062001");
        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task PaidReservation_Delete_NotFound()
    {
        var deleted = await _db.PaidReservations.DeleteAsync("9999999999");
        Assert.IsFalse(deleted);
    }

    // Count by date and status
    [TestMethod]
    public async Task PaidReservation_CountByDateAndStatus()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=1.0, ReserveName="A", Status="booked" });
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062002", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(14), Duration=1.0, ReserveName="B", Status="booked" });
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062003", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(16), Duration=1.0, ReserveName="C", Status="cancelled" });

        var count = await _db.PaidReservations.CountByDateAndStatusAsync(Today, "01", "booked");
        Assert.AreEqual(2, count);
    }

    // Course + Paid conflict on same court
    [TestMethod]
    public async Task CrossType_Conflict_PaidVsCourse()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "Coach");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        await _db.CourseReservations.AddAsync(new TestCourseReservation
        {
            ReserveId = "2025062001", CourtId = "01", ClassId = "TA04", TrainerId = "T001",
            ReserveDate = Today, ReserveTime = TimeSpan.FromHours(9), Duration = 1.0,
            ReserveName = "Coach", Status = "booked"
        });

        // Check paid on same slot
        var courseRes = await _db.CourseReservations.GetByDateAsync(Today);
        var paidStart = TimeSpan.FromHours(9);
        var paidEnd = paidStart.Add(TimeSpan.FromHours(1));
        var conflict = courseRes.FirstOrDefault(r =>
            r.CourtId == "01" &&
            r.Status is "booked" or "in_use" &&
            r.ReserveTime < paidEnd &&
            r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > paidStart);

        Assert.IsNotNull(conflict);
    }

    // Multiple courts - no conflict across courts
    [TestMethod]
    public async Task MultiCourt_NoConflict()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");

        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=2.0, ReserveName="Court1", Status="booked" });

        var avail = await _db.PaidReservations.IsCourtAvailableAsync("02", Today, TimeSpan.FromHours(10), 2.0);
        Assert.IsTrue(avail, "Different court should be available");
    }

    // Completed doesn't block new bookings
    [TestMethod]
    public async Task Completed_DoesNotBlock()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=1.0, ReserveName="Done", Status="completed" });

        var avail = await _db.PaidReservations.IsCourtAvailableAsync("01", Today, TimeSpan.FromHours(10), 1.0);
        Assert.IsTrue(avail, "Completed reservation should not block");
    }

    // Full flow: Book -> Start -> End -> Log -> Verify
    [TestMethod]
    public async Task FullFlow_BookToLog()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        var rid = "2025062010";

        // Book
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId=rid, CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(14), Duration=2.0, ReserveName="FullFlow", Status="booked" });
        var r = await _db.PaidReservations.GetByIdAsync(rid);
        Assert.AreEqual("booked", r!.Status);

        // Start
        var startTime = new DateTime(2025,6,20,14,0,0);
        await _db.PaidReservations.UpdateStatusAsync(rid, "in_use", actualStart: startTime);
        r = await _db.PaidReservations.GetByIdAsync(rid);
        Assert.AreEqual("in_use", r!.Status);

        // End
        var endTime = new DateTime(2025,6,20,16,0,0);
        await _db.PaidReservations.UpdateStatusAsync(rid, "completed", actualStart: startTime, actualEnd: endTime, actualPrice: 800);
        r = await _db.PaidReservations.GetByIdAsync(rid);
        Assert.AreEqual("completed", r!.Status);
        Assert.AreEqual(800, r.ActualPrice);

        // Log
        await _db.PaidUseLogs.InsertAsync(rid, rid, startTime, 2.0, 800);
        var log = await _db.PaidUseLogs.GetByReserveIdAsync(rid);
        Assert.IsNotNull(log);
        Assert.AreEqual(2.0, log.Value.Duration);
        Assert.AreEqual(800, log.Value.Price);
    }

    // ClearAllData preserves court 00
    [TestMethod]
    public async Task ClearAllData_PreservesCourt00()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");

        _db.ClearAllData();

        var maint = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.IsTrue(maint.Any(c => c.CourtId == "00"));

        var active = await _db.Courts.GetCourtsByStatusAsync("1");
        Assert.AreEqual(0, active.Count);
    }

    // ═══════════════════════════════════════════════════════════
    // ClearAllData — comprehensive verification per table
    // ═══════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ClearAllData_RemovesAllPaidReservations()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=1.0, ReserveName="A", Status="booked" });
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="2025062002", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(14), Duration=2.0, ReserveName="B", Status="in_use" });

        // Verify data exists before clear
        var before = await _db.PaidReservations.GetByDateAsync(Today);
        Assert.AreEqual(2, before.Count);

        _db.ClearAllData();

        var after = await _db.PaidReservations.GetByDateAsync(Today);
        Assert.AreEqual(0, after.Count, "All paid reservations should be deleted");
    }

    [TestMethod]
    public async Task ClearAllData_RemovesAllCourseReservations()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "Coach");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        await _db.CourseReservations.AddAsync(new TestCourseReservation { ReserveId="C025062001", CourtId="01", ClassId="TA04", TrainerId="T001", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(9), Duration=1.0, ReserveName="Student", Status="booked" });
        await _db.CourseReservations.AddAsync(new TestCourseReservation { ReserveId="C025062002", CourtId="01", ClassId="TA04", TrainerId="T001", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(11), Duration=1.0, ReserveName="Student2", Status="completed" });

        var before = await _db.CourseReservations.GetByDateAsync(Today);
        Assert.AreEqual(2, before.Count);

        _db.ClearAllData();

        var after = await _db.CourseReservations.GetByDateAsync(Today);
        Assert.AreEqual(0, after.Count, "All course reservations should be deleted");
    }

    [TestMethod]
    public async Task ClearAllData_RemovesAllPaidUseLogs()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        var rid = "2025062001";
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId=rid, CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=2.0, ReserveName="Test", Status="completed" });
        await _db.PaidUseLogs.InsertAsync(rid, rid, new DateTime(2025,6,20,10,0,0), 2.0, 800);

        var before = await _db.PaidUseLogs.CountAllAsync();
        Assert.AreEqual(1, before);

        _db.ClearAllData();

        var after = await _db.PaidUseLogs.CountAllAsync();
        Assert.AreEqual(0, after, "All paid use logs should be deleted");
    }

    [TestMethod]
    public async Task ClearAllData_RemovesAllCourseUseLogs()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courses.AddTrainerAsync("T001", "Coach");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");
        var rid = "C025062001";
        await _db.CourseReservations.AddAsync(new TestCourseReservation { ReserveId=rid, CourtId="01", ClassId="TA04", TrainerId="T001", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(9), Duration=1.0, ReserveName="Student", Status="completed" });
        await _db.CourseUseLogs.InsertAsync(rid, rid, new DateTime(2025,6,20,9,0,0), 1.0);

        var before = await _db.CourseUseLogs.CountAllAsync();
        Assert.AreEqual(1, before);

        _db.ClearAllData();

        var after = await _db.CourseUseLogs.CountAllAsync();
        Assert.AreEqual(0, after, "All course use logs should be deleted");
    }

    [TestMethod]
    public async Task ClearAllData_RemovesAllCourts_ExceptDummy00()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");
        await _db.Courts.AddCourtAsync("03", "0");

        _db.ClearAllData();

        var active = await _db.Courts.GetCourtsByStatusAsync("1");
        Assert.AreEqual(0, active.Count, "All active courts should be deleted");

        var maintenance = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.AreEqual(1, maintenance.Count, "Only dummy court 00 should remain");
        Assert.AreEqual("00", maintenance[0].CourtId);
    }

    [TestMethod]
    public async Task ClearAllData_FullPopulation_AllTablesEmpty()
    {
        // Populate every table
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.Courts.AddCourtAsync("02", "1");
        await _db.Courses.AddTrainerAsync("T001", "Coach");
        await _db.Courses.AddCourseAsync("TA04", "T001", "Adult");

        // Paid reservations + logs
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="P001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=2.0, ReserveName="PaidUser", Status="completed" });
        await _db.PaidUseLogs.InsertAsync("PL001", "P001", new DateTime(2025,6,20,10,0,0), 2.0, 800);

        // Course reservations + logs
        await _db.CourseReservations.AddAsync(new TestCourseReservation { ReserveId="C001", CourtId="02", ClassId="TA04", TrainerId="T001", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(14), Duration=1.0, ReserveName="Student", Status="completed" });
        await _db.CourseUseLogs.InsertAsync("CL001", "C001", new DateTime(2025,6,20,14,0,0), 1.0);

        // Verify everything populated
        Assert.AreEqual(2, (await _db.Courts.GetCourtsByStatusAsync("1")).Count);
        Assert.AreEqual(1, (await _db.PaidReservations.GetByDateAsync(Today)).Count);
        Assert.AreEqual(1, (await _db.CourseReservations.GetByDateAsync(Today)).Count);
        Assert.AreEqual(1, await _db.PaidUseLogs.CountAllAsync());
        Assert.AreEqual(1, await _db.CourseUseLogs.CountAllAsync());

        // Clear everything
        _db.ClearAllData();

        // Verify all tables are empty (except court 00)
        Assert.AreEqual(0, (await _db.Courts.GetCourtsByStatusAsync("1")).Count, "Active courts should be gone");
        Assert.AreEqual(1, (await _db.Courts.GetCourtsByStatusAsync("0")).Count, "Only court 00 remains");
        Assert.AreEqual(0, (await _db.PaidReservations.GetByDateAsync(Today)).Count, "Paid reservations should be gone");
        Assert.AreEqual(0, (await _db.CourseReservations.GetByDateAsync(Today)).Count, "Course reservations should be gone");
        Assert.AreEqual(0, await _db.PaidUseLogs.CountAllAsync(), "Paid use logs should be gone");
        Assert.AreEqual(0, await _db.CourseUseLogs.CountAllAsync(), "Course use logs should be gone");
    }

    [TestMethod]
    public async Task ClearAllData_CanReinsertAfterClear()
    {
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="P001", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=1.0, ReserveName="Before", Status="booked" });

        _db.ClearAllData();

        // Re-add court and reservation after clear
        await _db.Courts.AddCourtAsync("01", "1");
        await _db.PaidReservations.AddAsync(new TestPaidReservation { ReserveId="P002", CourtId="01", ReserveDate=Today, ReserveTime=TimeSpan.FromHours(10), Duration=1.0, ReserveName="After", Status="booked" });

        var res = await _db.PaidReservations.GetByIdAsync("P002");
        Assert.IsNotNull(res, "Should be able to insert new data after clear");
        Assert.AreEqual("After", res.ReserveName);

        // Old data should not exist
        var old = await _db.PaidReservations.GetByIdAsync("P001");
        Assert.IsNull(old, "Old data should not exist after clear");
    }

    [TestMethod]
    public async Task ClearAllData_Idempotent_CallingTwiceIsOk()
    {
        await _db.Courts.AddCourtAsync("01", "1");

        _db.ClearAllData();
        _db.ClearAllData(); // Second call should not throw

        var courts = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.IsTrue(courts.Any(c => c.CourtId == "00"), "Court 00 should survive double clear");
    }

    [TestMethod]
    public async Task ClearAllData_EmptyDatabase_NoErrors()
    {
        // Database has only the initial court 00 — clearing should not throw
        _db.ClearAllData();

        var courts = await _db.Courts.GetCourtsByStatusAsync("0");
        Assert.AreEqual(1, courts.Count);
        Assert.AreEqual("00", courts[0].CourtId);
    }
}
