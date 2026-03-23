using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TennisApp.Helpers;

namespace TennisApp.Models;

/// <summary>
/// Composite key สำหรับระบุคอร์ส (class_id + trainer_id)
/// </summary>
public record CourseKey(string ClassId, string TrainerId)
{
    public override string ToString() => $"{ClassId}|{TrainerId}";

    public static CourseKey? Parse(string? compositeKey)
    {
        if (string.IsNullOrEmpty(compositeKey)) return null;
        var parts = compositeKey.Split('|');
        return parts.Length == 2 ? new CourseKey(parts[0], parts[1]) : null;
    }
}

public class CourseItem : INotifyPropertyChanged
{
    private string _classId = string.Empty;
    private string _classTitle = string.Empty;
    private int _classTime;
    private int _classDuration;
    private int _classRate;
    private string _trainerId = string.Empty;
    private string _trainerName = string.Empty;
    private DateTime? _lastUpdated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClassId
    {
        get => _classId;
        set { if (_classId != value) { _classId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CourseTypeDescription)); OnPropertyChanged(nameof(SessionCountText)); OnPropertyChanged(nameof(CompositeKey)); } }
    }

    public string ClassTitle
    {
        get => _classTitle;
        set { if (_classTitle != value) { _classTitle = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// จำนวนครั้ง — ดึงจาก class_id หลัก 3-4 (เช่น TA04 → 4, T300 → 0 = รายเดือน)
    /// </summary>
    public int ClassTime
    {
        get => _classTime;
        set { if (_classTime != value) { _classTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(SessionCountText)); } }
    }

    public int ClassDuration
    {
        get => _classDuration;
        set { if (_classDuration != value) { _classDuration = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// ราคาของคอร์สนี้ (ดึงจาก CoursePricingHelper ตามตาราง Fee &amp; Tickets)
    /// </summary>
    public int ClassRate
    {
        get => _classRate;
        set { if (_classRate != value) { _classRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRateText)); } }
    }

    public string TrainerId
    {
        get => _trainerId;
        set { if (_trainerId != value) { _trainerId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CompositeKey)); } }
    }

    public string TrainerName
    {
        get => _trainerName;
        set { if (_trainerName != value) { _trainerName = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrainerDisplayName)); } }
    }

    public DateTime? LastUpdated
    {
        get => _lastUpdated;
        set { if (_lastUpdated != value) { _lastUpdated = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastUpdatedText)); } }
    }

    // ─── Composite Key ────────────────────────────────────────

    /// <summary>
    /// Composite key สำหรับระบุคอร์ส (class_id + trainer_id)
    /// ใช้ส่งผ่าน UI Tag เช่น "TA04|220250001"
    /// </summary>
    public string CompositeKey => $"{ClassId}|{TrainerId}";

    /// <summary>
    /// ดึง CourseKey record จาก composite key
    /// </summary>
    public CourseKey GetCourseKey() => new(ClassId, TrainerId);

    // ─── Display texts ────────────────────────────────────────

    /// <summary>ราคาแสดงผล เช่น "2,200"</summary>
    public string ClassRateText => ClassRate > 0 ? $"{ClassRate:N0}" : "-";

    /// <summary>จำนวนครั้งแสดงผล เช่น "4 ครั้ง", "รายเดือน", "ครั้งละ"</summary>
    public string SessionCountText => CoursePricingHelper.GetSessionDisplayText(ClassTime);

    /// <summary>ประเภทคอร์ส เช่น "Adult Class"</summary>
    public string CourseTypeDescription => GetCourseTypeDescription(ClassId);

    /// <summary>แสดงชื่อคอร์ส + จำนวนครั้ง เช่น "Adult Class (4 ครั้ง)"</summary>
    public string FullDisplayName => $"{ClassTitle} ({SessionCountText})";

    /// <summary>แสดงรหัส + ชื่อ + ราคา + อาจารย์ เช่น "TA04 - Adult 4 ครั้ง ฿2,200 [ครูมี]"</summary>
    public string ComboBoxDisplayText => $"{ClassId} - {ClassTitle} {SessionCountText} ฿{ClassRate:N0} [{TrainerDisplayName}]";

    public string LastUpdatedText => LastUpdated.HasValue ? LastUpdated.Value.ToString("dd/MM") : "-";
    public string TrainerDisplayName => string.IsNullOrWhiteSpace(TrainerName) ? "ไม่ระบุ" : TrainerName;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Factory method — ระบบใหม่: PK = (class_id, trainer_id)
    /// </summary>
    public static CourseItem FromDatabase(
        string classId,
        string classTitle,
        int classTime,
        int classDuration,
        int classRate,
        string? trainerId,
        string? trainerName = null,
        DateTime? lastUpdated = null)
    {
        return new CourseItem
        {
            ClassId = classId,
            ClassTitle = classTitle,
            ClassTime = classTime,
            ClassDuration = classDuration,
            ClassRate = classRate,
            TrainerId = trainerId ?? string.Empty,
            TrainerName = trainerName ?? string.Empty,
            LastUpdated = lastUpdated
        };
    }

    /// <summary>
    /// Factory method — สร้างจากประเภทคอร์ส + จำนวนครั้ง + อาจารย์
    /// ราคาและชื่อดึงอัตโนมัติจาก CoursePricingHelper
    /// </summary>
    public static CourseItem Create(string courseType, int sessions, string trainerId, string? trainerName = null)
    {
        var classId = CoursePricingHelper.GenerateClassId(courseType, sessions);
        var price = CoursePricingHelper.GetPrice(courseType, sessions);
        var title = CoursePricingHelper.GetCourseName(courseType);

        return new CourseItem
        {
            ClassId = classId,
            ClassTitle = title,
            ClassTime = sessions,
            ClassDuration = 1,
            ClassRate = price > 0 ? price : 0,
            TrainerId = trainerId,
            TrainerName = trainerName ?? string.Empty
        };
    }

    /// <summary>
    /// Validate Course ID format: XXYY (XX=type, YY=sessions)
    /// </summary>
    public static bool IsValidClassId(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId) || classId.Length != 4)
            return false;

        if (!char.IsLetter(classId[0]) || !char.IsLetterOrDigit(classId[1]))
            return false;

        if (!char.IsDigit(classId[2]) || !char.IsDigit(classId[3]))
            return false;

        var prefix = classId[..2].ToUpperInvariant();
        return CoursePricingHelper.IsValidCourseType(prefix);
    }

    /// <summary>
    /// Get course type description from class_id prefix
    /// </summary>
    public static string GetCourseTypeDescription(string classId)
    {
        if (string.IsNullOrEmpty(classId) || classId.Length < 2) return "Unknown";
        return CoursePricingHelper.GetCourseName(classId[..2]);
    }
}
