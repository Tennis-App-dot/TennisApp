using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Model สำหรับบันทึกการเข้าใช้สนามจากคอร์ส (Course Court Usage Log)
/// </summary>
public class CourseCourtUseLogItem : INotifyPropertyChanged
{
    private string _logId = string.Empty;
    private string _reserveId = string.Empty;
    private string _courtId = string.Empty;
    private string _classId = string.Empty;
    private DateTime _checkInTime;
    private double _logDuration;
    private string _logStatus = "completed";
    
    // Reservation details (from join)
    private string _reserveName = string.Empty;
    private string _reservePhone = string.Empty;
    private string _classTitle = string.Empty;
    private DateTime _reserveDate;
    private TimeSpan _reserveTime;

    // ========================================================================
    // Core Properties (from CourseCourtUseLog table)
    // ========================================================================

    /// <summary>
    /// รหัสบันทึกการใช้งาน (Log ID) - Format: YYYYMMDDXX
    /// </summary>
    public string LogId
    {
        get => _logId;
        set { _logId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// รหัสการจอง (Reserve ID) - FK to CourseCourtReservation
    /// </summary>
    public string ReserveId
    {
        get => _reserveId;
        set { _reserveId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// สนามที่ใช้งานจริง (Court ID)
    /// </summary>
    public string CourtId
    {
        get => _courtId;
        set { _courtId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CourtDisplayName)); }
    }

    /// <summary>
    /// รหัสคอร์ส (Class ID)
    /// </summary>
    public string ClassId
    {
        get => _classId;
        set { _classId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// เวลาที่เข้าใช้งานจริง (Check-in Time)
    /// </summary>
    public DateTime CheckInTime
    {
        get => _checkInTime;
        set { _checkInTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(CheckInTimeDisplay)); }
    }

    /// <summary>
    /// ระยะเวลาที่ใช้งานจริง (ชั่วโมง)
    /// </summary>
    public double LogDuration
    {
        get => _logDuration;
        set { _logDuration = value; OnPropertyChanged(); OnPropertyChanged(nameof(LogDurationDisplay)); }
    }

    /// <summary>
    /// สถานะการใช้งาน: completed, cancelled, no-show
    /// </summary>
    public string LogStatus
    {
        get => _logStatus;
        set { _logStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(LogStatusDisplay)); }
    }

    // ========================================================================
    // Reservation & Course Details (from joins)
    // ========================================================================

    /// <summary>
    /// ชื่อผู้จอง/ครูผู้สอน (from reservation)
    /// </summary>
    public string ReserveName
    {
        get => _reserveName;
        set { _reserveName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// เบอร์โทรศัพท์ผู้จอง (from reservation)
    /// </summary>
    public string ReservePhone
    {
        get => _reservePhone;
        set { _reservePhone = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// ชื่อคอร์ส (from Course table)
    /// </summary>
    public string ClassTitle
    {
        get => _classTitle;
        set { _classTitle = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// วันที่จองไว้ (from reservation)
    /// </summary>
    public DateTime ReserveDate
    {
        get => _reserveDate;
        set { _reserveDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReserveDateDisplay)); }
    }

    /// <summary>
    /// เวลาที่จองไว้ (from reservation)
    /// </summary>
    public TimeSpan ReserveTime
    {
        get => _reserveTime;
        set { _reserveTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReserveTimeDisplay)); }
    }

    // ========================================================================
    // Display Properties (for UI binding)
    // ========================================================================

    /// <summary>
    /// แสดงชื่อสนาม (สนาม 01, สนาม 02, etc.)
    /// </summary>
    public string CourtDisplayName => string.IsNullOrEmpty(CourtId) ? "ยังไม่ระบุสนาม" : $"สนาม {CourtId}";

    /// <summary>
    /// แสดงเวลาเข้าใช้งานจริง (dd/MM/yyyy HH:mm)
    /// </summary>
    public string CheckInTimeDisplay => CheckInTime.ToString("dd/MM/yyyy HH:mm");

    /// <summary>
    /// แสดงระยะเวลาที่ใช้จริง (1.0 ชั่วโมง, 2.5 ชั่วโมง)
    /// </summary>
    public string LogDurationDisplay => $"{LogDuration:0.#} ชั่วโมง";

    /// <summary>
    /// แสดงสถานะ (เสร็จสมบูรณ์, ยกเลิก, ไม่มาใช้งาน)
    /// </summary>
    public string LogStatusDisplay => LogStatus switch
    {
        "completed" => "เสร็จสมบูรณ์",
        "cancelled" => "ยกเลิก",
        "no-show" => "ไม่มาใช้งาน",
        _ => LogStatus
    };

    /// <summary>
    /// แสดงวันที่จองไว้ (dd/MM/yyyy)
    /// </summary>
    public string ReserveDateDisplay => ReserveDate.ToString("dd/MM/yyyy");

    /// <summary>
    /// แสดงเวลาที่จองไว้ (HH:mm)
    /// </summary>
    public string ReserveTimeDisplay => $"{ReserveTime:hh\\:mm}";

    // ========================================================================
    // INotifyPropertyChanged Implementation
    // ========================================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
