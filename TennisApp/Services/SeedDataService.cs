using System;
using System.Threading.Tasks;
using TennisApp.Models;

namespace TennisApp.Services;

/// <summary>
/// Seed demo data สำหรับดู UI — จะ seed เฉพาะเมื่อ database ว่างเปล่า
/// </summary>
public static class SeedDataService
{
    /// <summary>
    /// ลบข้อมูลทั้งหมดแล้ว seed ใหม่ — ใช้ตอนต้องการ force reset
    /// </summary>
    public static async Task ResetAndSeedAsync(DatabaseService db)
    {
        System.Diagnostics.Debug.WriteLine("🔄 SeedData: Force reset + re-seed...");
        await db.ResetDatabaseAsync();
        await Helpers.CoursePricingHelper.LoadFromDatabaseAsync(db);
        await SeedIfEmptyAsync(db);
    }

    /// <summary>
    /// Seed ข้อมูลตัวอย่างถ้า database ยังว่าง (ไม่มีสนามจริง)
    /// </summary>
    public static async Task SeedIfEmptyAsync(DatabaseService db)
    {
        db.EnsureInitialized();

        // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่ (ไม่นับ dummy court "00")
        var courts = await db.Courts.GetAllCourtsAsync();
        if (courts.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine("📦 SeedData: มีข้อมูลอยู่แล้ว — ข้าม seed");
            return;
        }

        System.Diagnostics.Debug.WriteLine("🌱 SeedData: เริ่ม seed ข้อมูลตัวอย่าง...");

        // ════════════════════════════════════════════
        // 1. Courts (สนาม 4 สนาม)
        // ════════════════════════════════════════════
        var courtIds = new[] { "01", "02", "03", "04" };
        foreach (var id in courtIds)
        {
            await db.Courts.AddCourtAsync(new CourtItem
            {
                CourtID = id,
                Status = id == "04" ? "0" : "1", // สนาม 04 ปิดปรับปรุง
                MaintenanceDate = DateTime.Today.AddDays(-10),
                LastUpdated = DateTime.Now
            });
        }
        System.Diagnostics.Debug.WriteLine("   ✅ Courts: 4 สนาม (3 เปิด, 1 ปิดปรับปรุง)");

        // ════════════════════════════════════════════
        // 2. Trainers (ผู้ฝึกสอน 3 คน)
        // ════════════════════════════════════════════
        var trainers = new[]
        {
            new TrainerItem { TrainerId = "220250001", FirstName = "สมชาย", LastName = "มีสุข", Nickname = "ครูมี", BirthDate = new DateTime(1985, 3, 15), Phone = "0812345678" },
            new TrainerItem { TrainerId = "220250002", FirstName = "วิภา", LastName = "สว่างใจ", Nickname = "ครูเอ", BirthDate = new DateTime(1990, 7, 22), Phone = "0898765432" },
            new TrainerItem { TrainerId = "220250003", FirstName = "ธนกร", LastName = "เจริญรุ่ง", Nickname = "โค้ชบอล", BirthDate = new DateTime(1988, 11, 5), Phone = "0865554433" },
        };
        foreach (var t in trainers)
            await db.Trainers.AddTrainerAsync(t);
        System.Diagnostics.Debug.WriteLine("   ✅ Trainers: 3 คน");

        // ════════════════════════════════════════════
        // 3. Trainees (ผู้เรียน 6 คน)
        // ════════════════════════════════════════════
        var trainees = new[]
        {
            new TraineeItem { TraineeId = "120250001", FirstName = "ณัฐพล", LastName = "รักเรียน", Nickname = "เปิ้ล", BirthDate = new DateTime(2000, 1, 10), Phone = "0911112222" },
            new TraineeItem { TraineeId = "120250002", FirstName = "สุดา", LastName = "ใจดี", Nickname = "ดา", BirthDate = new DateTime(1998, 5, 20), Phone = "0922223333" },
            new TraineeItem { TraineeId = "120250003", FirstName = "ปิยะ", LastName = "ชัยชนะ", Nickname = "แบงค์", BirthDate = new DateTime(2005, 8, 30), Phone = "0933334444" },
            new TraineeItem { TraineeId = "120250004", FirstName = "อรอนงค์", LastName = "แสงทอง", Nickname = "น้ำ", BirthDate = new DateTime(2003, 12, 1), Phone = "0944445555" },
            new TraineeItem { TraineeId = "120250005", FirstName = "กิตติ", LastName = "พัฒนา", Nickname = "กิต", BirthDate = new DateTime(2010, 4, 15), Phone = "0955556666" },
            new TraineeItem { TraineeId = "120250006", FirstName = "พิมพ์ใจ", LastName = "สุขสม", Nickname = "พิม", BirthDate = new DateTime(2012, 9, 25), Phone = "0966667777" },
        };
        foreach (var t in trainees)
            await db.Trainees.AddTraineeAsync(t);
        System.Diagnostics.Debug.WriteLine("   ✅ Trainees: 6 คน");

        // ════════════════════════════════════════════
        // 4. Courses (คอร์ส 5 คอร์ส)
        // ════════════════════════════════════════════
        var courses = new[]
        {
            CourseItem.FromDatabase("TA04", "Adult", 4, 1, 2200, "220250001", "สมชาย มีสุข"),
            CourseItem.FromDatabase("TA08", "Adult", 8, 1, 4000, "220250002", "วิภา สว่างใจ"),
            CourseItem.FromDatabase("T104", "Red & Orange Ball", 4, 1, 1800, "220250001", "สมชาย มีสุข"),
            CourseItem.FromDatabase("T208", "Intermediate", 8, 1, 4800, "220250003", "ธนกร เจริญรุ่ง"),
            CourseItem.FromDatabase("P101", "Private Kru Mee", 1, 1, 2500, "220250001", "สมชาย มีสุข"),
        };
        foreach (var c in courses)
            await db.Courses.AddCourseAsync(c);
        System.Diagnostics.Debug.WriteLine("   ✅ Courses: 5 คอร์ส");

        // ════════════════════════════════════════════
        // 5. Class Registrations (สมัครคอร์ส 8 รายการ)
        // ════════════════════════════════════════════
        var registrations = new[]
        {
            new ClassRegisRecordItem { TraineeId = "120250001", ClassId = "TA04", TrainerId = "220250001", RegisDate = DateTime.Today.AddDays(-20) },
            new ClassRegisRecordItem { TraineeId = "120250002", ClassId = "TA04", TrainerId = "220250001", RegisDate = DateTime.Today.AddDays(-18) },
            new ClassRegisRecordItem { TraineeId = "120250003", ClassId = "T104", TrainerId = "220250001", RegisDate = DateTime.Today.AddDays(-15) },
            new ClassRegisRecordItem { TraineeId = "120250004", ClassId = "TA08", TrainerId = "220250002", RegisDate = DateTime.Today.AddDays(-12) },
            new ClassRegisRecordItem { TraineeId = "120250005", ClassId = "T104", TrainerId = "220250001", RegisDate = DateTime.Today.AddDays(-10) },
            new ClassRegisRecordItem { TraineeId = "120250006", ClassId = "T208", TrainerId = "220250003", RegisDate = DateTime.Today.AddDays(-8) },
            new ClassRegisRecordItem { TraineeId = "120250001", ClassId = "T208", TrainerId = "220250003", RegisDate = DateTime.Today.AddDays(-5) },
            new ClassRegisRecordItem { TraineeId = "120250002", ClassId = "P101", TrainerId = "220250001", RegisDate = DateTime.Today.AddDays(-3) },
        };
        foreach (var r in registrations)
            await db.Registrations.AddRegistrationAsync(r);
        System.Diagnostics.Debug.WriteLine("   ✅ Registrations: 8 รายการ");

        // ════════════════════════════════════════════
        // 6. Paid Court Reservations (จองเช่าสนาม 10 รายการ)
        //    Flow จริง: จองใหม่ → CourtId="00" (รอจัดสรร)
        //               check-in → จัดสรรสนามจริง + status="in_use"
        //               สิ้นสุด  → status="completed"
        // ════════════════════════════════════════════
        var today = DateTime.Today;
        var paidReservations = new[]
        {
            // วันนี้ — booked (ยังไม่ check-in → CourtId="00" รอจัดสรร)
            new PaidCourtReservationItem { ReserveId = $"{today:yyyyMMdd}01", CourtId = "00", RequestDate = today.AddDays(-1), ReserveDate = today, ReserveTime = new TimeSpan(9, 0, 0), Duration = 2.0, ReserveName = "คุณวีระ", ReservePhone = "0811111111", Status = "booked" },
            new PaidCourtReservationItem { ReserveId = $"{today:yyyyMMdd}02", CourtId = "00", RequestDate = today.AddDays(-1), ReserveDate = today, ReserveTime = new TimeSpan(10, 0, 0), Duration = 1.0, ReserveName = "คุณสมศรี", ReservePhone = "0822222222", Status = "booked" },
            new PaidCourtReservationItem { ReserveId = $"{today:yyyyMMdd}03", CourtId = "00", RequestDate = today, ReserveDate = today, ReserveTime = new TimeSpan(14, 0, 0), Duration = 1.5, ReserveName = "คุณพิชัย", ReservePhone = "0833333333", Status = "booked" },
            // อนาคต — booked (ยังไม่ check-in → CourtId="00" รอจัดสรร)
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(1):yyyyMMdd}01", CourtId = "00", RequestDate = today, ReserveDate = today.AddDays(1), ReserveTime = new TimeSpan(8, 0, 0), Duration = 2.0, ReserveName = "คุณอนันต์", ReservePhone = "0844444444", Status = "booked" },
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(2):yyyyMMdd}01", CourtId = "00", RequestDate = today, ReserveDate = today.AddDays(2), ReserveTime = new TimeSpan(16, 0, 0), Duration = 1.0, ReserveName = "คุณนิดา", ReservePhone = "0855555555", Status = "booked" },
            // อดีต — completed (ผ่าน check-in → จัดสรรสนามจริงแล้ว + มี ActualStart/ActualEnd)
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(-3):yyyyMMdd}01", CourtId = "01", RequestDate = today.AddDays(-5), ReserveDate = today.AddDays(-3), ReserveTime = new TimeSpan(9, 0, 0), Duration = 2.0, ReserveName = "คุณวีระ", ReservePhone = "0811111111", Status = "completed", ActualStart = today.AddDays(-3).Add(new TimeSpan(9, 5, 0)), ActualEnd = today.AddDays(-3).Add(new TimeSpan(11, 2, 0)), ActualPrice = 400 },
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(-3):yyyyMMdd}02", CourtId = "02", RequestDate = today.AddDays(-5), ReserveDate = today.AddDays(-3), ReserveTime = new TimeSpan(10, 0, 0), Duration = 1.0, ReserveName = "คุณสมศรี", ReservePhone = "0822222222", Status = "completed", ActualStart = today.AddDays(-3).Add(new TimeSpan(10, 0, 0)), ActualEnd = today.AddDays(-3).Add(new TimeSpan(11, 0, 0)), ActualPrice = 200 },
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(-7):yyyyMMdd}01", CourtId = "03", RequestDate = today.AddDays(-10), ReserveDate = today.AddDays(-7), ReserveTime = new TimeSpan(14, 0, 0), Duration = 1.5, ReserveName = "คุณพิชัย", ReservePhone = "0833333333", Status = "completed", ActualStart = today.AddDays(-7).Add(new TimeSpan(14, 0, 0)), ActualEnd = today.AddDays(-7).Add(new TimeSpan(15, 30, 0)), ActualPrice = 300 },
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(-14):yyyyMMdd}01", CourtId = "01", RequestDate = today.AddDays(-16), ReserveDate = today.AddDays(-14), ReserveTime = new TimeSpan(8, 0, 0), Duration = 2.0, ReserveName = "คุณอนันต์", ReservePhone = "0844444444", Status = "completed", ActualStart = today.AddDays(-14).Add(new TimeSpan(8, 10, 0)), ActualEnd = today.AddDays(-14).Add(new TimeSpan(10, 5, 0)), ActualPrice = 400 },
            // ยกเลิก (ยกเลิกก่อนจัดสรร → CourtId="00")
            new PaidCourtReservationItem { ReserveId = $"{today.AddDays(-5):yyyyMMdd}01", CourtId = "00", RequestDate = today.AddDays(-7), ReserveDate = today.AddDays(-5), ReserveTime = new TimeSpan(15, 0, 0), Duration = 1.0, ReserveName = "คุณนิดา", ReservePhone = "0855555555", Status = "cancelled" },
        };
        foreach (var r in paidReservations)
            await db.PaidCourtReservations.AddReservationAsync(r);
        System.Diagnostics.Debug.WriteLine("   ✅ PaidReservations: 10 รายการ (booked=CourtId 00, completed=สนามจริง)");

        // ════════════════════════════════════════════
        // 7. Course Court Reservations (จองคอร์ส 6 รายการ)
        //    Flow เดียวกัน: จองใหม่ → CourtId="00"
        //                   check-in → จัดสรรสนามจริง
        // ════════════════════════════════════════════
        var courseReservations = new[]
        {
            // วันนี้ — booked (ยังไม่ check-in → CourtId="00" รอจัดสรร)
            new CourseCourtReservationItem { ReserveId = $"{today:yyyyMMdd}04", CourtId = "00", ClassId = "TA04", TrainerId = "220250001", RequestDate = today.AddDays(-2), ReserveDate = today, ReserveTime = new TimeSpan(8, 0, 0), Duration = 1, ReserveName = "ณัฐพล รักเรียน", ReservePhone = "0911112222", ClassTitle = "Adult", Status = "booked" },
            // อนาคต — booked (ยังไม่ check-in → CourtId="00" รอจัดสรร)
            new CourseCourtReservationItem { ReserveId = $"{today.AddDays(1):yyyyMMdd}02", CourtId = "00", ClassId = "T104", TrainerId = "220250001", RequestDate = today, ReserveDate = today.AddDays(1), ReserveTime = new TimeSpan(10, 0, 0), Duration = 1, ReserveName = "ปิยะ ชัยชนะ", ReservePhone = "0933334444", ClassTitle = "Red & Orange Ball", Status = "booked" },
            // อดีต — completed (ผ่าน check-in → จัดสรรสนามจริงแล้ว + มี ActualStart/ActualEnd)
            new CourseCourtReservationItem { ReserveId = $"{today.AddDays(-2):yyyyMMdd}01", CourtId = "01", ClassId = "TA04", TrainerId = "220250001", RequestDate = today.AddDays(-4), ReserveDate = today.AddDays(-2), ReserveTime = new TimeSpan(9, 0, 0), Duration = 1, ReserveName = "สุดา ใจดี", ReservePhone = "0922223333", ClassTitle = "Adult", Status = "completed", ActualStart = today.AddDays(-2).Add(new TimeSpan(9, 0, 0)), ActualEnd = today.AddDays(-2).Add(new TimeSpan(10, 0, 0)) },
            new CourseCourtReservationItem { ReserveId = $"{today.AddDays(-6):yyyyMMdd}01", CourtId = "02", ClassId = "T208", TrainerId = "220250003", RequestDate = today.AddDays(-8), ReserveDate = today.AddDays(-6), ReserveTime = new TimeSpan(14, 0, 0), Duration = 1, ReserveName = "อรอนงค์ แสงทอง", ReservePhone = "0944445555", ClassTitle = "Intermediate", Status = "completed", ActualStart = today.AddDays(-6).Add(new TimeSpan(14, 0, 0)), ActualEnd = today.AddDays(-6).Add(new TimeSpan(15, 0, 0)) },
            new CourseCourtReservationItem { ReserveId = $"{today.AddDays(-10):yyyyMMdd}01", CourtId = "03", ClassId = "T104", TrainerId = "220250001", RequestDate = today.AddDays(-12), ReserveDate = today.AddDays(-10), ReserveTime = new TimeSpan(10, 0, 0), Duration = 1, ReserveName = "กิตติ พัฒนา", ReservePhone = "0955556666", ClassTitle = "Red & Orange Ball", Status = "completed", ActualStart = today.AddDays(-10).Add(new TimeSpan(10, 0, 0)), ActualEnd = today.AddDays(-10).Add(new TimeSpan(11, 0, 0)) },
            // ยกเลิก (ยกเลิกก่อนจัดสรร → CourtId="00")
            new CourseCourtReservationItem { ReserveId = $"{today.AddDays(-4):yyyyMMdd}01", CourtId = "00", ClassId = "TA08", TrainerId = "220250002", RequestDate = today.AddDays(-6), ReserveDate = today.AddDays(-4), ReserveTime = new TimeSpan(16, 0, 0), Duration = 1, ReserveName = "พิมพ์ใจ สุขสม", ReservePhone = "0966667777", ClassTitle = "Adult", Status = "cancelled" },
        };
        foreach (var r in courseReservations)
            await db.CourseCourtReservations.AddReservationAsync(r);
        System.Diagnostics.Debug.WriteLine("   ✅ CourseReservations: 6 รายการ (booked=CourtId 00, completed=สนามจริง)");

        // ════════════════════════════════════════════
        // 8. Paid Court Use Logs (บันทึกใช้งานสนาม — completed เท่านั้น)
        // ════════════════════════════════════════════
        var paidLogItems = new PaidCourtUseLogItem[]
        {
            new() { LogId = $"{today.AddDays(-3):yyyyMMdd}01", ReserveId = $"{today.AddDays(-3):yyyyMMdd}01", CheckInTime = today.AddDays(-3).Add(new TimeSpan(9, 5, 0)), LogDuration = 1.95, LogPrice = 400, LogStatus = "completed" },
            new() { LogId = $"{today.AddDays(-3):yyyyMMdd}02", ReserveId = $"{today.AddDays(-3):yyyyMMdd}02", CheckInTime = today.AddDays(-3).Add(new TimeSpan(10, 0, 0)), LogDuration = 1.0, LogPrice = 200, LogStatus = "completed" },
            new() { LogId = $"{today.AddDays(-7):yyyyMMdd}01", ReserveId = $"{today.AddDays(-7):yyyyMMdd}01", CheckInTime = today.AddDays(-7).Add(new TimeSpan(14, 0, 0)), LogDuration = 1.5, LogPrice = 300, LogStatus = "completed" },
            new() { LogId = $"{today.AddDays(-14):yyyyMMdd}01", ReserveId = $"{today.AddDays(-14):yyyyMMdd}01", CheckInTime = today.AddDays(-14).Add(new TimeSpan(8, 10, 0)), LogDuration = 1.92, LogPrice = 400, LogStatus = "completed" },
        };
        foreach (var log in paidLogItems)
            await db.PaidCourtUseLogs.InsertAsync(log);
        System.Diagnostics.Debug.WriteLine("   ✅ PaidUseLogs: 4 รายการ");

        // ════════════════════════════════════════════
        // 9. Course Court Use Logs (บันทึกใช้งานสนามคอร์ส — completed เท่านั้น)
        // ════════════════════════════════════════════
        var courseLogItems = new CourseCourtUseLogItem[]
        {
            new() { LogId = $"{today.AddDays(-2):yyyyMMdd}01", ReserveId = $"{today.AddDays(-2):yyyyMMdd}01", CheckInTime = today.AddDays(-2).Add(new TimeSpan(9, 0, 0)), LogDuration = 1.0, LogStatus = "completed" },
            new() { LogId = $"{today.AddDays(-6):yyyyMMdd}01", ReserveId = $"{today.AddDays(-6):yyyyMMdd}01", CheckInTime = today.AddDays(-6).Add(new TimeSpan(14, 0, 0)), LogDuration = 1.0, LogStatus = "completed" },
            new() { LogId = $"{today.AddDays(-10):yyyyMMdd}01", ReserveId = $"{today.AddDays(-10):yyyyMMdd}01", CheckInTime = today.AddDays(-10).Add(new TimeSpan(10, 0, 0)), LogDuration = 1.0, LogStatus = "completed" },
        };
        foreach (var log in courseLogItems)
            await db.CourseCourtUseLogs.InsertAsync(log);
        System.Diagnostics.Debug.WriteLine("   ✅ CourseUseLogs: 3 รายการ");

        System.Diagnostics.Debug.WriteLine("🌱 SeedData: เสร็จสิ้น — ข้อมูลตัวอย่างพร้อมแสดงผล UI");
    }
}
