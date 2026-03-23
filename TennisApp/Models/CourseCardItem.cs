using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Display model for course selection card in RegisterCoursePage.
/// Shows course info + default price + trainer + registration count.
/// </summary>
public class CourseCardItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _registrationCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CourseItem Course { get; }

    public string ClassId => Course.ClassId;
    public string ClassTitle => Course.ClassTitle;
    public string TrainerName => Course.TrainerDisplayName;
    public string TrainerId => Course.TrainerId;
    public string CompositeKey => Course.CompositeKey;
    public bool HasTrainer => !string.IsNullOrWhiteSpace(Course.TrainerName);

    public string SessionsText => Course.SessionCountText;

    public int DefaultPrice => Course.ClassRate;

    public string DefaultPriceText => DefaultPrice > 0 ? $"฿{DefaultPrice:N0}" : "-";

    public int RegistrationCount
    {
        get => _registrationCount;
        set
        {
            if (_registrationCount != value)
            {
                _registrationCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RegistrationCountText));
            }
        }
    }

    public string RegistrationCountText => $"{RegistrationCount} คนลง";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public CourseCardItem(CourseItem course, int registrationCount = 0)
    {
        Course = course;
        _registrationCount = registrationCount;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
