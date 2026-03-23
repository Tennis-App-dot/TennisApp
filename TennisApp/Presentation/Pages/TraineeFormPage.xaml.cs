using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using TennisApp.Models;
using TennisApp.Services;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace TennisApp.Presentation.Pages;

public sealed partial class TraineeFormPage : Page
{
    private readonly DatabaseService _databaseService;
    private byte[]? _selectedImageData;
    private NotificationService? _notify;

    public TraineeFormPage()
    {
        InitializeComponent();
        _databaseService = ((App)Application.Current).DatabaseService;
        _databaseService.EnsureInitialized();
        this.Loaded += (s, e) => _notify = NotificationService.GetFromPage(this);
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
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
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
            var nextId = await _databaseService.Trainees.GetNextTraineeIdAsync();

            DateTime? birthDate = null;
            if (DateBirthDate.Date.Year > 1900)
                birthDate = DateBirthDate.Date.DateTime;

            var newTrainee = new TraineeItem
            {
                TraineeId = nextId,
                FirstName = TxtFirstName.Text.Trim(),
                LastName = TxtLastName.Text.Trim(),
                Nickname = string.IsNullOrWhiteSpace(TxtNickname.Text) ? string.Empty : TxtNickname.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? string.Empty : TxtPhone.Text.Trim(),
                BirthDate = birthDate,
                ImageData = _selectedImageData
            };

            var success = await _databaseService.Trainees.AddTraineeAsync(newTrainee);

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
