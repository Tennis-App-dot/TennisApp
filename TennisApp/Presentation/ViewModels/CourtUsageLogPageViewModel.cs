using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Helpers;
using TennisApp.Data;

namespace TennisApp.Presentation.ViewModels;

/// <summary>
/// ViewModel สำหรับหน้าบันทึกการเข้าใช้งานสนาม (Court Usage Log Page)
/// รองรับทั้งการเข้าใช้จากการจองล่วงหน้าและ Walk-in
/// </summary>
public partial class CourtUsageLogPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    // ========================================================================
    // Collections
    // ========================================================================

    /// <summary>
    /// รายการบันทึกการใช้งานแบบเช่าทั้งหมด
    /// </summary>
    public ObservableCollection<PaidCourtUseLogItem> PaidUsageLogs { get; } = new();

    /// <summary>
    /// รายการบันทึกการใช้งานจากคอร์สทั้งหมด
    /// </summary>
    public ObservableCollection<CourseCourtUseLogItem> CourseUsageLogs { get; } = new();

    /// <summary>
    /// รายการสนามที่พร้อมใช้งาน (court_status = 1)
    /// </summary>
    public ObservableCollection<CourtItem> AvailableCourts { get; } = new();

    /// <summary>
    /// รายการคอร์สที่เปิดสอน
    /// </summary>
    public ObservableCollection<CourseItem> AvailableCourses { get; } = new();

    /// <summary>
    /// รายการการจองแบบเช่า (สำหรับ search)
    /// </summary>
    public ObservableCollection<PaidCourtReservationItem> PaidReservations { get; } = new();

    /// <summary>
    /// รายการการจองคอร์ส (สำหรับ search)
    /// </summary>
    public ObservableCollection<CourseCourtReservationItem> CourseReservations { get; } = new();

    /// <summary>
    /// สถานะสนามทั้งหมด (real-time) สำหรับตารางด้านล่าง
    /// </summary>
    public ObservableCollection<CourtStatusItem> CourtStatuses { get; } = new();

    // ========================================================================
    // Properties - Form Input
    // ========================================================================

    [ObservableProperty]
    private string _reserveIdSearch = string.Empty;

    [ObservableProperty]
    private string _usageType = "Paid"; // "Paid" or "Course"

    [ObservableProperty]
    private string _selectedReserveId = string.Empty;

    [ObservableProperty]
    private string _selectedCourtId = string.Empty;

    [ObservableProperty]
    private string _selectedCourseId = string.Empty;

    [ObservableProperty]
    private DateTime _usageDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _usageTime = TimeSpan.FromHours(8); // Default 08:00

    [ObservableProperty]
    private double _usageDuration = 1.0; // Default 1 hour

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private int _calculatedPrice = 0;

    [ObservableProperty]
    private bool _isFromReservation = false;

    [ObservableProperty]
    private bool _isWalkIn = true;

    // ========================================================================
    // Properties - Detail Card
    // ========================================================================

    [ObservableProperty]
    private CourtStatusItem? _selectedCourtStatus;

    [ObservableProperty]
    private bool _isDetailCardVisible = false;

    [ObservableProperty]
    private double _extendHours = 1.0;

    [ObservableProperty]
    private int _endUsagePrice = 0;

    // ========================================================================
    // Display Properties
    // ========================================================================

    /// <summary>
    /// แสดงเวลาสิ้นสุดที่คาดการณ์
    /// </summary>
    public string EstimatedEndTime
    {
        get
        {
            var endTime = UsageTime.Add(TimeSpan.FromHours(UsageDuration));
            return endTime.ToString(@"hh\:mm");
        }
    }

    /// <summary>
    /// แสดง/ซ่อนส่วนเลือกคอร์ส (สำหรับ Course Usage)
    /// </summary>
    public bool IsCourseUsage => UsageType == "Course";

    /// <summary>
    /// แสดง/ซ่อนส่วนคำนวณราคา (สำหรับ Paid Usage)
    /// </summary>
    public bool IsPaidUsage => UsageType == "Paid";

    // ========================================================================
    // Constructor
    // ========================================================================

    public CourtUsageLogPageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("🎾 CourtUsageLogPageViewModel constructor เริ่มทำงาน");

        try
        {
            _databaseService = new DatabaseService();
            _databaseService.EnsureInitialized();
            System.Diagnostics.Debug.WriteLine("✅ DatabaseService สร้างสำเร็จ");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CourtUsageLogPageViewModel constructor error: {ex.Message}");
        }
    }

    // ========================================================================
    // Load Data Commands
    // ========================================================================

    /// <summary>
    /// โหลดข้อมูลบันทึกการใช้งานทั้งหมด
    /// </summary>
    [RelayCommand]
    public async Task LoadUsageLogsAsync()
    {
        try
        {
            _databaseService.EnsureInitialized();
            System.Diagnostics.Debug.WriteLine("📥 LoadUsageLogsAsync เริ่มทำงาน...");

            var paidLogsTask = _databaseService.PaidCourtUseLogs.GetAllAsync();
            var courseLogsTask = _databaseService.CourseCourtUseLogs.GetAllAsync();

            await Task.WhenAll(paidLogsTask, courseLogsTask);

            var paidLogs = paidLogsTask.Result;
            var courseLogs = courseLogsTask.Result;

            PaidUsageLogs.Clear();
            foreach (var log in paidLogs)
            {
                PaidUsageLogs.Add(log);
            }

            CourseUsageLogs.Clear();
            foreach (var log in courseLogs)
            {
                CourseUsageLogs.Add(log);
            }

            System.Diagnostics.Debug.WriteLine($"✅ LoadUsageLogsAsync เสร็จสิ้น - Paid: {PaidUsageLogs.Count}, Course: {CourseUsageLogs.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading usage logs: {ex.Message}");
        }
    }

    /// <summary>
    /// โหลดรายการการจองทั้งหมด (สำหรับ search)
    /// </summary>
    [RelayCommand]
    public async Task LoadReservationsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("📥 LoadReservationsAsync เริ่มทำงาน...");

            var paidTask = _databaseService.PaidCourtReservations.GetAllReservationsAsync();
            var courseTask = _databaseService.CourseCourtReservations.GetAllReservationsAsync();

            await Task.WhenAll(paidTask, courseTask);

            var paidReservations = paidTask.Result;
            var courseReservations = courseTask.Result;

            PaidReservations.Clear();
            foreach (var reservation in paidReservations)
            {
                PaidReservations.Add(reservation);
            }

            CourseReservations.Clear();
            foreach (var reservation in courseReservations)
            {
                CourseReservations.Add(reservation);
            }

            System.Diagnostics.Debug.WriteLine($"✅ LoadReservationsAsync เสร็จสิ้น");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
        }
    }

    /// <summary>
    /// โหลดรายการสนามที่พร้อมใช้งาน
    /// </summary>
    [RelayCommand]
    public async Task LoadAvailableCourtsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🎾 LoadAvailableCourtsAsync เริ่มทำงาน...");

            var courts = await _databaseService.Courts.GetCourtsByStatusAsync("1");

            AvailableCourts.Clear();
            foreach (var court in courts)
            {
                AvailableCourts.Add(court);
            }

            System.Diagnostics.Debug.WriteLine($"✅ LoadAvailableCourtsAsync เสร็จสิ้น");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courts: {ex.Message}");
        }
    }

    /// <summary>
    /// โหลดรายการคอร์สที่เปิดสอน
    /// </summary>
    [RelayCommand]
    public async Task LoadAvailableCoursesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("📚 LoadAvailableCoursesAsync เริ่มทำงาน...");

            var courses = await _databaseService.Courses.GetAllCoursesAsync();

            AvailableCourses.Clear();
            foreach (var course in courses)
            {
                AvailableCourses.Add(course);
            }

            System.Diagnostics.Debug.WriteLine($"✅ LoadAvailableCoursesAsync เสร็จสิ้น");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courses: {ex.Message}");
        }
    }

    // ========================================================================
    // Court Status - Real-time
    // ========================================================================

    /// <summary>
    /// โหลดสถานะสนามทั้งหมด: ดูจาก Court + UseLog วันนี้ที่ยัง in_use
    /// </summary>
    [RelayCommand]
    public async Task LoadCourtStatusesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🏟️ LoadCourtStatusesAsync เริ่มทำงาน...");

            var courtsTask = _databaseService.Courts.GetCourtsByStatusAsync("1");
            var paidLogsTask = _databaseService.PaidCourtUseLogs.GetAllAsync();
            var courseLogsTask = _databaseService.CourseCourtUseLogs.GetAllAsync();

            await Task.WhenAll(courtsTask, paidLogsTask, courseLogsTask);

            var courts = courtsTask.Result;
            var paidLogs = paidLogsTask.Result;
            var courseLogs = courseLogsTask.Result;

            var today = DateTime.Today;

            CourtStatuses.Clear();

            foreach (var court in courts)
            {
                var status = new CourtStatusItem
                {
                    CourtId = court.CourtID,
                    IsInUse = false
                };

                // ตรวจสอบ Paid UseLog ที่ยัง in_use (status = "in_use") วันนี้
                var activePaidLog = paidLogs.FirstOrDefault(l =>
                    l.CourtId == court.CourtID &&
                    l.LogStatus == "in_use" &&
                    l.CheckInTime.Date == today);

                if (activePaidLog != null)
                {
                    status.IsInUse = true;
                    status.UserName = activePaidLog.ReserveName;
                    status.UserPhone = activePaidLog.ReservePhone;
                    status.UsageType = "Paid";
                    status.StartTime = activePaidLog.ReserveTime;
                    status.Duration = activePaidLog.LogDuration;
                    status.Price = activePaidLog.LogPrice;
                    status.LogId = activePaidLog.LogId;
                    status.ReserveId = activePaidLog.ReserveId;
                    status.LogStatus = activePaidLog.LogStatus;
                }

                // ตรวจสอบ Course UseLog ที่ยัง in_use วันนี้
                var activeCourseLog = courseLogs.FirstOrDefault(l =>
                    l.CourtId == court.CourtID &&
                    l.LogStatus == "in_use" &&
                    l.CheckInTime.Date == today);

                if (activeCourseLog != null)
                {
                    status.IsInUse = true;
                    status.UserName = activeCourseLog.ReserveName;
                    status.UserPhone = activeCourseLog.ReservePhone;
                    status.UsageType = "Course";
                    status.CourseTitle = activeCourseLog.ClassTitle;
                    status.StartTime = activeCourseLog.ReserveTime;
                    status.Duration = activeCourseLog.LogDuration;
                    status.Price = 0;
                    status.LogId = activeCourseLog.LogId;
                    status.ReserveId = activeCourseLog.ReserveId;
                    status.LogStatus = activeCourseLog.LogStatus;
                }

                CourtStatuses.Add(status);
            }

            System.Diagnostics.Debug.WriteLine($"✅ LoadCourtStatusesAsync เสร็จสิ้น - {CourtStatuses.Count} สนาม, ใช้งาน: {CourtStatuses.Count(s => s.IsInUse)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading court statuses: {ex.Message}");
        }
    }

    // ========================================================================
    // Search Reservation
    // ========================================================================

    /// <summary>
    /// ค้นหาการจองด้วยรหัส Reserve ID
    /// </summary>
    [RelayCommand]
    public async Task<bool> SearchReservationAsync(string reserveId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 SearchReservationAsync: {reserveId}");

            if (string.IsNullOrWhiteSpace(reserveId))
                return false;

            // Try Paid Reservation first
            var paidReservation = await _databaseService.PaidCourtReservations
                .GetReservationByIdAsync(reserveId);

            if (paidReservation != null && paidReservation.Status == "booked")
            {
                UsageType = "Paid";
                SelectedReserveId = paidReservation.ReserveId;
                SelectedCourtId = paidReservation.CourtId;
                UsageDate = paidReservation.ReserveDate;
                UsageTime = paidReservation.ReserveTime;
                UsageDuration = paidReservation.Duration;
                CustomerName = paidReservation.ReserveName;
                CustomerPhone = paidReservation.ReservePhone;
                IsFromReservation = true;
                IsWalkIn = false;
                CalculatedPrice = (int)(UsageDuration * 200);
                return true;
            }

            // Try Course Reservation
            var courseReservation = await _databaseService.CourseCourtReservations
                .GetReservationByIdAsync(reserveId);

            if (courseReservation != null && courseReservation.Status == "booked")
            {
                UsageType = "Course";
                SelectedReserveId = courseReservation.ReserveId;
                SelectedCourtId = courseReservation.CourtId;
                SelectedCourseId = courseReservation.ClassId;
                UsageDate = courseReservation.ReserveDate;
                UsageTime = courseReservation.ReserveTime;
                UsageDuration = courseReservation.Duration;
                CustomerName = courseReservation.ReserveName;
                CustomerPhone = courseReservation.ReservePhone;
                IsFromReservation = true;
                IsWalkIn = false;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error searching reservation: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Court Availability Check
    // ========================================================================

    /// <summary>
    /// หาสนามที่ว่างในช่วงเวลาที่เลือก
    /// </summary>
    public async Task<List<CourtItem>> GetAvailableCourtsForTimeSlotAsync()
    {
        var availableCourts = new List<CourtItem>();

        foreach (var court in AvailableCourts)
        {
            var isAvailable = await IsCourtAvailableAsync(court.CourtID, UsageDate, UsageTime, UsageDuration);
            if (isAvailable)
            {
                availableCourts.Add(court);
            }
        }

        return availableCourts;
    }

    /// <summary>
    /// ตรวจสอบว่าสนามว่างในช่วงเวลาที่เลือกหรือไม่
    /// </summary>
    private async Task<bool> IsCourtAvailableAsync(string courtId, DateTime usageDate, TimeSpan usageTime, double duration)
    {
        try
        {
            var isPaidAvailable = await _databaseService.PaidCourtReservations
                .IsCourtAvailableAsync(courtId, usageDate, usageTime, duration);
            if (!isPaidAvailable) return false;

            var isCourseAvailable = await _databaseService.CourseCourtReservations
                .IsCourtAvailableAsync(courtId, usageDate, usageTime, duration);
            return isCourseAvailable;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking court availability: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Log Usage (Check-in)
    // ========================================================================

    /// <summary>
    /// บันทึกเข้าใช้งาน Paid — ถ้า Walk-in จะสร้าง Reservation อัตโนมัติ
    /// UseLog status = "in_use" (ยังไม่ complete จนกว่าจะกดสิ้นสุด)
    /// </summary>
    [RelayCommand]
    public async Task<bool> LogPaidUsageAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"➕ LogPaidUsageAsync: {CustomerName}");

            // Walk-in: สร้าง Reservation อัตโนมัติ
            if (IsWalkIn || string.IsNullOrEmpty(SelectedReserveId))
            {
                var reserveId = await ReservationIdGenerator.GeneratePaidReservationIdAsync(_databaseService, DateTime.Now);
                var reservation = new PaidCourtReservationItem
                {
                    ReserveId = reserveId,
                    CourtId = SelectedCourtId,
                    RequestDate = DateTime.Now,
                    ReserveDate = UsageDate,
                    ReserveTime = UsageTime,
                    Duration = UsageDuration,
                    ReserveName = CustomerName,
                    ReservePhone = CustomerPhone,
                    Status = "in_use"
                };

                var reserveSuccess = await _databaseService.PaidCourtReservations.AddReservationAsync(reservation);
                if (!reserveSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("❌ สร้าง Reservation สำหรับ Walk-in ล้มเหลว");
                    return false;
                }

                SelectedReserveId = reserveId;
            }
            else
            {
                // มีการจองอยู่แล้ว → เปลี่ยน status เป็น in_use + อัปเดต court_id ให้ตรงกับสนามที่เลือก
                var reservation = await _databaseService.PaidCourtReservations
                    .GetReservationByIdAsync(SelectedReserveId);
                if (reservation != null)
                {
                    reservation.Status = "in_use";
                    reservation.CourtId = SelectedCourtId;
                    await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);
                }
            }

            // สร้าง UseLog
            var logId = await ReservationIdGenerator.GeneratePaidUseLogIdAsync(_databaseService, DateTime.Now);

            var log = new PaidCourtUseLogItem
            {
                LogId = logId,
                ReserveId = SelectedReserveId,
                CourtId = SelectedCourtId,
                CheckInTime = DateTime.Now,
                LogDuration = UsageDuration,
                LogPrice = CalculatedPrice,
                LogStatus = "in_use",
                ReserveName = CustomerName,
                ReservePhone = CustomerPhone,
                ReserveDate = UsageDate,
                ReserveTime = UsageTime,
                ReserveDuration = UsageDuration
            };

            var success = await _databaseService.PaidCourtUseLogs.InsertAsync(log);

            if (success)
            {
                await LoadUsageLogsAsync();
                await LoadCourtStatusesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ บันทึกเข้าใช้งาน (in_use) สำเร็จ: {logId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error logging paid usage: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// บันทึกเข้าใช้งาน Course — ถ้า Walk-in จะสร้าง Reservation อัตโนมัติ
    /// </summary>
    [RelayCommand]
    public async Task<bool> LogCourseUsageAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"➕ LogCourseUsageAsync: {CustomerName}");

            if (IsWalkIn || string.IsNullOrEmpty(SelectedReserveId))
            {
                var reserveId = await ReservationIdGenerator.GenerateCourseReservationIdAsync(_databaseService, DateTime.Now);
                var reservation = new CourseCourtReservationItem
                {
                    ReserveId = reserveId,
                    CourtId = SelectedCourtId,
                    ClassId = SelectedCourseId,
                    RequestDate = DateTime.Now,
                    ReserveDate = UsageDate,
                    ReserveTime = UsageTime,
                    Duration = UsageDuration,
                    ReserveName = CustomerName,
                    ReservePhone = CustomerPhone,
                    Status = "in_use"
                };

                var reserveSuccess = await _databaseService.CourseCourtReservations.AddReservationAsync(reservation);
                if (!reserveSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("❌ สร้าง Course Reservation สำหรับ Walk-in ล้มเหลว");
                    return false;
                }

                SelectedReserveId = reserveId;
            }
            else
            {
                // มีการจองอยู่แล้ว → เปลี่ยน status เป็น in_use + อัปเดต court_id ให้ตรงกับสนามที่เลือก
                var reservation = await _databaseService.CourseCourtReservations
                    .GetReservationByIdAsync(SelectedReserveId);
                if (reservation != null)
                {
                    reservation.Status = "in_use";
                    reservation.CourtId = SelectedCourtId;
                    await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);
                }
            }

            var logId = await ReservationIdGenerator.GenerateCourseUseLogIdAsync(_databaseService, DateTime.Now);

            var log = new CourseCourtUseLogItem
            {
                LogId = logId,
                ReserveId = SelectedReserveId,
                CourtId = SelectedCourtId,
                ClassId = SelectedCourseId,
                CheckInTime = DateTime.Now,
                LogDuration = UsageDuration,
                LogStatus = "in_use",
                ReserveName = CustomerName,
                ReservePhone = CustomerPhone,
                ReserveDate = UsageDate,
                ReserveTime = UsageTime,
                ClassTitle = AvailableCourses.FirstOrDefault(c => c.ClassId == SelectedCourseId)?.ClassTitle ?? ""
            };

            var success = await _databaseService.CourseCourtUseLogs.InsertAsync(log);

            if (success)
            {
                await LoadUsageLogsAsync();
                await LoadCourtStatusesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ บันทึกเข้าใช้งาน Course (in_use) สำเร็จ: {logId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error logging course usage: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Detail Card Actions
    // ========================================================================

    /// <summary>
    /// แสดง Detail Card สำหรับสนามที่กำลังใช้งาน
    /// </summary>
    public void ShowDetailCard(CourtStatusItem courtStatus)
    {
        if (courtStatus == null || !courtStatus.IsInUse) return;

        SelectedCourtStatus = courtStatus;
        EndUsagePrice = courtStatus.Price;
        ExtendHours = 1.0;
        IsDetailCardVisible = true;
    }

    /// <summary>
    /// ซ่อน Detail Card
    /// </summary>
    public void HideDetailCard()
    {
        IsDetailCardVisible = false;
        SelectedCourtStatus = null;
    }

    /// <summary>
    /// ตรวจสอบว่าการขยายเวลาจะชนกับการจองถัดไปในสนามเดียวกันหรือไม่
    /// คืน null ถ้าไม่ชน, คืนข้อความถ้าชน
    /// </summary>
    public async Task<string?> CheckExtendConflictAsync()
    {
        if (SelectedCourtStatus == null) return null;

        try
        {
            var courtId = SelectedCourtStatus.CourtId;
            var startTime = SelectedCourtStatus.StartTime;
            var newDuration = SelectedCourtStatus.Duration + ExtendHours;
            var newEndTime = startTime.Add(TimeSpan.FromHours(newDuration));
            var today = DateTime.Today;

            // เช็คการจอง Paid ที่ชน (ยกเว้นตัวเอง) — ทั้ง booked และ in_use
            var paidReservations = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(today);
            var conflictPaid = paidReservations.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                (r.Status == "booked" || r.Status == "in_use") &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflictPaid != null)
            {
                var conflictEndTime = conflictPaid.ReserveTime.Add(TimeSpan.FromHours(conflictPaid.Duration));
                return $"การขยายเวลาจะชนกับการจอง {conflictPaid.ReserveId}\n" +
                       $"ผู้จอง: {conflictPaid.ReserveName}\n" +
                       $"เวลา: {conflictPaid.ReserveTime:hh\\:mm} - {conflictEndTime:hh\\:mm}";
            }

            // เช็คการจอง Course ที่ชน (ยกเว้นตัวเอง) — ทั้ง booked และ in_use
            var courseReservations = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(today);
            var conflictCourse = courseReservations.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                (r.Status == "booked" || r.Status == "in_use") &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflictCourse != null)
            {
                var conflictEndTime = conflictCourse.ReserveTime.Add(TimeSpan.FromHours(conflictCourse.Duration));
                return $"การขยายเวลาจะชนกับการจองคอร์ส {conflictCourse.ReserveId}\n" +
                       $"ผู้จอง: {conflictCourse.ReserveName}\n" +
                       $"คอร์ส: {conflictCourse.ClassDisplayName}\n" +
                       $"เวลา: {conflictCourse.ReserveTime:hh\\:mm} - {conflictEndTime:hh\\:mm}";
            }

            return null; // ไม่ชน
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking extend conflict: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// ขยายระยะเวลาการใช้งาน
    /// อัปเดตทั้ง UseLog.LogDuration และ Reservation.Duration ให้ตรงกัน
    /// </summary>
    [RelayCommand]
    public async Task<bool> ExtendUsageTimeAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            var newDuration = SelectedCourtStatus.Duration + ExtendHours;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var log = PaidUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogDuration = newDuration;
                    var success = await _databaseService.PaidCourtUseLogs.UpdateAsync(log);
                    if (success)
                    {
                        // อัปเดต Reservation.Duration ให้ตรงกับ UseLog
                        var reservation = await _databaseService.PaidCourtReservations
                            .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                        if (reservation != null)
                        {
                            reservation.Duration = newDuration;
                            await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);
                        }

                        SelectedCourtStatus.Duration = newDuration;
                        await LoadCourtStatusesAsync();
                        return true;
                    }
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var log = CourseUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogDuration = newDuration;
                    var success = await _databaseService.CourseCourtUseLogs.UpdateAsync(log);
                    if (success)
                    {
                        // อัปเดต Reservation.Duration ให้ตรงกับ UseLog
                        var reservation = await _databaseService.CourseCourtReservations
                            .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                        if (reservation != null)
                        {
                            reservation.Duration = newDuration;
                            await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);
                        }

                        SelectedCourtStatus.Duration = newDuration;
                        await LoadCourtStatusesAsync();
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error extending usage time: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// สิ้นสุดการใช้งาน → status = "completed" + บันทึกค่าบริการ
    /// </summary>
    [RelayCommand]
    public async Task<bool> EndUsageAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            bool success = false;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var log = PaidUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogStatus = "completed";
                    log.LogPrice = EndUsagePrice;
                    success = await _databaseService.PaidCourtUseLogs.UpdateAsync(log);

                    if (success)
                    {
                        await _databaseService.PaidCourtReservations.UpdateStatusAsync(
                            SelectedCourtStatus.ReserveId, "completed");
                    }
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var log = CourseUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogStatus = "completed";
                    success = await _databaseService.CourseCourtUseLogs.UpdateAsync(log);

                    if (success)
                    {
                        await _databaseService.CourseCourtReservations.UpdateStatusAsync(
                            SelectedCourtStatus.ReserveId, "completed");
                    }
                }
            }

            if (success)
            {
                HideDetailCard();
                await LoadCourtStatusesAsync();
                await LoadUsageLogsAsync();
                System.Diagnostics.Debug.WriteLine($"✅ สิ้นสุดการใช้งานสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error ending usage: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ยกเลิกการใช้งาน → status = "cancelled"
    /// </summary>
    [RelayCommand]
    public async Task<bool> CancelUsageAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            bool success = false;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var log = PaidUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogStatus = "cancelled";
                    log.LogPrice = 0;
                    success = await _databaseService.PaidCourtUseLogs.UpdateAsync(log);

                    if (success)
                    {
                        await _databaseService.PaidCourtReservations.UpdateStatusAsync(
                            SelectedCourtStatus.ReserveId, "cancelled");
                    }
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var log = CourseUsageLogs.FirstOrDefault(l => l.LogId == SelectedCourtStatus.LogId);
                if (log != null)
                {
                    log.LogStatus = "cancelled";
                    success = await _databaseService.CourseCourtUseLogs.UpdateAsync(log);

                    if (success)
                    {
                        await _databaseService.CourseCourtReservations.UpdateStatusAsync(
                            SelectedCourtStatus.ReserveId, "cancelled");
                    }
                }
            }

            if (success)
            {
                HideDetailCard();
                await LoadCourtStatusesAsync();
                await LoadUsageLogsAsync();
                System.Diagnostics.Debug.WriteLine($"✅ ยกเลิกการใช้งานสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cancelling usage: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// อัปเดตบันทึกการใช้งานแบบเช่า (backward compatibility)
    /// </summary>
    [RelayCommand]
    public async Task<bool> UpdatePaidUsageAsync(PaidCourtUseLogItem log)
    {
        try
        {
            var success = await _databaseService.PaidCourtUseLogs.UpdateAsync(log);

            if (success)
            {
                var existing = PaidUsageLogs.FirstOrDefault(l => l.LogId == log.LogId);
                if (existing != null)
                {
                    var index = PaidUsageLogs.IndexOf(existing);
                    PaidUsageLogs[index] = log;
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating paid usage: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    /// <summary>
    /// ล้างข้อมูลฟอร์ม
    /// </summary>
    [RelayCommand]
    public void ClearForm()
    {
        ReserveIdSearch = string.Empty;
        SelectedReserveId = string.Empty;
        SelectedCourtId = string.Empty;
        SelectedCourseId = string.Empty;
        UsageDate = DateTime.Today;
        UsageTime = TimeSpan.FromHours(8);
        UsageDuration = 1.0;
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;
        CalculatedPrice = 0;
        IsFromReservation = false;
        IsWalkIn = true;
    }

    /// <summary>
    /// คำนวณราคา (สำหรับ Paid Usage)
    /// อัตรา: 200 บาท/ชั่วโมง (สามารถปรับได้)
    /// </summary>
    [RelayCommand]
    public void CalculatePrice()
    {
        const int pricePerHour = 200;
        CalculatedPrice = (int)(UsageDuration * pricePerHour);
    }

    // ========================================================================
    // Property Change Notifications
    // ========================================================================

    partial void OnUsageTimeChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(EstimatedEndTime));
    }

    partial void OnUsageDurationChanged(double value)
    {
        OnPropertyChanged(nameof(EstimatedEndTime));
        if (IsPaidUsage)
        {
            CalculatePrice();
        }
    }

    partial void OnUsageTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCourseUsage));
        OnPropertyChanged(nameof(IsPaidUsage));
    }
}
