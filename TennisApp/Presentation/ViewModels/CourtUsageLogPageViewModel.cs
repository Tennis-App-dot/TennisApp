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

    public CourtUsageLogPageViewModel()
    {
        try
        {
            _databaseService = ((App)Microsoft.UI.Xaml.Application.Current).DatabaseService;
            _databaseService.EnsureInitialized();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CourtUsageLogPageViewModel error: {ex.Message}");
        }
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

            var paidRes = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(today);
            var courseRes = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(today);

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
                    status.Price = activePaid.ActualPrice ?? 0;
                    status.ReserveId = activePaid.ReserveId;
                    status.LogStatus = "in_use";
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
                CalculatedPrice = (int)(UsageDuration * 200);
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
    /// </summary>
    public async Task<string?> CheckCourtConflictForCheckinAsync(
        string courtId, DateTime date, TimeSpan startTime, double duration, string? excludeReserveId = null)
    {
        try
        {
            var endTime = startTime.Add(TimeSpan.FromHours(duration));

            // Check Paid reservations
            var paidRes = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(date);
            var paidConflict = paidRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != (excludeReserveId ?? "") &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (paidConflict != null)
            {
                var cEnd = paidConflict.ReserveTime.Add(TimeSpan.FromHours(paidConflict.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"ชนกับ: {paidConflict.ReserveId}\n" +
                       $"ผู้จอง: {paidConflict.ReserveName}\n" +
                       $"เวลา: {paidConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: {(paidConflict.Status == "in_use" ? "กำลังใช้งาน" : "จองแล้ว")}";
            }

            // Check Course reservations
            var courseRes = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(date);
            var courseConflict = courseRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != (excludeReserveId ?? "") &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (courseConflict != null)
            {
                var cEnd = courseConflict.ReserveTime.Add(TimeSpan.FromHours(courseConflict.Duration));
                return $"สนาม {courtId} ไม่ว่าง!\n\n" +
                       $"ชนกับ: {courseConflict.ReserveId}\n" +
                       $"คอร์ส: {courseConflict.ClassTitle}\n" +
                       $"ผู้จอง: {courseConflict.ReserveName}\n" +
                       $"เวลา: {courseConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"สถานะ: {(courseConflict.Status == "in_use" ? "กำลังใช้งาน" : "จองแล้ว")}";
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
                    ActualStart = DateTime.Now
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
                    var logId = await ReservationIdGenerator.GeneratePaidUseLogIdAsync(_databaseService, DateTime.Now);
                    var log = new PaidCourtUseLogItem
                    {
                        LogId = logId,
                        ReserveId = reservation.ReserveId,
                        CourtId = reservation.CourtId,
                        CheckInTime = reservation.ActualStart ?? DateTime.Now,
                        LogDuration = reservation.Duration,
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
                    var logId = await ReservationIdGenerator.GenerateCourseUseLogIdAsync(_databaseService, DateTime.Now);
                    var log = new CourseCourtUseLogItem
                    {
                        LogId = logId,
                        ReserveId = reservation.ReserveId,
                        CourtId = reservation.CourtId,
                        ClassId = reservation.ClassId,
                        CheckInTime = reservation.ActualStart ?? DateTime.Now,
                        LogDuration = reservation.Duration,
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
            var today = DateTime.Today;

            var paidRes = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(today);
            var conflict = paidRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflict != null)
            {
                var end = conflict.ReserveTime.Add(TimeSpan.FromHours(conflict.Duration));
                return $"ชนกับการจอง {conflict.ReserveId}\nผู้จอง: {conflict.ReserveName}\nเวลา: {conflict.ReserveTime:hh\\:mm} - {end:hh\\:mm}";
            }

            var courseRes = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(today);
            var conflictC = courseRes.FirstOrDefault(r =>
                r.CourtId == courtId &&
                r.ReserveId != SelectedCourtStatus.ReserveId &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < newEndTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (conflictC != null)
            {
                var end = conflictC.ReserveTime.Add(TimeSpan.FromHours(conflictC.Duration));
                return $"ชนกับการจองคอร์ส {conflictC.ReserveId}\nผู้จอง: {conflictC.ReserveName}\nเวลา: {conflictC.ReserveTime:hh\\:mm} - {end:hh\\:mm}";
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
        EndUsagePrice = courtStatus.Price;
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
        const int pricePerHour = 200;
        CalculatedPrice = (int)(UsageDuration * pricePerHour);
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
