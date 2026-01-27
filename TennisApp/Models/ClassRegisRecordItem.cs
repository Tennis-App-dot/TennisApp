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
    private DateTime _regisDate = DateTime.Now;
    
    // Display properties (from JOINs)
    private string _traineeName = string.Empty;
    private string _traineePhone = string.Empty;
    private string _className = string.Empty;
    private int _classTime;
    private int _classRate;

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
    /// class_id: FK to Class/Course (PK, FK2)
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

    /// <summary>
    /// Format registration date as Thai format
    /// </summary>
    public string RegisDateFormatted => RegisDate.ToString("dd/MM/yyyy");

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
        int classRate = 0)
    {
        return new ClassRegisRecordItem
        {
            TraineeId = traineeId,
            ClassId = classId,
            RegisDate = regisDate,
            TraineeName = traineeName ?? string.Empty,
            TraineePhone = traineePhone ?? string.Empty,
            ClassName = className ?? string.Empty,
            ClassTime = classTime,
            ClassRate = classRate
        };
    }
}
