using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;
using System.Text.RegularExpressions;
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

    public TraineeEditPage()
    {
        InitializeComponent();
        _databaseService = ((App)Application.Current).DatabaseService;
        _databaseService.EnsureInitialized();
        this.Loaded += (s, e) => _notify = NotificationService.GetFromPage(this);
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
                    DateBirthDate.Date = new DateTimeOffset(_currentTrainee.BirthDate.Value);

                if (_currentTrainee.ImageData != null && _currentTrainee.ImageData.Length > 0)
                {
                    var bitmap = await Services.ImagePickerService.CreateBitmapAsync(_currentTrainee.ImageData);
                    if (bitmap != null)
                    {
                        ProfileImage.Source = bitmap;
                        ProfileImage.Visibility = Visibility.Visible;
                        PlaceholderIcon.Visibility = Visibility.Collapsed;
                    }
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

            // ลบตัวอักษรที่ไม่ใช่ตัวเลข (เก็บเฉพาะ 0-9)
            var numericText = Regex.Replace(originalText, @"[^\d]", "");

            if (originalText != numericText)
            {
                textBox.Text = numericText;
                // คืนตำแหน่ง cursor
                textBox.SelectionStart = Math.Min(selectionStart, numericText.Length);
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("TraineeEditPage: Cancel button clicked");
        
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("TraineeEditPage: Save button clicked");

        if (_currentTrainee == null)
        {
            _notify?.ShowError("ไม่พบข้อมูลผู้เรียนที่จะแก้ไข");
            return;
        }

        // Validate required fields
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
            // Get birth date if selected
            DateTime? birthDate = null;
            if (DateBirthDate.Date.Year > 1900)
            {
                birthDate = DateBirthDate.Date.DateTime;
            }
            
            // Update trainee data
            _currentTrainee.FirstName = TxtFirstName.Text.Trim();
            _currentTrainee.LastName = TxtLastName.Text.Trim();
            _currentTrainee.Nickname = string.IsNullOrWhiteSpace(TxtNickname.Text) ? string.Empty : TxtNickname.Text.Trim();
            _currentTrainee.Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? string.Empty : TxtPhone.Text.Trim();
            _currentTrainee.BirthDate = birthDate;
            _currentTrainee.ImageData = _selectedImageData; // อัปเดตรูปภาพ

            System.Diagnostics.Debug.WriteLine($"Updating trainee: {_currentTrainee.FullName} (ID: {_currentTrainee.TraineeId})");
            System.Diagnostics.Debug.WriteLine($"Image data: {_selectedImageData?.Length ?? 0} bytes");

            // Save to database
            var success = await _databaseService.Trainees.UpdateTraineeAsync(_currentTrainee);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("✅ Trainee updated successfully");
                _notify?.ShowSuccess("บันทึกข้อมูลผู้เรียนเรียบร้อยแล้ว");
                
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to update trainee");
                _notify?.ShowError("เกิดข้อผิดพลาดในการบันทึกข้อมูล");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating trainee: {ex.Message}");
            _notify?.ShowError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }
}
