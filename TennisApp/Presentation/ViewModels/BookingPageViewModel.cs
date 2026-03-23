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
/// ViewModel สำหรับหน้าจองสนาม (BookingPage)
/// จัดการทั้งการจองแบบเช่า (Paid) และการจองสำหรับคอร์ส (Course)
/// </summary>
public partial class BookingPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService = null!;

    // ========================================================================
    // Collections
    // ========================================================================

    /// <summary>
    /// รายการจองแบบเช่าทั้งหมด
    /// </summary>
    public ObservableCollection<PaidCourtReservationItem> PaidReservations { get; } = new();

    /// <summary>
    /// รายการจองสำหรับคอร์สทั้งหมด
    /// </summary>
    public ObservableCollection<CourseCourtReservationItem> CourseReservations { get; } = new();

    /// <summary>
    /// รายการสนามที่พร้อมใช้งาน (court_status = 1)
    /// </summary>
    public ObservableCollection<CourtItem> AvailableCourts { get; } = new();

    /// <summary>
    /// รายการคอร์สที่เปิดสอน
    /// </summary>
    public ObservableCollection<CourseItem> AvailableCourses { get; } = new();

    // ========================================================================
    // Summary Properties (สำหรับ Summary Cards)
    // ========================================================================

    [ObservableProperty]
    private int _todayCount;

    [ObservableProperty]
    private int _futureCount;

    [ObservableProperty]
    private int _cancelledCount;

    // ========================================================================
    // Filter Properties (สำหรับกรองข้อมูล)
    // ========================================================================

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _selectedTime = new TimeSpan(8, 0, 0); // Default 08:00

    [ObservableProperty]
    private double _selectedDuration = 1.0; // Default 1 hour

    [ObservableProperty]
    private string _selectedCourtId = string.Empty;

    [ObservableProperty]
    private string _reservationType = "Paid"; // "Paid" or "Course"

    [ObservableProperty]
    private string _selectedCourseId = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    // ========================================================================
    // Display Properties
    // ========================================================================

    /// <summary>
    /// เวลาสิ้นสุดที่คาดการณ์ (อ่านอย่างเดียว)
    /// </summary>
    public string EstimatedEndTime
    {
        get
        {
            var endTime = SelectedTime.Add(TimeSpan.FromHours(SelectedDuration));
            return endTime.ToString(@"hh\:mm");
        }
    }

    /// <summary>
    /// แสดง/ซ่อน ComboBox สำหรับเลือกคอร์ส
    /// </summary>
    public bool IsCourseReservation => ReservationType == "Course";

    // ========================================================================
    // Constructor
    // ========================================================================

    public BookingPageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("📅 BookingPageViewModel constructor เริ่มทำงาน");

        try
        {
            _databaseService = ((App)Microsoft.UI.Xaml.Application.Current).DatabaseService;
            _databaseService.EnsureInitialized();
            System.Diagnostics.Debug.WriteLine("✅ DatabaseService สร้างสำเร็จ");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ BookingPageViewModel constructor error: {ex.Message}");
        }
    }

    // ========================================================================
    // Load Data Commands
    // ========================================================================

    /// <summary>
    /// โหลดข้อมูลจองทั้งหมดจาก Database
    /// </summary>
    [RelayCommand]
    public async Task LoadReservationsAsync()
    {
        try
        {
            _databaseService.EnsureInitialized();
            System.Diagnostics.Debug.WriteLine("📥 LoadReservationsAsync เริ่มทำงาน...");

            // โหลดการจองแบบเช่า
            var paidReservations = await _databaseService.PaidCourtReservations.GetAllReservationsAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"   พบการจองแบบเช่า: {paidReservations.Count} รายการ");

            PaidReservations.Clear();
            foreach (var reservation in paidReservations)
            {
                PaidReservations.Add(reservation);
            }

            // โหลดการจองสำหรับคอร์ส
            var courseReservations = await _databaseService.CourseCourtReservations.GetAllReservationsAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"   พบการจองสำหรับคอร์ส: {courseReservations.Count} รายการ");

            CourseReservations.Clear();
            foreach (var reservation in courseReservations)
            {
                CourseReservations.Add(reservation);
            }

            UpdateSummary();
            System.Diagnostics.Debug.WriteLine($"✅ LoadReservationsAsync เสร็จสิ้น - Paid: {PaidReservations.Count}, Course: {CourseReservations.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading reservations: {ex.Message}");
        }
    }

    /// <summary>
    /// โหลดรายการสนามที่พร้อมใช้งาน (court_status = 1)
    /// </summary>
    [RelayCommand]
    public async Task LoadAvailableCourtsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🎾 LoadAvailableCourtsAsync เริ่มทำงาน...");

            var courts = await _databaseService.Courts.GetCourtsByStatusAsync("1").ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"   พบสนามที่พร้อมใช้: {courts.Count} สนาม");

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

            var courses = await _databaseService.Courses.GetAllCoursesAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"   พบคอร์ส: {courses.Count} คอร์ส");

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
    // Summary
    // ========================================================================

    /// <summary>
    /// อัปเดตค่าต่างๆ สำหรับ Summary Cards
    /// </summary>
    public void UpdateSummary()
    {
        var today = DateTime.Today;

        var allPaidStatuses = PaidReservations.ToList();
        var allCourseStatuses = CourseReservations.ToList();

        TodayCount = allPaidStatuses.Count(r => r.ReserveDate.Date == today && r.Status != "cancelled")
                   + allCourseStatuses.Count(r => r.ReserveDate.Date == today && r.Status != "cancelled");

        FutureCount = allPaidStatuses.Count(r => r.ReserveDate.Date > today && r.Status == "booked")
                    + allCourseStatuses.Count(r => r.ReserveDate.Date > today && r.Status == "booked");

        CancelledCount = allPaidStatuses.Count(r => r.Status == "cancelled")
                       + allCourseStatuses.Count(r => r.Status == "cancelled");
    }

    // ========================================================================
    // Court Availability Check
    // ========================================================================

    /// <summary>
    /// ตรวจสอบว่าสนามว่างในช่วงเวลาที่เลือกหรือไม่
    /// </summary>
    /// <param name="courtId">รหัสสนาม</param>
    /// <param name="reserveDate">วันที่ต้องการใช้</param>
    /// <param name="reserveTime">เวลาที่ต้องการใช้</param>
    /// <param name="duration">ระยะเวลา (ชั่วโมง)</param>
    /// <returns>true ถ้าสนามว่าง, false ถ้าสนามไม่ว่าง</returns>
    public async Task<bool> IsCourtAvailableAsync(string courtId, DateTime reserveDate, TimeSpan reserveTime, double duration)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 ตรวจสอบสนาม {courtId} วันที่ {reserveDate:yyyy-MM-dd} เวลา {reserveTime:hh\\:mm} ({duration} ชม.)");

            // ตรวจสอบการจองแบบเช่า
            var isPaidAvailable = await _databaseService.PaidCourtReservations
                .IsCourtAvailableAsync(courtId, reserveDate, reserveTime, duration)
                .ConfigureAwait(false);

            if (!isPaidAvailable)
            {
                System.Diagnostics.Debug.WriteLine($"   ❌ มีการจองแบบเช่าซ้อนทับ");
                return false;
            }

            // ตรวจสอบการจองสำหรับคอร์ส
            var isCourseAvailable = await _databaseService.CourseCourtReservations
                .IsCourtAvailableAsync(courtId, reserveDate, reserveTime, duration)
                .ConfigureAwait(false);

            if (!isCourseAvailable)
            {
                System.Diagnostics.Debug.WriteLine($"   ❌ มีการจองสำหรับคอร์สซ้อนทับ");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"   ✅ สนามว่าง");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking court availability: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// หาสนามที่ว่างในช่วงเวลาที่เลือก
    /// </summary>
    public async Task<List<CourtItem>> GetAvailableCourtsForTimeSlotAsync(DateTime reserveDate, TimeSpan reserveTime, double duration)
    {
        var availableCourts = new List<CourtItem>();

        foreach (var court in AvailableCourts)
        {
            var isAvailable = await IsCourtAvailableAsync(court.CourtID, reserveDate, reserveTime, duration);
            if (isAvailable)
            {
                availableCourts.Add(court);
            }
        }

        System.Diagnostics.Debug.WriteLine($"🎾 พบสนามว่าง: {availableCourts.Count}/{AvailableCourts.Count} สนาม");
        return availableCourts;
    }

    // ========================================================================
    // CRUD Commands (Paid Reservation)
    // ========================================================================

    /// <summary>
    /// เพิ่มการจองแบบเช่า
    /// </summary>
    [RelayCommand]
    public async Task<bool> AddPaidReservationAsync(PaidCourtReservationItem reservation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"➕ เพิ่มการจองแบบเช่า: {reservation.ReserveId}");

            // No court availability check needed since court is not assigned yet
            // Court will be assigned later by staff

            // บันทึกลง database
            var success = await _databaseService.PaidCourtReservations.AddReservationAsync(reservation).ConfigureAwait(false);

            if (success)
            {
                PaidReservations.Add(reservation);
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ เพิ่มสำเร็จ (สนามจะถูกจัดสรรในภายหลัง)");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding paid reservation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// แก้ไขการจองแบบเช่า
    /// </summary>
    [RelayCommand]
    public async Task<bool> UpdatePaidReservationAsync(PaidCourtReservationItem reservation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"✏️ แก้ไขการจองแบบเช่า: {reservation.ReserveId}");

            var success = await _databaseService.PaidCourtReservations.UpdateReservationAsync(reservation).ConfigureAwait(false);

            if (success)
            {
                // อัปเดตใน collection
                var existing = PaidReservations.FirstOrDefault(r => r.ReserveId == reservation.ReserveId);
                if (existing != null)
                {
                    var index = PaidReservations.IndexOf(existing);
                    PaidReservations[index] = reservation;
                }
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ แก้ไขสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating paid reservation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ลบการจองแบบเช่า
    /// </summary>
    [RelayCommand]
    public async Task<bool> DeletePaidReservationAsync(string reserveId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ ลบการจองแบบเช่า: {reserveId}");

            var success = await _databaseService.PaidCourtReservations.DeleteReservationAsync(reserveId).ConfigureAwait(false);

            if (success)
            {
                var existing = PaidReservations.FirstOrDefault(r => r.ReserveId == reserveId);
                if (existing != null)
                {
                    PaidReservations.Remove(existing);
                }
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ ลบสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting paid reservation: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // CRUD Commands (Course Reservation)
    // ========================================================================

    /// <summary>
    /// เพิ่มการจองสำหรับคอร์ส
    /// </summary>
    [RelayCommand]
    public async Task<bool> AddCourseReservationAsync(CourseCourtReservationItem reservation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"➕ เพิ่มการจองสำหรับคอร์ส: {reservation.ReserveId}");

            // No court availability check needed since court is not assigned yet
            // Court will be assigned later by staff

            // บันทึกลง database
            var success = await _databaseService.CourseCourtReservations.AddReservationAsync(reservation).ConfigureAwait(false);

            if (success)
            {
                CourseReservations.Add(reservation);
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ เพิ่มสำเร็จ (สนามจะถูกจัดสรรในภายหลัง)");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding course reservation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// แก้ไขการจองสำหรับคอร์ส
    /// </summary>
    [RelayCommand]
    public async Task<bool> UpdateCourseReservationAsync(CourseCourtReservationItem reservation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"✏️ แก้ไขการจองสำหรับคอร์ส: {reservation.ReserveId}");

            var success = await _databaseService.CourseCourtReservations.UpdateReservationAsync(reservation).ConfigureAwait(false);

            if (success)
            {
                // อัปเดตใน collection
                var existing = CourseReservations.FirstOrDefault(r => r.ReserveId == reservation.ReserveId);
                if (existing != null)
                {
                    var index = CourseReservations.IndexOf(existing);
                    CourseReservations[index] = reservation;
                }
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ แก้ไขสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating course reservation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ลบการจองสำหรับคอร์ส
    /// </summary>
    [RelayCommand]
    public async Task<bool> DeleteCourseReservationAsync(string reserveId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ ลบการจองสำหรับคอร์ส: {reserveId}");

            var success = await _databaseService.CourseCourtReservations.DeleteReservationAsync(reserveId).ConfigureAwait(false);

            if (success)
            {
                var existing = CourseReservations.FirstOrDefault(r => r.ReserveId == reserveId);
                if (existing != null)
                {
                    CourseReservations.Remove(existing);
                }
                UpdateSummary();
                System.Diagnostics.Debug.WriteLine($"   ✅ ลบสำเร็จ");
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting course reservation: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Status Update
    // ========================================================================

    public async Task<bool> UpdatePaidStatusAsync(string reserveId, string status)
    {
        try
        {
            var success = await _databaseService.PaidCourtReservations.UpdateStatusAsync(reserveId, status).ConfigureAwait(false);
            if (success)
            {
                var existing = PaidReservations.FirstOrDefault(r => r.ReserveId == reserveId);
                if (existing != null) existing.Status = status;
                UpdateSummary();
            }
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating paid status: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateCourseStatusAsync(string reserveId, string status)
    {
        try
        {
            var success = await _databaseService.CourseCourtReservations.UpdateStatusAsync(reserveId, status).ConfigureAwait(false);
            if (success)
            {
                var existing = CourseReservations.FirstOrDefault(r => r.ReserveId == reserveId);
                if (existing != null) existing.Status = status;
                UpdateSummary();
            }
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating course status: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Duplicate Check
    // ========================================================================

    /// <summary>
    /// ตรวจสอบว่ามีการจองซ้ำหรือไม่ (ชื่อเดียวกัน + วันเดียวกัน + เวลาซ้อนทับ)
    /// เช็คทั้งตาราง Paid และ Course
    /// </summary>
    public async Task<bool> HasDuplicateReservationAsync(string reserveName, DateTime reserveDate, TimeSpan startTime, double duration)
    {
        try
        {
            var hasPaidDuplicate = await _databaseService.PaidCourtReservations
                .HasDuplicateReservationAsync(reserveName, reserveDate, startTime, duration)
                .ConfigureAwait(false);

            if (hasPaidDuplicate) return true;

            var hasCourseDuplicate = await _databaseService.CourseCourtReservations
                .HasDuplicateReservationAsync(reserveName, reserveDate, startTime, duration)
                .ConfigureAwait(false);

            return hasCourseDuplicate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error checking duplicate reservation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ตรวจสอบการจองซ้ำและคืนข้อความอธิบาย (null = ไม่ซ้ำ)
    /// เช็คทั้ง Paid และ Course cross-table
    /// </summary>
    public async Task<string?> GetDuplicateReservationMessageAsync(
        string reserveName, DateTime reserveDate, TimeSpan startTime, double duration, string? excludeReserveId = null)
    {
        try
        {
            var endTime = startTime.Add(TimeSpan.FromHours(duration));

            // Check Paid table
            var paidList = await _databaseService.PaidCourtReservations.GetReservationsByDateAsync(reserveDate).ConfigureAwait(false);
            var paidConflict = paidList.FirstOrDefault(r =>
                r.ReserveName.Equals(reserveName, StringComparison.OrdinalIgnoreCase) &&
                r.ReserveId != (excludeReserveId ?? "") &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (paidConflict != null)
            {
                var cEnd = paidConflict.ReserveTime.Add(TimeSpan.FromHours(paidConflict.Duration));
                return $"พบการจองเช่าสนามของ \"{paidConflict.ReserveName}\"\n" +
                       $"วันที่ {reserveDate:dd/MM/yyyy}\n" +
                       $"เวลา {paidConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"รหัสจอง: {paidConflict.ReserveId}";
            }

            // Check Course table
            var courseList = await _databaseService.CourseCourtReservations.GetReservationsByDateAsync(reserveDate).ConfigureAwait(false);
            var courseConflict = courseList.FirstOrDefault(r =>
                r.ReserveName.Equals(reserveName, StringComparison.OrdinalIgnoreCase) &&
                r.ReserveId != (excludeReserveId ?? "") &&
                r.Status is "booked" or "in_use" &&
                r.ReserveTime < endTime &&
                r.ReserveTime.Add(TimeSpan.FromHours(r.Duration)) > startTime);

            if (courseConflict != null)
            {
                var cEnd = courseConflict.ReserveTime.Add(TimeSpan.FromHours(courseConflict.Duration));
                return $"พบการจองคอร์สของ \"{courseConflict.ReserveName}\"\n" +
                       $"คอร์ส: {courseConflict.ClassDisplayName}\n" +
                       $"วันที่ {reserveDate:dd/MM/yyyy}\n" +
                       $"เวลา {courseConflict.ReserveTime:hh\\:mm} - {cEnd:hh\\:mm}\n" +
                       $"รหัสจอง: {courseConflict.ReserveId}";
            }

            return null; // ไม่ซ้ำ
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ GetDuplicateReservationMessage: {ex.Message}");
            return null;
        }
    }

    // ========================================================================
    // Property Change Notifications
    // ========================================================================

    partial void OnSelectedTimeChanged(TimeSpan value) => OnPropertyChanged(nameof(EstimatedEndTime));
    partial void OnSelectedDurationChanged(double value) => OnPropertyChanged(nameof(EstimatedEndTime));
    partial void OnReservationTypeChanged(string value) => OnPropertyChanged(nameof(IsCourseReservation));
}
