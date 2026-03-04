using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

public class CourseItem : INotifyPropertyChanged
{
    private string _classId = string.Empty;
    private string _classTitle = string.Empty;
    private int _classTime;
    private int _classDuration;
    private int _classRate;
    private string _trainerId = string.Empty;
    private string _trainerName = string.Empty;

    // Tier pricing
    private int _classRatePerTime;
    private int _classRate4;
    private int _classRate8;
    private int _classRate12;
    private int _classRate16;
    private int _classRateMonthly;
    private int _classRateNight;
    private DateTime? _lastUpdated;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClassId
    {
        get => _classId;
        set { if (_classId != value) { _classId = value; OnPropertyChanged(); } }
    }

    public string ClassTitle
    {
        get => _classTitle;
        set { if (_classTitle != value) { _classTitle = value; OnPropertyChanged(); } }
    }

    public int ClassTime
    {
        get => _classTime;
        set { if (_classTime != value) { _classTime = value; OnPropertyChanged(); } }
    }

    public int ClassDuration
    {
        get => _classDuration;
        set { if (_classDuration != value) { _classDuration = value; OnPropertyChanged(); } }
    }

    public int ClassRate
    {
        get => _classRate;
        set { if (_classRate != value) { _classRate = value; OnPropertyChanged(); } }
    }

    public string TrainerId
    {
        get => _trainerId;
        set { if (_trainerId != value) { _trainerId = value; OnPropertyChanged(); } }
    }

    public string TrainerName
    {
        get => _trainerName;
        set { if (_trainerName != value) { _trainerName = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrainerDisplayName)); } }
    }

    // ─── Tier pricing ──────────────────────────────────────────
    public int ClassRatePerTime
    {
        get => _classRatePerTime;
        set { if (_classRatePerTime != value) { _classRatePerTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRatePerTimeText)); } }
    }

    public int ClassRate4
    {
        get => _classRate4;
        set { if (_classRate4 != value) { _classRate4 = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRate4Text)); } }
    }

    public int ClassRate8
    {
        get => _classRate8;
        set { if (_classRate8 != value) { _classRate8 = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRate8Text)); } }
    }

    public int ClassRate12
    {
        get => _classRate12;
        set { if (_classRate12 != value) { _classRate12 = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRate12Text)); } }
    }

    public int ClassRate16
    {
        get => _classRate16;
        set { if (_classRate16 != value) { _classRate16 = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRate16Text)); } }
    }

    public int ClassRateMonthly
    {
        get => _classRateMonthly;
        set { if (_classRateMonthly != value) { _classRateMonthly = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClassRateMonthlyText)); } }
    }

    public int ClassRateNight
    {
        get => _classRateNight;
        set { if (_classRateNight != value) { _classRateNight = value; OnPropertyChanged(); } }
    }

    public DateTime? LastUpdated
    {
        get => _lastUpdated;
        set { if (_lastUpdated != value) { _lastUpdated = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastUpdatedText)); } }
    }

    // ─── Display texts ────────────────────────────────────────
    public string ClassRatePerTimeText => ClassRatePerTime > 0 ? $"{ClassRatePerTime:N0}" : "-";
    public string ClassRate4Text => ClassRate4 > 0 ? $"{ClassRate4:N0}" : "-";
    public string ClassRate8Text => ClassRate8 > 0 ? $"{ClassRate8:N0}" : "-";
    public string ClassRate12Text => ClassRate12 > 0 ? $"{ClassRate12:N0}" : "-";
    public string ClassRate16Text => ClassRate16 > 0 ? $"{ClassRate16:N0}" : "-";
    public string ClassRateMonthlyText => ClassRateMonthly > 0 ? $"{ClassRateMonthly:N0}" : "-";
    public string LastUpdatedText => LastUpdated.HasValue ? LastUpdated.Value.ToString("dd/MM") : "-";
    public string TrainerDisplayName => string.IsNullOrWhiteSpace(TrainerName) ? "ไม่ระบุ" : TrainerName;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Factory method for creating CourseItem from database
    /// </summary>
    public static CourseItem FromDatabase(
        string classId,
        string classTitle,
        int classTime,
        int classDuration,
        int classRate,
        string? trainerId,
        string? trainerName = null,
        int classRatePerTime = 0,
        int classRate4 = 0,
        int classRate8 = 0,
        int classRate12 = 0,
        int classRate16 = 0,
        int classRateMonthly = 0,
        int classRateNight = 0,
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
            ClassRatePerTime = classRatePerTime,
            ClassRate4 = classRate4,
            ClassRate8 = classRate8,
            ClassRate12 = classRate12,
            ClassRate16 = classRate16,
            ClassRateMonthly = classRateMonthly,
            ClassRateNight = classRateNight,
            LastUpdated = lastUpdated
        };
    }

    /// <summary>
    /// Validate Course ID format
    /// Format: XX## (Type + Sessions)
    /// Examples: TA01, T104, P201
    /// </summary>
    public static bool IsValidClassId(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId) || classId.Length != 4)
            return false;

        // Check first 2 chars are letters
        if (!char.IsLetter(classId[0]) || !char.IsLetter(classId[1]))
            return false;

        // Check last 2 chars are digits
        if (!char.IsDigit(classId[2]) || !char.IsDigit(classId[3]))
            return false;

        // Check valid type prefixes
        var prefix = classId.Substring(0, 2).ToUpper();
        var validPrefixes = new[] { "TA", "T1", "T2", "T3", "P1", "P2", "P3" };
        
        return Array.Exists(validPrefixes, p => p == prefix);
    }

    /// <summary>
    /// Get course type description from class_id
    /// </summary>
    public static string GetCourseTypeDescription(string classId)
    {
        if (classId.Length < 2) return "Unknown";

        return classId.Substring(0, 2).ToUpper() switch
        {
            "TA" => "Adult Class",
            "T1" => "Kids Class",
            "T2" => "Intermediate Class",
            "T3" => "Competitive Class",
            "P1" => "Private & Master Coach",
            "P2" => "Private & Standard Coach (Day)",
            "P3" => "Private & Standard Coach (Night)",
            _ => "Unknown"
        };
    }
}
