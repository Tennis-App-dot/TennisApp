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

    public event PropertyChangedEventHandler? PropertyChanged;

    // class_id: "TA01" (4 characters)
    // Pattern: Type(2) + Sessions(2)
    // Examples: TA01, T104, P201
    public string ClassId
    {
        get => _classId;
        set
        {
            if (_classId != value)
            {
                _classId = value;
                OnPropertyChanged();
            }
        }
    }

    // class_title: varchar(50), not null
    public string ClassTitle
    {
        get => _classTitle;
        set
        {
            if (_classTitle != value)
            {
                _classTitle = value;
                OnPropertyChanged();
            }
        }
    }

    // class_time: int(2), not null
    // Number of sessions (1, 4, 8, 12)
    public int ClassTime
    {
        get => _classTime;
        set
        {
            if (_classTime != value)
            {
                _classTime = value;
                OnPropertyChanged();
            }
        }
    }

    // class_duration: int(1), nullable
    // Duration per session in hours
    public int ClassDuration
    {
        get => _classDuration;
        set
        {
            if (_classDuration != value)
            {
                _classDuration = value;
                OnPropertyChanged();
            }
        }
    }

    // class_rate: int(6), nullable
    // Course fee
    public int ClassRate
    {
        get => _classRate;
        set
        {
            if (_classRate != value)
            {
                _classRate = value;
                OnPropertyChanged();
            }
        }
    }

    // trainer_id: FK to Trainer
    public string TrainerId
    {
        get => _trainerId;
        set
        {
            if (_trainerId != value)
            {
                _trainerId = value;
                OnPropertyChanged();
            }
        }
    }

    // Computed property for display (from JOIN)
    public string TrainerName
    {
        get => _trainerName;
        set
        {
            if (_trainerName != value)
            {
                _trainerName = value;
                OnPropertyChanged();
            }
        }
    }

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
        string? trainerName = null)
    {
        return new CourseItem
        {
            ClassId = classId,
            ClassTitle = classTitle,
            ClassTime = classTime,
            ClassDuration = classDuration,
            ClassRate = classRate,
            TrainerId = trainerId ?? string.Empty,
            TrainerName = trainerName ?? string.Empty
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
