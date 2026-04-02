using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Presentation.Dialogs;
using System.Text.RegularExpressions;
using TennisApp.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace TennisApp.Presentation.Pages;

public sealed partial class TraineeEditPage : Page
{
    private readonly DatabaseService _databaseService;
    private TraineeItem? _currentTrainee;
    private string? _traineeId;
    private byte[]? _selectedImageData;
    private NotificationService? _notify;
    private DateTime? _selectedBirthDate;

    public TraineeEditPage()
    {
        InitializeComponent();
        _databaseService = ((App)Application.Current).DatabaseService;
        _databaseService.EnsureInitialized();
        this.Loaded += (s, e) =>
        {
            _notify = NotificationService.GetFromPage(this);
            InputScrollHelper.Attach(this);
        };
        this.Unloaded += (s, e) => InputScrollHelper.Detach(this);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string traineeId)
        {
            _traineeId = traineeId;
            await LoadTraineeDataAsync(traineeId);
        }
    }

    private async System.Threading.Tasks.Task LoadTraineeDataAsync(string traineeId)
    {
        try
        {
            _currentTrainee = await _databaseService.Trainees.GetTraineeByIdAsync(traineeId);

            if (_currentTrainee != null)
            {
                TxtFirstName.Text = _currentTrainee.FirstName;
                TxtLastName.Text = _currentTrainee.LastName;
                TxtPhone.Text = _currentTrainee.Phone ?? string.Empty;
                TxtNickname.Text = _currentTrainee.Nickname ?? string.Empty;

                if (_currentTrainee.BirthDate.HasValue)
                {
                    _selectedBirthDate = _currentTrainee.BirthDate.Value;
                    TxtBirthDateDisplay.Text = _currentTrainee.BirthDate.Value.ToString("dd/MM/yyyy");
                    TxtBirthDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
                }

                if (_currentTrainee.ImageData != null && _currentTrainee.ImageData.Length > 0)
                {
                    var bitmap = await Services.ImagePickerService.CreateBitmapAsync(_currentTrainee.ImageData);
                    if (bitmap != null)
                    {
                        ProfileImage.Source = bitmap;
                        ProfileImage.Visibility = Visibility.Visible;
                        PlaceholderIcon.Visibility = Visibility.Collapsed;
                    }
                    _selectedImageData = _currentTrainee.ImageData;
                }
            }
            else
            {
                _notify?.ShowError("ไม่พบข้อมูลผู้เรียน");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}");
        }
    }

    private async void BtnPickBirthDate_Click(object sender, RoutedEventArgs e)
    {
        var result = await DatePickerDialog.ShowAsync(this.XamlRoot!, _selectedBirthDate, allowPastDates: true);
        if (result.HasValue)
        {
            _selectedBirthDate = result.Value;
            TxtBirthDateDisplay.Text = result.Value.ToString("dd/MM/yyyy");
            TxtBirthDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        }
    }

    private async void BtnUploadPhoto_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await Services.ImagePickerService.PickAndCropCircleAsync(this.XamlRoot!);

            if (result.IsSuccess && result.ImageData != null)
            {
                _selectedImageData = result.ImageData;
                var bitmap = await Services.ImagePickerService.CreateBitmapAsync(result.ImageData);
                if (bitmap != null)
                {
                    ProfileImage.Source = bitmap;
                    ProfileImage.Visibility = Visibility.Visible;
                    PlaceholderIcon.Visibility = Visibility.Collapsed;
                }
            }
            else if (!result.IsCancelled && result.ErrorTitle != null)
            {
                _notify?.ShowWarning(result.ErrorMessage ?? result.ErrorTitle);
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"เกิดข้อผิดพลาดในการเลือกรูปภาพ: {ex.Message}");
        }
    }

    private void TxtPhone_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var originalText = textBox.Text;
            var selectionStart = textBox.SelectionStart;

            var numericText = Regex.Replace(originalText, @"[^\d]", "");

            if (originalText != numericText)
            {
                textBox.Text = numericText;
                textBox.SelectionStart = Math.Min(selectionStart, numericText.Length);
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTrainee == null)
        {
            _notify?.ShowError("ไม่พบข้อมูลผู้เรียนที่จะแก้ไข");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
        {
            _notify?.ShowWarning("กรุณากรอกชื่อ");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLastName.Text))
        {
            _notify?.ShowWarning("กรุณากรอกนามสกุล");
            return;
        }

        try
        {
            _currentTrainee.FirstName = TxtFirstName.Text.Trim();
            _currentTrainee.LastName = TxtLastName.Text.Trim();
            _currentTrainee.Nickname = string.IsNullOrWhiteSpace(TxtNickname.Text) ? string.Empty : TxtNickname.Text.Trim();
            _currentTrainee.Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? string.Empty : TxtPhone.Text.Trim();
            _currentTrainee.BirthDate = _selectedBirthDate;
            _currentTrainee.ImageData = _selectedImageData;

            var success = await _databaseService.Trainees.UpdateTraineeAsync(_currentTrainee);

            if (success)
            {
                _notify?.ShowSuccess("บันทึกข้อมูลผู้เรียนเรียบร้อยแล้ว");
                if (Frame.CanGoBack) Frame.GoBack();
            }
            else
            {
                _notify?.ShowError("เกิดข้อผิดพลาดในการบันทึกข้อมูล");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }
}
