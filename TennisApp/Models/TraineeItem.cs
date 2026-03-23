using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace TennisApp.Models;

public class TraineeItem : INotifyPropertyChanged
{
    private byte[]? _imageData;
    private BitmapImage? _imageSource;
    private string _traineeId = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _nickname = string.Empty;
    private DateTime? _birthDate;
    private string _phone = string.Empty;
    private string _imagePath = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    // trainee_id: "120250001" (9 digits)
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

    // trainee_fname: varchar(50), not null
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

    // trainee_lname: varchar(50), not null
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

    // trainee_nickname: varchar(50), allow null
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

    // trainee_birthdate: datetime, allow null
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

    // trainee_phone: string(10), allow null
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

    // trainee_img: varchar(100), allow null
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

    public bool HasNickname => !string.IsNullOrWhiteSpace(Nickname);

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
    /// Factory method for creating TraineeItem from database
    /// </summary>
    public static TraineeItem FromDatabase(
        string traineeId,
        string firstName,
        string lastName,
        string? nickname,
        DateTime? birthDate,
        string? phone,
        byte[]? imageData)
    {
        return new TraineeItem
        {
            TraineeId = traineeId,
            FirstName = firstName,
            LastName = lastName,
            Nickname = nickname ?? string.Empty,
            BirthDate = birthDate,
            Phone = phone ?? string.Empty,
            ImageData = imageData
        };
    }

    /// <summary>
    /// Generate next trainee ID for current year
    /// Pattern: 1YYYY#### (1 = trainee type, YYYY = year, #### = running number)
    /// </summary>
    public static string GenerateTraineeId(int currentYear, int runningNumber)
    {
        return $"1{currentYear:D4}{runningNumber:D4}";
    }

    /// <summary>
    /// Parse trainee ID to get year and running number
    /// </summary>
    public static (int year, int runningNumber) ParseTraineeId(string traineeId)
    {
        if (traineeId.Length != 9 || !traineeId.StartsWith("1"))
        {
            throw new ArgumentException("Invalid trainee ID format");
        }

        var yearStr = traineeId.Substring(1, 4);
        var runningStr = traineeId.Substring(5, 4);

        return (int.Parse(yearStr), int.Parse(runningStr));
    }
}
