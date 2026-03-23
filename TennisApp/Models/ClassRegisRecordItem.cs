using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Represents a course registration record (ClassRegisRecord table)
/// Links trainee to course with registration date
/// </summary>
public class ClassRegisRecordItem : INotifyPropertyChanged
{
    private string _traineeId = string.Empty;
    private string _classId = string.Empty;
    private string _trainerId = string.Empty;
    private DateTime _regisDate = DateTime.Now;
    
    // Display properties (from JOINs)
    private string _traineeName = string.Empty;
    private string _traineePhone = string.Empty;
    private string _className = string.Empty;
    private int _classTime;
    private int _classRate;
    private string _trainerName = string.Empty;
    private int _rowNumber;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// trainee_id: FK to Trainee (PK, FK1)
    /// </summary>
    public string TraineeId
    {
        get => _traineeId;
        set
        {
            if (_traineeId != value)
            {
                _traineeId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// class_id: FK to Course (PK, FK2) — part of composite FK (class_id, trainer_id)
    /// </summary>
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

    /// <summary>
    /// trainer_id: FK to Course (FK3) — part of composite FK (class_id, trainer_id)
    /// </summary>
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

    /// <summary>
    /// regis_date: Registration date
    /// </summary>
    public DateTime RegisDate
    {
        get => _regisDate;
        set
        {
            if (_regisDate != value)
            {
                _regisDate = value;
                OnPropertyChanged();
            }
        }
    }

    // Display properties for UI (from JOINs)

    public string TraineeName
    {
        get => _traineeName;
        set
        {
            if (_traineeName != value)
            {
                _traineeName = value;
                OnPropertyChanged();
            }
        }
    }

    public string TraineePhone
    {
        get => _traineePhone;
        set
        {
            if (_traineePhone != value)
            {
                _traineePhone = value;
                OnPropertyChanged();
            }
        }
    }

    public string ClassName
    {
        get => _className;
        set
        {
            if (_className != value)
            {
                _className = value;
                OnPropertyChanged();
            }
        }
    }

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

    /// <summary>
    /// Row number for display in list
    /// </summary>
    public int RowNumber
    {
        get => _rowNumber;
        set
        {
            if (_rowNumber != value)
            {
                _rowNumber = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Format registration date as Thai format
    /// </summary>
    public string RegisDateFormatted => RegisDate.ToString("dd/MM/yyyy");

    public string ClassTimeText => ClassTime > 0 ? $"{ClassTime} ครั้ง" : "-";

    public string ClassRateText => ClassRate > 0 ? $"฿{ClassRate:N0}" : "-";

    public string TrainerDisplayName => string.IsNullOrWhiteSpace(TrainerName) ? "ไม่ระบุ" : TrainerName;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Factory method for creating ClassRegisRecordItem from database
    /// </summary>
    public static ClassRegisRecordItem FromDatabase(
        string traineeId,
        string classId,
        DateTime regisDate,
        string? traineeName = null,
        string? traineePhone = null,
        string? className = null,
        int classTime = 0,
        int classRate = 0,
        string? trainerName = null,
        string? trainerId = null)
    {
        return new ClassRegisRecordItem
        {
            TraineeId = traineeId,
            ClassId = classId,
            TrainerId = trainerId ?? string.Empty,
            RegisDate = regisDate,
            TraineeName = traineeName ?? string.Empty,
            TraineePhone = traineePhone ?? string.Empty,
            ClassName = className ?? string.Empty,
            ClassTime = classTime,
            ClassRate = classRate,
            TrainerName = trainerName ?? string.Empty
        };
    }
}
