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

namespace TennisApp.Presentation.ViewModels;

/// <summary>
/// ViewModel สำหรับหน้าบันทึกการเข้าใช้งานสนาม (Court Usage Log Page)
/// ศูนย์ควบคุม: ค้นหาจอง → Start/Stop/Extend + Walk-in + สถานะสนาม + ประวัติวันนี้
/// </summary>
public partial class CourtUsageLogPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService = null!;

    // ========================================================================
    // Collections
    // ========================================================================

    public ObservableCollection<CourtStatusItem> CourtStatuses { get; } = new();
    public ObservableCollection<CourtItem> AvailableCourts { get; } = new();
    public ObservableCollection<CourseItem> AvailableCourses { get; } = new();
    public ObservableCollection<PaidCourtUseLogItem> PaidUsageLogs { get; } = new();
    public ObservableCollection<CourseCourtUseLogItem> CourseUsageLogs { get; } = new();

    // ========================================================================
    // Properties - Walk-in Form
    // ========================================================================

    [ObservableProperty]
    private string _reserveIdSearch = string.Empty;

    [ObservableProperty]
    private string _usageType = "Paid";

    [ObservableProperty]
    private string _selectedCourtId = string.Empty;

    [ObservableProperty]
    private string _selectedCourseId = string.Empty;

    [ObservableProperty]
    private DateTime _usageDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _usageTime = TimeSpan.FromHours(8);

    [ObservableProperty]
    private double _usageDuration = 1.0;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private int _calculatedPrice;

    [ObservableProperty]
    private bool _isFromReservation;

    [ObservableProperty]
    private bool _isWalkIn = true;

    [ObservableProperty]
    private string _selectedReserveId = string.Empty;

    // ========================================================================
    // Properties - Detail Card
    // ========================================================================

    [ObservableProperty]
    private CourtStatusItem? _selectedCourtStatus;

    [ObservableProperty]
    private bool _isDetailCardVisible;

    [ObservableProperty]
    private double _extendHours = 1.0;

    [ObservableProperty]
    private int _endUsagePrice;

    // ========================================================================
    // Display Properties
    // ========================================================================

    public string EstimatedEndTime
    {
        get
        {
            var endTime = UsageTime.Add(TimeSpan.FromHours(UsageDuration));
            return endTime.ToString(@"hh\:mm");
        }
    }

    public bool IsCourseUsage => UsageType == "Course";
    public bool IsPaidUsage => UsageType == "Paid";

    // ========================================================================
    // Constructor
    // ========================================================================

    public CourtUsageLogPageViewModel() : this(((App)Microsoft.UI.Xaml.Application.Current).DatabaseService) { }

    public CourtUsageLogPageViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _databaseService.EnsureInitialized();
        System.Diagnostics.Debug.WriteLine("✅ CourtUsageLogPageViewModel created via DI");
    }

    // ========================================================================
    // Load Data
    // ========================================================================

    [RelayCommand]
    public async Task LoadAvailableCourtsAsync()
    {
        try
        {
            var courts = await _databaseService.Courts.GetCourtsByStatusAsync("1");
            AvailableCourts.Clear();
            foreach (var c in courts) AvailableCourts.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadAvailableCourts: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadAvailableCoursesAsync()
    {
        try
        {
            var courses = await _databaseService.Courses.GetAllCoursesAsync();
            AvailableCourses.Clear();
            foreach (var c in courses) AvailableCourses.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadAvailableCourses: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadUsageLogsAsync()
    {
        try
        {
            var paidLogs = await _databaseService.PaidCourtUseLogs.GetAllAsync();
            var courseLogs = await _databaseService.CourseCourtUseLogs.GetAllAsync();

            PaidUsageLogs.Clear();
            foreach (var l in paidLogs) PaidUsageLogs.Add(l);

            CourseUsageLogs.Clear();
            foreach (var l in courseLogs) CourseUsageLogs.Add(l);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadUsageLogs: {ex.Message}");
        }
    }

    // ========================================================================
    // Court Status — Real-time (ดูจาก Reservation ที่ status=in_use วันนี้)
    // ========================================================================

    [RelayCommand]
    public async Task LoadCourtStatusesAsync()
    {
        try
        {
            var courts = await _databaseService.Courts.GetCourtsByStatusAsync("1");
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            // ✅ ดึงทั้งวันนี้และเมื่อวาน เพื่อจับ in_use ที่ค้างข้ามวัน
            var paidResToday = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(today);
            var courseResToday = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(today);
            var paidResYesterday = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(yesterday);
            var courseResYesterday = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(yesterday);

            var paidRes = paidResToday.Concat(paidResYesterday).ToList();
            var courseRes = courseResToday.Concat(courseResYesterday).ToList();

            CourtStatuses.Clear();

            foreach (var court in courts)
            {
                var status = new CourtStatusItem { CourtId = court.CourtID, IsInUse = false };

                var activePaid = paidRes.FirstOrDefault(r =>
                    r.CourtId == court.CourtID && r.Status == "in_use");

                if (activePaid != null)
                {
                    status.IsInUse = true;
                    status.UserName = activePaid.ReserveName;
                    status.UserPhone = activePaid.ReservePhone;
                    status.UsageType = "Paid";
                    status.StartTime = activePaid.ReserveTime;
                    status.Duration = activePaid.Duration;
                    status.Price = activePaid.ActualPrice > 0
                        ? activePaid.ActualPrice.Value
                        : AppConstants.CalculateCourtPrice(activePaid.ReserveTime, activePaid.Duration);
                    status.ReserveId = activePaid.ReserveId;
                    status.LogStatus = "in_use";
                    status.ActualStartTime = activePaid.ActualStart;
                }

                var activeCourse = courseRes.FirstOrDefault(r =>
                    r.CourtId == court.CourtID && r.Status == "in_use");

                if (activeCourse != null)
                {
                    status.IsInUse = true;
                    status.UserName = activeCourse.ReserveName;
                    status.UserPhone = activeCourse.ReservePhone;
                    status.UsageType = "Course";
                    status.CourseTitle = activeCourse.ClassTitle;
                    status.StartTime = activeCourse.ReserveTime;
                    status.Duration = activeCourse.Duration;
                    status.Price = 0;
                    status.ReserveId = activeCourse.ReserveId;
                    status.LogStatus = "in_use";
                    status.ActualStartTime = activeCourse.ActualStart;
                }

                CourtStatuses.Add(status);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadCourtStatuses: {ex.Message}");
        }
    }

    // ========================================================================
    // Search Reservation
    // ========================================================================

    [RelayCommand]
    public async Task<bool> SearchReservationAsync(string reserveId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reserveId)) return false;

            var paid = await _databaseService.PaidCourtReservations.GetReservationByIdAsync(reserveId);
            if (paid != null && paid.Status == "booked")
            {
                UsageType = "Paid";
                SelectedReserveId = paid.ReserveId;
                UsageDate = paid.ReserveDate;
                UsageTime = paid.ReserveTime;
                UsageDuration = paid.Duration;
                CustomerName = paid.ReserveName;
                CustomerPhone = paid.ReservePhone;
                IsFromReservation = true;
                IsWalkIn = false;
                CalculatedPrice = AppConstants.CalculateCourtPrice(UsageTime, UsageDuration);
                return true;
            }

            var course = await _databaseService.CourseCourtReservations.GetReservationByIdAsync(reserveId);
            if (course != null && course.Status == "booked")
            {
                UsageType = "Course";
                SelectedReserveId = course.ReserveId;
                SelectedCourseId = course.ClassId;
                UsageDate = course.ReserveDate;
                UsageTime = course.ReserveTime;
                UsageDuration = course.Duration;
                CustomerName = course.ReserveName;
                CustomerPhone = course.ReservePhone;
                IsFromReservation = true;
                IsWalkIn = false;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SearchReservation: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Court Conflict Check — ใช้ก่อนเช็คอินทุกครั้ง (ทั้งจองและ Walk-in)
    // ========================================================================

    /// <summary>
    /// ตรวจสอบว่าสนามที่เลือกมีการจอง/ใช้งานซ้อนทับหรือไม่
    /// คืน null = ว่าง, คืน string = ข้อความ conflict
    /// 
    /// ตรวจ 2 กรณี:
    /// 1. สนามยัง in_use อยู่ (ค้างจากวันใดก็ตาม — ยังไม่สิ้นสุด)
    /// 2. มี booked/in_use ที่เวลาซ้อนทับในวันเดียวกัน
    /// </summary>
    public async Task<string?> CheckCourtConflictForCheckinAsync(
        string courtId, DateTime date, TimeSpan startTime, double duration, string? excludeReserveId = null)
    {
        try
        {
            var endTime = startTime.Add(TimeSpan.FromHours(duration));
            var exclude = excludeReserveId ?? "";

            // ════════════════════════════════════════════════════════════
            // ขั้นที่ 1: ตรวจว่าสนามนี้ยัง "in_use" อยู่หรือไม่ (ทุกวัน)
            //   → ถ้ายังมีคนใช้อยู่ ห้ามเช็คอินซ้ำเด็ดขาด
            // ════════════════════════════════════════════════════════════

            // Check Paid in_use (any date)
            var allPaidRes = await _databaseService.PaidCourtReservations.GetReservationsByCourtAsync(courtId);
            var activeInUsePaid = allPaidRes.FirstOrDefault(r =>
                r.ReserveId != exclude &&
                r.Status == "in_use");

            if (activeInUsePaid != null)
            {
                var cEnd = activeInUsePaid.ReserveTime.Add(TimeSpan.FromHours(activeInUsePaid.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"ผู้ใช้: {activeInUsePaid.ReserveName}\n" +
                       $"วันที่: {activeInUsePaid.ReserveDate:dd/MM/yyyy}\n" +
                       $"เวลา: {activeInUsePaid.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: กำลังใช้งาน";
            }

            // Check Course in_use (any date)
            var allCourseRes = await _databaseService.CourseCourtReservations.GetReservationsByCourtAsync(courtId);
            var activeInUseCourse = allCourseRes.FirstOrDefault(r =>
                r.ReserveId != exclude &&
                r.Status == "in_use");

            if (activeInUseCourse != null)
            {
                var cEnd = activeInUseCourse.ReserveTime.Add(TimeSpan.FromHours(activeInUseCourse.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"คอร์ส: {activeInUseCourse.ClassTitle}\n" +
                       $"ผู้ใช้: {activeInUseCourse.ReserveName}\n" +
                       $"วันที่: {activeInUseCourse.ReserveDate:dd/MM/yyyy}\n" +
                       $"เวลา: {activeInUseCourse.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: กำลังใช้งาน";
            }

            // ════════════════════════════════════════════════════════════
            // ขั้นที่ 2: ตรวจ booked ที่เวลาซ้อนทับในวันเดียวกัน
            // ════════════════════════════════════════════════════════════

            // Check Paid booked (same date, time overlap)
            var paidRes = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(date);
            var paidConflict = paidRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != exclude &&
                r.Status == "booked" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (paidConflict != null)
            {
                var cEnd = paidConflict.ReserveTime.Add(TimeSpan.FromHours(paidConflict.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"ผู้จอง: {paidConflict.ReserveName}\n" +
                       $"เวลา: {paidConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: จองแล้ว";
            }

            // Check Course booked (same date, time overlap)
            var courseRes = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(date);
            var courseConflict = courseRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != exclude &&
                r.Status == "booked" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (courseConflict != null)
            {
                var cEnd = courseConflict.ReserveTime.Add(TimeSpan.FromHours(courseConflict.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"คอร์ส: {courseConflict.ClassTitle}\n" +
                       $"ผู้จอง: {courseConflict.ReserveName}\n" +
                       $"เวลา: {courseConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: จองแล้ว";
            }

            return null; // ว่าง!
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CheckCourtConflict: {ex.Message}");
            return null;
        }
    }

    // ========================================================================
    // Court Availability
    // ========================================================================

    public async Task<List<CourtItem>> GetAvailableCourtsForTimeSlotAsync()
    {
        var available = new List<CourtItem>();
        foreach (var court in AvailableCourts)
        {
            var isPaidOk = await _databaseService.PaidCourtReservations
                .IsCourtAvailableAsync(court.CourtID, UsageDate, UsageTime, UsageDuration);
            var isCourseOk = await _databaseService.CourseCourtReservations
                .IsCourtAvailableAsync(court.CourtID, UsageDate, UsageTime, UsageDuration);
            if (isPaidOk && isCourseOk) available.Add(court);
        }
        return available;
    }

    // ========================================================================
    // START — เริ่มใช้งาน
    // ========================================================================

    [RelayCommand]
    public async Task<bool> StartPaidUsageAsync()
    {
        try
        {
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
                    Status = "in_use",
                    ActualStart = DateTime.Now,
                    ActualPrice = AppConstants.CalculateCourtPrice(UsageTime, UsageDuration)
                };
                if (!await _databaseService.PaidCourtReservations.AddReservationAsync(reservation))
                    return false;
                SelectedReserveId = reserveId;
            }
            else
            {
                var reservation = await _databaseService.PaidCourtReservations.GetReservationByIdAsync(SelectedReserveId);
                if (reservation == null) return false;
                reservation.Status = "in_use";
                reservation.CourtId = SelectedCourtId;
                reservation.ActualStart = DateTime.Now;
                reservation.ActualPrice = AppConstants.CalculateCourtPrice(reservation.ReserveTime, reservation.Duration);
                await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);
            }

            await LoadCourtStatusesAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ StartPaidUsage: {ex.Message}");
            return false;
        }
    }

    [RelayCommand]
    public async Task<bool> StartCourseUsageAsync()
    {
        try
        {
            if (IsWalkIn || string.IsNullOrEmpty(SelectedReserveId))
            {
                // ✅ ดึง trainer_id จาก Course table เพื่อให้ INNER JOIN ทำงานได้
                var trainerId = string.Empty;
                if (!string.IsNullOrEmpty(SelectedCourseId))
                {
                    var course = await _databaseService.Courses.GetCourseByIdAsync(SelectedCourseId);
                    if (course != null)
                        trainerId = course.TrainerId;
                }

                var reserveId = await ReservationIdGenerator.GenerateCourseReservationIdAsync(_databaseService, DateTime.Now);
                var reservation = new CourseCourtReservationItem
                {
                    ReserveId = reserveId,
                    CourtId = SelectedCourtId,
                    ClassId = SelectedCourseId,
                    TrainerId = trainerId,
                    RequestDate = DateTime.Now,
                    ReserveDate = UsageDate,
                    ReserveTime = UsageTime,
                    Duration = UsageDuration,
                    ReserveName = CustomerName,
                    ReservePhone = CustomerPhone,
                    Status = "in_use",
                    ActualStart = DateTime.Now
                };
                if (!await _databaseService.CourseCourtReservations.AddReservationAsync(reservation))
                    return false;
                SelectedReserveId = reserveId;
            }
            else
            {
                var reservation = await _databaseService.CourseCourtReservations.GetReservationByIdAsync(SelectedReserveId);
                if (reservation == null) return false;
                reservation.Status = "in_use";
                reservation.CourtId = SelectedCourtId;
                reservation.ActualStart = DateTime.Now;
                await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);
            }

            await LoadCourtStatusesAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ StartCourseUsage: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // STOP — สิ้นสุดใช้งาน → completed + actual_end + INSERT UseLog
    // ========================================================================

    [RelayCommand]
    public async Task<bool> EndUsageAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            bool success = false;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var reservation = await _databaseService.PaidCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation == null) return false;

                reservation.Status = "completed";
                reservation.ActualEnd = DateTime.Now;
                reservation.ActualPrice = EndUsagePrice;
                success = await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);

                if (success)
                {
                    // คำนวณ duration จริงจาก ActualStart → ActualEnd
                    var actualStart = reservation.ActualStart ?? DateTime.Now;
                    var actualEnd = reservation.ActualEnd ?? DateTime.Now;
                    var actualDuration = (actualEnd - actualStart).TotalHours;
                    if (actualDuration < 0) actualDuration = 0;

                    var logId = await ReservationIdGenerator.GeneratePaidUseLogIdAsync(_databaseService, DateTime.Now);
                    var log = new PaidCourtUseLogItem
                    {
                        LogId = logId,
                        ReserveId = reservation.ReserveId,
                        CourtId = reservation.CourtId,
                        CheckInTime = actualStart,
                        LogDuration = Math.Round(actualDuration, 2),
                        LogPrice = EndUsagePrice,
                        LogStatus = "completed",
                        ReserveName = reservation.ReserveName,
                        ReservePhone = reservation.ReservePhone,
                        ReserveDate = reservation.ReserveDate,
                        ReserveTime = reservation.ReserveTime,
                        ReserveDuration = reservation.Duration
                    };
                    await _databaseService.PaidCourtUseLogs.InsertAsync(log);
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var reservation = await _databaseService.CourseCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation == null) return false;

                reservation.Status = "completed";
                reservation.ActualEnd = DateTime.Now;
                success = await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);

                if (success)
                {
                    // คำนวณ duration จริงจาก ActualStart → ActualEnd
                    var actualStart = reservation.ActualStart ?? DateTime.Now;
                    var actualEnd = reservation.ActualEnd ?? DateTime.Now;
                    var actualDuration = (actualEnd - actualStart).TotalHours;
                    if (actualDuration < 0) actualDuration = 0;

                    var logId = await ReservationIdGenerator.GenerateCourseUseLogIdAsync(_databaseService, DateTime.Now);
                    var log = new CourseCourtUseLogItem
                    {
                        LogId = logId,
                        ReserveId = reservation.ReserveId,
                        CourtId = reservation.CourtId,
                        ClassId = reservation.ClassId,
                        CheckInTime = actualStart,
                        LogDuration = Math.Round(actualDuration, 2),
                        LogStatus = "completed",
                        ReserveName = reservation.ReserveName,
                        ReservePhone = reservation.ReservePhone,
                        ReserveDate = reservation.ReserveDate,
                        ReserveTime = reservation.ReserveTime,
                        ClassTitle = reservation.ClassTitle
                    };
                    await _databaseService.CourseCourtUseLogs.InsertAsync(log);
                }
            }

            if (success)
            {
                HideDetailCard();
                await LoadCourtStatusesAsync();
                await LoadUsageLogsAsync();
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ EndUsage: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // EXTEND — ขยายเวลา
    // ========================================================================

    public async Task<string?> CheckExtendConflictAsync()
    {
        if (SelectedCourtStatus == null) return null;

        try
        {
            var courtId = SelectedCourtStatus.CourtId;
            var startTime = SelectedCourtStatus.StartTime;
            var newDuration = SelectedCourtStatus.Duration + ExtendHours;
            var newEndTime = startTime.Add(TimeSpan.FromHours(newDuration));

            // ✅ ตรวจสอบเวลาสิ้นสุดไม่เกิน 21:00
            if (newEndTime > TimeSpan.FromHours(21))
            {
                return $"ไม่สามารถขยายเวลาได้\nเวลาสิ้นสุดใหม่ ({newEndTime:hh\\:mm}) เกิน 21:00";
            }

            // ✅ ใช้ ReserveDate จาก reservation จริง แทน hardcode DateTime.Today
            // เพื่อรองรับ in_use ที่ค้างข้ามวัน
            DateTime reserveDate;
            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var res = await _databaseService.PaidCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                reserveDate = res?.ReserveDate ?? DateTime.Today;
            }
            else
            {
                var res = await _databaseService.CourseCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                reserveDate = res?.ReserveDate ?? DateTime.Today;
            }

            var paidRes = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(reserveDate);
            var conflict = paidRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflict != null)
            {
                var end = conflict.ReserveTime.Add(TimeSpan.FromHours(conflict.Duration));
                return $"ชนกับการจอง\nผู้จอง: {conflict.ReserveName}\nเวลา: {conflict.ReserveTime:hh\\:mm} - {end:hh\\:mm}";
            }

            var courseRes = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(reserveDate);
            var conflictC = courseRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflictC != null)
            {
                var end = conflictC.ReserveTime.Add(TimeSpan.FromHours(conflictC.Duration));
                return $"ชนกับการจองคอร์ส\nผู้จอง: {conflictC.ReserveName}\nเวลา: {conflictC.ReserveTime:hh\\:mm} - {end:hh\\:mm}";
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CheckExtendConflict: {ex.Message}");
            return null;
        }
    }

    [RelayCommand]
    public async Task<bool> ExtendUsageTimeAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            var newDuration = SelectedCourtStatus.Duration + ExtendHours;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var reservation = await _databaseService.PaidCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation == null) return false;

                reservation.Duration = newDuration;
                var success = await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);
                if (success)
                {
                    SelectedCourtStatus.Duration = newDuration;
                    await LoadCourtStatusesAsync();
                    return true;
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var reservation = await _databaseService.CourseCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation == null) return false;

                reservation.Duration = newDuration;
                var success = await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);
                if (success)
                {
                    SelectedCourtStatus.Duration = newDuration;
                    await LoadCourtStatusesAsync();
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ExtendUsageTime: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // CANCEL
    // ========================================================================

    [RelayCommand]
    public async Task<bool> CancelUsageAsync()
    {
        if (SelectedCourtStatus == null) return false;

        try
        {
            bool success = false;

            if (SelectedCourtStatus.UsageType == "Paid")
            {
                var reservation = await _databaseService.PaidCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation != null)
                {
                    reservation.Status = "cancelled";
                    reservation.ActualEnd = DateTime.Now;
                    reservation.ActualPrice = 0;
                    success = await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation);
                }
            }
            else if (SelectedCourtStatus.UsageType == "Course")
            {
                var reservation = await _databaseService.CourseCourtReservations
                    .GetReservationByIdAsync(SelectedCourtStatus.ReserveId);
                if (reservation != null)
                {
                    reservation.Status = "cancelled";
                    reservation.ActualEnd = DateTime.Now;
                    success = await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation);
                }
            }

            if (success)
            {
                HideDetailCard();
                await LoadCourtStatusesAsync();
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CancelUsage: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Detail Card
    // ========================================================================

    public void ShowDetailCard(CourtStatusItem courtStatus)
    {
        if (courtStatus == null || !courtStatus.IsInUse) return;

        SelectedCourtStatus = courtStatus;
        EndUsagePrice = courtStatus.UsageType == "Paid" && courtStatus.Price <= 0
            ? AppConstants.CalculateCourtPrice(courtStatus.StartTime, courtStatus.Duration)
            : courtStatus.Price;
        ExtendHours = 1.0;
        IsDetailCardVisible = true;
    }

    public void HideDetailCard()
    {
        IsDetailCardVisible = false;
        SelectedCourtStatus = null;
    }

    // ========================================================================
    // Load Reservations by Date (for Check-in tab)
    // ========================================================================

    public async Task<List<PaidCourtReservationItem>> LoadPaidReservationsByDateAsync(DateTime date)
    {
        try
        {
            return await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(date);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadPaidReservationsByDate: {ex.Message}");
            return new List<PaidCourtReservationItem>();
        }
    }

    public async Task<List<CourseCourtReservationItem>> LoadCourseReservationsByDateAsync(DateTime date)
    {
        try
        {
            return await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(date);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadCourseReservationsByDate: {ex.Message}");
            return new List<CourseCourtReservationItem>();
        }
    }

    // ========================================================================
    // Helper
    // ========================================================================

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

    [RelayCommand]
    public void CalculatePrice()
    {
        CalculatedPrice = AppConstants.CalculateCourtPrice(UsageTime, UsageDuration);
    }

    partial void OnUsageTimeChanged(TimeSpan value) => OnPropertyChanged(nameof(EstimatedEndTime));

    partial void OnUsageDurationChanged(double value)
    {
        OnPropertyChanged(nameof(EstimatedEndTime));
        if (IsPaidUsage) CalculatePrice();
    }

    partial void OnUsageTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCourseUsage));
        OnPropertyChanged(nameof(IsPaidUsage));
    }
}
