using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace TennisApp.Models;

public class TrainerItem : INotifyPropertyChanged
{
    private byte[]? _imageData;
    private BitmapImage? _imageSource;
    private string _trainerId = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _nickname = string.Empty;
    private DateTime? _birthDate;
    private string _phone = string.Empty;
    private string _imagePath = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    // trainer_id: "220250001" (9 digits)
    // Pattern: 2YYYY#### (2 = trainer type, YYYY = year, #### = running number)
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

    // trainer_fname: varchar(50), not null
    public string FirstName
    {
        get => _firstName;
        set
        {
            if (_firstName != value)
            {
                _firstName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    // trainer_lname: varchar(50), not null
    public string LastName
    {
        get => _lastName;
        set
        {
            if (_lastName != value)
            {
                _lastName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
            }
        }
    }

    // trainer_nickname: varchar(50), allow null
    public string Nickname
    {
        get => _nickname;
        set
        {
            if (_nickname != value)
            {
                _nickname = value;
                OnPropertyChanged();
            }
        }
    }

    // trainer_birthdate: datetime, allow null
    public DateTime? BirthDate
    {
        get => _birthDate;
        set
        {
            if (_birthDate != value)
            {
                _birthDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BirthDateForPicker));
                OnPropertyChanged(nameof(Age));
            }
        }
    }

    // trainer_phone: string(10), allow null
    public string Phone
    {
        get => _phone;
        set
        {
            if (_phone != value)
            {
                _phone = value;
                OnPropertyChanged();
            }
        }
    }

    // Trainer_img: varchar(100), allow null
    public string ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath != value)
            {
                _imagePath = value;
                OnPropertyChanged();
            }
        }
    }

    // Image data for database storage
    public byte[]? ImageData
    {
        get => _imageData;
        set
        {
            if (_imageData != value)
            {
                _imageData = value;
                OnPropertyChanged();
            }
        }
    }

    // Image source for UI binding
    public BitmapImage? ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource != value)
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }
    }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";

    public int? Age
    {
        get
        {
            if (!_birthDate.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - _birthDate.Value.Year;
            if (_birthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    // DateTimeOffset for DatePicker (WinUI requires DateTimeOffset)
    public DateTimeOffset? BirthDateForPicker
    {
        get => _birthDate.HasValue ? new DateTimeOffset(_birthDate.Value) : null;
        set
        {
            var newDate = value?.DateTime;
            if (_birthDate != newDate)
            {
                _birthDate = newDate;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BirthDate));
                OnPropertyChanged(nameof(Age));
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Factory method for creating TrainerItem from database
    /// </summary>
    public static TrainerItem FromDatabase(
        string trainerId,
        string firstName,
        string lastName,
        string? nickname,
        DateTime? birthDate,
        string? phone,
        byte[]? imageData)
    {
        return new TrainerItem
        {
            TrainerId = trainerId,
            FirstName = firstName,
            LastName = lastName,
            Nickname = nickname ?? string.Empty,
            BirthDate = birthDate,
            Phone = phone ?? string.Empty,
            ImageData = imageData
        };
    }

    /// <summary>
    /// Generate next trainer ID for current year
    /// Pattern: 2YYYY#### (2 = trainer type, YYYY = year, #### = running number)
    /// Example: 220250001 = Trainer registered in year 2025, sequence number 1
    /// </summary>
    public static string GenerateTrainerId(int currentYear, int runningNumber)
    {
        return $"2{currentYear:D4}{runningNumber:D4}";
    }

    /// <summary>
    /// Parse trainer ID to get year and running number
    /// </summary>
    public static (int year, int runningNumber) ParseTrainerId(string trainerId)
    {
        if (trainerId.Length != 9 || !trainerId.StartsWith("2"))
        {
            throw new ArgumentException("Invalid trainer ID format");
        }

        var yearStr = trainerId.Substring(1, 4);    // YYYY (full year)
        var runningStr = trainerId.Substring(5, 4);  // #### (4 digits)

        return (int.Parse(yearStr), int.Parse(runningStr));
    }
}
