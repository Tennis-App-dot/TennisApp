using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Model สำหรับแสดงสถานะสนาม real-time ในหน้าบันทึกการเข้าใช้งาน
/// รวมข้อมูลจาก Court + UseLog + Reservation
/// </summary>
public class CourtStatusItem : INotifyPropertyChanged
{
    private string _courtId = string.Empty;
    private bool _isInUse;
    private string _statusText = "ว่าง";
    private string _userName = string.Empty;
    private string _userPhone = string.Empty;
    private string _usageType = string.Empty;
    private string _courseTitle = string.Empty;
    private TimeSpan _startTime;
    private double _duration;
    private int _price;
    private string _logId = string.Empty;
    private string _reserveId = string.Empty;
    private string _logStatus = string.Empty;
    private DateTime? _actualStartTime;

    // ========================================================================
    // Core Properties
    // ========================================================================

    public string CourtId
    {
        get => _courtId;
        set { _courtId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CourtDisplayName)); }
    }

    public bool IsInUse
    {
        get => _isInUse;
        set { _isInUse = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusColor)); }
    }

    public string StatusText
    {
        get
        {
            if (!IsInUse) return "ว่าง";
            return $"ระยะเวลาสิ้นสุด: {EndTime:hh\\:mm}";
        }
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(); }
    }

    public string UserPhone
    {
        get => _userPhone;
        set { _userPhone = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// "Paid" or "Course"
    /// </summary>
    public string UsageType
    {
        get => _usageType;
        set { _usageType = value; OnPropertyChanged(); OnPropertyChanged(nameof(UsageTypeDisplay)); }
    }

    public string CourseTitle
    {
        get => _courseTitle;
        set { _courseTitle = value; OnPropertyChanged(); }
    }

    public TimeSpan StartTime
    {
        get => _startTime;
        set { _startTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartTimeDisplay)); }
    }

    public double Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationDisplay)); OnPropertyChanged(nameof(EndTime)); OnPropertyChanged(nameof(StatusText)); }
    }

    public TimeSpan EndTime
    {
        get => StartTime.Add(TimeSpan.FromHours(Duration));
    }

    public int Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceDisplay)); }
    }

    public string LogId
    {
        get => _logId;
        set { _logId = value; OnPropertyChanged(); }
    }

    public string ReserveId
    {
        get => _reserveId;
        set { _reserveId = value; OnPropertyChanged(); }
    }

    public string LogStatus
    {
        get => _logStatus;
        set { _logStatus = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// เวลาเช็คอินจริง (ActualStart จาก Reservation)
    /// ถ้าไม่มี จะ fallback เป็น StartTime (ReserveTime)
    /// </summary>
    public DateTime? ActualStartTime
    {
        get => _actualStartTime;
        set { _actualStartTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActualStartTimeDisplay)); }
    }

    // ========================================================================
    // Display Properties
    // ========================================================================

    public string CourtDisplayName => $"สนาม {CourtId}";

    public string StartTimeDisplay => StartTime.ToString(@"hh\:mm");

    public string DurationDisplay => $"{Duration:0.#} ชั่วโมง";

    public string EndTimeDisplay => EndTime.ToString(@"hh\:mm");

    public string PriceDisplay => $"{Price:N0}";

    public string ActualStartTimeDisplay => ActualStartTime?.ToString("HH:mm") ?? StartTimeDisplay;

    public string UsageTypeDisplay => UsageType switch
    {
        "Paid" => "เช่าใช้พื้นที่",
        "Course" => "คอร์สเรียน",
        _ => "-"
    };

    public string StatusColor => IsInUse ? "#FF9800" : "#4CAF50";

    // ========================================================================
    // INotifyPropertyChanged
    // ========================================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
