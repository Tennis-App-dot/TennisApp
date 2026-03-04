using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// Wrapper around TraineeItem for multi-select in TraineeSelectionDialog.
/// Adds IsSelected and IsAlreadyRegistered flags.
/// </summary>
public class SelectableTraineeItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isAlreadyRegistered;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TraineeItem Trainee { get; }

    public string TraineeId => Trainee.TraineeId;
    public string FullName => Trainee.FullName;
    public string FirstName => Trainee.FirstName;
    public string LastName => Trainee.LastName;
    public string Nickname => Trainee.Nickname;
    public string Phone => Trainee.Phone;

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

    public bool IsAlreadyRegistered
    {
        get => _isAlreadyRegistered;
        set
        {
            if (_isAlreadyRegistered != value)
            {
                _isAlreadyRegistered = value;
                OnPropertyChanged();
            }
        }
    }

    public SelectableTraineeItem(TraineeItem trainee, bool isAlreadyRegistered = false)
    {
        Trainee = trainee;
        _isAlreadyRegistered = isAlreadyRegistered;
        _isSelected = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
