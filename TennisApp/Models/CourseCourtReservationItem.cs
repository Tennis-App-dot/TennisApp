using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Model for Course Court Reservation (การจองสนามสำหรับคอร์ส - ไม่คิดเงิน)
/// </summary>
public class CourseCourtReservationItem : INotifyPropertyChanged
{
    private string _reserveId = string.Empty;
    private string _courtId = string.Empty;
    private string _classId = string.Empty;
    private string _trainerId = string.Empty;
    private DateTime _requestDate = DateTime.Now;
    private DateTime _reserveDate = DateTime.Today;
    private TimeSpan _reserveTime = new TimeSpan(8, 0, 0); // Default 08:00
    private string _reserveName = string.Empty;
    private string _reservePhone = string.Empty;
    private string _status = "booked";

    // Start-Stop fields (สำหรับบันทึกเวลาเข้า-ออกจริง)
    private DateTime? _actualStart;
    private DateTime? _actualEnd;

    // Additional fields from joined tables (not in database, for display only)
    private string _classTitle = string.Empty;
    private int _classDuration = 1;

    // ========================================================================
    // Database Fields (ตรงกับ Database Schema)
    // ========================================================================

    /// <summary>
    /// Reservation ID (10 digits: YYYYMMDDXX)
    /// Example: 2025041609 = Booked on April 16, 2025, 9th booking of the day
    /// </summary>
    public string ReserveId
    {
        get => _reserveId;
        set => SetProperty(ref _reserveId, value);
    }

    /// <summary>
    /// Court ID (FK to Court table)
    /// Example: "01", "02", "TA"
    /// </summary>
    public string CourtId
    {
        get => _courtId;
        set => SetProperty(ref _courtId, value);
    }

    /// <summary>
    /// Class ID (FK to Course table)
    /// Example: "TA01", "T104"
    /// </summary>
    public string ClassId
    {
        get => _classId;
        set => SetProperty(ref _classId, value);
    }

    /// <summary>
    /// Trainer ID (FK to Course composite key)
    /// </summary>
    public string TrainerId
    {
        get => _trainerId;
        set => SetProperty(ref _trainerId, value);
    }

    /// <summary>
    /// Request Date (วันที่ติดต่อมาจอง)
    /// When the booking request was made
    /// </summary>
    public DateTime RequestDate
    {
        get => _requestDate;
        set => SetProperty(ref _requestDate, value);
    }

    /// <summary>
    /// Reserve Date (วันที่เรียน)
    /// The date of the class
    /// </summary>
    public DateTime ReserveDate
    {
        get => _reserveDate;
        set
        {
            if (SetProperty(ref _reserveDate, value))
            {
                OnPropertyChanged(nameof(ReserveDateDisplay));
            }
        }
    }

    /// <summary>
    /// Reserve Time (เวลาที่เรียน)
    /// Time when class starts (HH:MM)
    /// </summary>
    public TimeSpan ReserveTime
    {
        get => _reserveTime;
        set
        {
            if (SetProperty(ref _reserveTime, value))
            {
                OnPropertyChanged(nameof(ReserveTimeDisplay));
                OnPropertyChanged(nameof(EndTimeDisplay));
                OnPropertyChanged(nameof(ReserveDateTimeDisplay));
            }
        }
    }

    /// <summary>
    /// Trainer Name (ชื่อโค้ช - from Trainer table via Course)
    /// Populated automatically from Course.trainer_id → Trainer.trainer_fname
    /// </summary>
    public string ReserveName
    {
        get => _reserveName;
        set => SetProperty(ref _reserveName, value);
    }

    /// <summary>
    /// Trainer Phone (เบอร์โค้ช - from Trainer table via Course)
    /// Populated automatically from Course.trainer_id → Trainer.trainer_phone
    /// </summary>
    public string ReservePhone
    {
        get => _reservePhone;
        set => SetProperty(ref _reservePhone, value);
    }

    /// <summary>
    /// สถานะการจอง: booked / completed / cancelled
    /// </summary>
    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    // ========================================================================
    // Start-Stop Fields (บันทึกเวลาเข้า-ออกจริง)
    // ========================================================================

    /// <summary>
    /// เวลาเริ่มใช้งานจริง (ตอนกด Start)
    /// </summary>
    public DateTime? ActualStart
    {
        get => _actualStart;
        set => SetProperty(ref _actualStart, value);
    }

    /// <summary>
    /// เวลาหยุดใช้งานจริง (ตอนกด Stop)
    /// </summary>
    public DateTime? ActualEnd
    {
        get => _actualEnd;
        set => SetProperty(ref _actualEnd, value);
    }

    // ========================================================================
    // Additional Fields (from Joined Tables - for Display)
    // ========================================================================

    /// <summary>
    /// Class Title (ชื่อคอร์ส - from Course table)
    /// Example: "คอร์สผู้ใหญ่", "คอร์สเด็ก"
    /// </summary>
    public string ClassTitle
    {
        get => _classTitle;
        set => SetProperty(ref _classTitle, value);
    }

    /// <summary>
    /// Class Duration (ระยะเวลาคอร์ส - from Course table)
    /// Duration per session in hours (e.g., 1, 2)
    /// </summary>
    public int ClassDuration
    {
        get => _classDuration;
        set
        {
            if (SetProperty(ref _classDuration, value))
            {
                OnPropertyChanged(nameof(EndTimeDisplay));
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }
    }

    // ========================================================================
    // Display Properties (สำหรับแสดงใน UI)
    // ========================================================================

    /// <summary>
    /// Display Court ID (e.g., "สนาม 01", "สนาม TA", "รอจัดสรรสนาม")
    /// </summary>
    public string CourtDisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(CourtId))
                return "-";
            
            // ✅ ถ้า court_id = "00" แสดงว่ายังไม่ได้จัดสรรสนาม
            if (CourtId == "00")
                return "รอจัดสรรสนาม";
            
            return $"สนาม {CourtId}";
        }
    }

    /// <summary>
    /// Check if court is assigned (not "00")
    /// </summary>
    public bool IsCourtAssigned => !string.IsNullOrEmpty(CourtId) && CourtId != "00";

    /// <summary>
    /// Display Class (คอร์ส)
    /// </summary>
    public string ClassDisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(ClassTitle))
                return ClassTitle;
            return string.IsNullOrEmpty(ClassId) ? "-" : ClassId;
        }
    }

    /// <summary>
    /// Duration as double (for compatibility with PaidCourtReservation)
    /// </summary>
    public double Duration
    {
        get => ClassDuration;
        set => ClassDuration = (int)Math.Round(value);
    }

    /// <summary>
    /// Display Reserve Date (e.g., "16/04/2025")
    /// </summary>
    public string ReserveDateDisplay => ReserveDate.ToString("dd/MM/yyyy");

    /// <summary>
    /// Display Reserve Time (e.g., "08:00", "14:30")
    /// </summary>
    public string ReserveTimeDisplay => ReserveTime.ToString(@"hh\:mm");

    /// <summary>
    /// Display Duration (from ClassDuration)
    /// Example: "1 ชม.", "2 ชม."
    /// </summary>
    public string DurationDisplay => $"{ClassDuration} ชม.";

    /// <summary>
    /// Display End Time (คำนวณจาก ReserveTime + ClassDuration)
    /// Example: ReserveTime=08:00, ClassDuration=2 → EndTime=10:00
    /// </summary>
    public string EndTimeDisplay
    {
        get
        {
            var endTime = ReserveTime.Add(TimeSpan.FromHours(ClassDuration));
            return endTime.ToString(@"hh\:mm");
        }
    }

    /// <summary>
    /// Display Date + Time (e.g., "16/04/2025, 14:00")
    /// </summary>
    public string ReserveDateTimeDisplay => $"{ReserveDateDisplay}, {ReserveTimeDisplay}";

    /// <summary>
    /// Display Full Info (for DataGrid)
    /// Example: "16/04/2025, 14:00 - คอร์สผู้ใหญ่"
    /// </summary>
    public string FullReservationDisplay
    {
        get
        {
            var classInfo = string.IsNullOrEmpty(ClassTitle) ? ClassId : ClassTitle;
            return $"{ReserveDateTimeDisplay} - {classInfo}";
        }
    }

    /// <summary>
    /// Display Status (for UI - based on status field)
    /// </summary>
    public string StatusDisplay => Status switch
    {
        "booked" => "จองแล้ว",
        "in_use" => "กำลังใช้งาน",
        "completed" => "เสร็จสิ้น",
        "cancelled" => "ยกเลิก",
        _ => "จองแล้ว"
    };

    /// <summary>
    /// สีสถานะสำหรับแสดงใน UI
    /// </summary>
    public string StatusColor => Status switch
    {
        "booked" => "#2196F3",
        "in_use" => "#FF9800",
        "completed" => "#4CAF50",
        "cancelled" => "#F44336",
        _ => "#2196F3"
    };

    /// <summary>
    /// Display Time Range (e.g., "10:00-11:00")
    /// </summary>
    public string TimeRangeDisplay => $"{ReserveTimeDisplay}-{EndTimeDisplay}";

    /// <summary>
    /// Purpose Display (for UI filter)
    /// Always returns "คอร์สเรียน" for course reservations
    /// </summary>
    public string PurposeDisplay => "คอร์สเรียน";

    // ========================================================================
    // INotifyPropertyChanged Implementation
    // ========================================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    /// <summary>
    /// Check if this reservation overlaps with another time range
    /// </summary>
    public bool OverlapsWith(DateTime date, TimeSpan startTime, int duration)
    {
        if (ReserveDate.Date != date.Date)
            return false;

        var thisStart = ReserveTime;
        var thisEnd = ReserveTime.Add(TimeSpan.FromHours(ClassDuration));
        var otherStart = startTime;
        var otherEnd = startTime.Add(TimeSpan.FromHours(duration));

        return thisStart < otherEnd && thisEnd > otherStart;
    }

    /// <summary>
    /// Clone this reservation
    /// </summary>
    public CourseCourtReservationItem Clone()
    {
        return new CourseCourtReservationItem
        {
            ReserveId = this.ReserveId,
            CourtId = this.CourtId,
            ClassId = this.ClassId,
            TrainerId = this.TrainerId,
            RequestDate = this.RequestDate,
            ReserveDate = this.ReserveDate,
            ReserveTime = this.ReserveTime,
            ReserveName = this.ReserveName,
            ReservePhone = this.ReservePhone,
            ClassTitle = this.ClassTitle,
            ClassDuration = this.ClassDuration,
            Status = this.Status,
            ActualStart = this.ActualStart,
            ActualEnd = this.ActualEnd
        };
    }
}
