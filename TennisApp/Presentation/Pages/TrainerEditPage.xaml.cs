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

public sealed partial class TrainerEditPage : Page
{
    private readonly DatabaseService _databaseService;
    private TrainerItem? _currentTrainer;
    private string? _trainerId;
    private byte[]? _selectedImageData;
    private NotificationService? _notify;

    public TrainerEditPage()
    {
        InitializeComponent();
        _databaseService = ((App)Application.Current).DatabaseService;
        _databaseService.EnsureInitialized();
        this.Loaded += (s, e) => _notify = NotificationService.GetFromPage(this);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string trainerId)
        {
            _trainerId = trainerId;
            System.Diagnostics.Debug.WriteLine($"TrainerEditPage: Loading trainer {trainerId}");
            await LoadTrainerDataAsync(trainerId);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("TrainerEditPage: No trainer ID provided");
        }
    }

    private async System.Threading.Tasks.Task LoadTrainerDataAsync(string trainerId)
    {
        try
        {
            _currentTrainer = await _databaseService.Trainers.GetTrainerByIdAsync(trainerId);

            if (_currentTrainer != null)
            {
                TxtFirstName.Text = _currentTrainer.FirstName;
                TxtLastName.Text = _currentTrainer.LastName;
                TxtPhone.Text = _currentTrainer.Phone ?? string.Empty;
                TxtNickname.Text = _currentTrainer.Nickname ?? string.Empty;

                if (_currentTrainer.BirthDate.HasValue)
                    DateBirthDate.Date = new DateTimeOffset(_currentTrainer.BirthDate.Value);

                // โหลดรูปภาพ
                if (_currentTrainer.ImageData != null && _currentTrainer.ImageData.Length > 0)
                {
                    var bitmap = await Services.ImagePickerService.CreateBitmapAsync(_currentTrainer.ImageData);
                    if (bitmap != null)
                    {
                        ProfileImage.Source = bitmap;
                        ProfileImage.Visibility = Visibility.Visible;
                        PlaceholderIcon.Visibility = Visibility.Collapsed;
                    }
                    _selectedImageData = _currentTrainer.ImageData;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded trainer: {_currentTrainer.FullName}");
            }
            else
            {
                _notify?.ShowError("ไม่พบข้อมูลผู้ฝึกสอน");
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
        System.Diagnostics.Debug.WriteLine("TrainerEditPage: Cancel button clicked");
        
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("TrainerEditPage: Save button clicked");

        if (_currentTrainer == null)
        {
            _notify?.ShowError("ไม่พบข้อมูลผู้ฝึกสอนที่จะแก้ไข");
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
            
            // Update trainer data
            _currentTrainer.FirstName = TxtFirstName.Text.Trim();
            _currentTrainer.LastName = TxtLastName.Text.Trim();
            _currentTrainer.Nickname = string.IsNullOrWhiteSpace(TxtNickname.Text) ? string.Empty : TxtNickname.Text.Trim();
            _currentTrainer.Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? string.Empty : TxtPhone.Text.Trim();
            _currentTrainer.BirthDate = birthDate;
            _currentTrainer.ImageData = _selectedImageData; // อัปเดตรูปภาพ

            System.Diagnostics.Debug.WriteLine($"Updating trainer: {_currentTrainer.FullName} (ID: {_currentTrainer.TrainerId})");
            System.Diagnostics.Debug.WriteLine($"Image data: {_selectedImageData?.Length ?? 0} bytes");

            // Save to database
            var success = await _databaseService.Trainers.UpdateTrainerAsync(_currentTrainer);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("✅ Trainer updated successfully");
                _notify?.ShowSuccess("บันทึกข้อมูลผู้ฝึกสอนเรียบร้อยแล้ว");
                
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to update trainer");
                _notify?.ShowError("เกิดข้อผิดพลาดในการบันทึกข้อมูล");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating trainer: {ex.Message}");
            _notify?.ShowError($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }
}
