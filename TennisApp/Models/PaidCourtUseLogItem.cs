using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Model สำหรับบันทึกการเข้าใช้สนามแบบเช่า (Paid Court Usage Log)
/// </summary>
public class PaidCourtUseLogItem : INotifyPropertyChanged
{
    private string _logId = string.Empty;
    private string _reserveId = string.Empty;
    private string _courtId = string.Empty;
    private DateTime _checkInTime;
    private double _logDuration;
    private int _logPrice;
    private string _logStatus = "completed";
    
    // Reservation details (from join)
    private string _reserveName = string.Empty;
    private string _reservePhone = string.Empty;
    private DateTime _reserveDate;
    private TimeSpan _reserveTime;
    private double _reserveDuration;

    // ========================================================================
    // Core Properties (from PaidCourtUseLog table)
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
    /// รหัสการจอง (Reserve ID) - FK to PaidCourtReservation
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
    /// ราคาที่คิดจริง (บาท)
    /// </summary>
    public int LogPrice
    {
        get => _logPrice;
        set { _logPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(LogPriceDisplay)); }
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
    // Reservation Details (from PaidCourtReservation)
    // ========================================================================

    /// <summary>
    /// ชื่อผู้จอง (from reservation)
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

    /// <summary>
    /// ระยะเวลาที่จองไว้ (ชั่วโมง) (from reservation)
    /// </summary>
    public double ReserveDuration
    {
        get => _reserveDuration;
        set { _reserveDuration = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReserveDurationDisplay)); }
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
    /// แสดงราคา (฿200, ฿400)
    /// </summary>
    public string LogPriceDisplay => $"฿{LogPrice:N0}";

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
    /// แสดงเวลาที่จองไว้ (HH:mm - HH:mm)
    /// </summary>
    public string ReserveTimeDisplay
    {
        get
        {
            var endTime = ReserveTime.Add(TimeSpan.FromHours(ReserveDuration));
            return $"{ReserveTime:hh\\:mm} - {endTime:hh\\:mm}";
        }
    }

    /// <summary>
    /// แสดงระยะเวลาที่จองไว้ (1.0 ชั่วโมง, 2.0 ชั่วโมง)
    /// </summary>
    public string ReserveDurationDisplay => $"{ReserveDuration:0.#} ชั่วโมง";

    // ========================================================================
    // INotifyPropertyChanged Implementation
    // ========================================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
