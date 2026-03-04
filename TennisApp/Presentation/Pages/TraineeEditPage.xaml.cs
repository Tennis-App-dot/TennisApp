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

    public TraineeEditPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _databaseService.EnsureInitialized();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string traineeId)
        {
            _traineeId = traineeId;
            System.Diagnostics.Debug.WriteLine($"TraineeEditPage: Loading trainee {traineeId}");
            await LoadTraineeDataAsync(traineeId);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("TraineeEditPage: No trainee ID provided");
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
                    DateBirthDate.Date = new DateTimeOffset(_currentTrainee.BirthDate.Value);
                }

                // โหลดรูปภาพ
                if (_currentTrainee.ImageData != null && _currentTrainee.ImageData.Length > 0)
                {
                    await LoadImageFromBytes(_currentTrainee.ImageData);
                    _selectedImageData = _currentTrainee.ImageData;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded trainee: {_currentTrainee.FullName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Trainee not found: {traineeId}");
                await ShowErrorDialog("ไม่พบข้อมูลผู้เรียน");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainee: {ex.Message}");
            await ShowErrorDialog($"เกิดข้อผิดพลาดในการโหลดข้อมูล: {ex.Message}");
        }
    }

    private async void BtnUploadPhoto_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            
            // Get the window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var file = await picker.PickSingleFileAsync();
            
            if (file != null)
            {
                System.Diagnostics.Debug.WriteLine($"📸 Selected file: {file.Name}");
                
                // ตรวจสอบขนาดไฟล์ (3MB = 3 * 1024 * 1024 bytes)
                var properties = await file.GetBasicPropertiesAsync();
                if (properties.Size > 3 * 1024 * 1024)
                {
                    await ShowErrorDialog("ขนาดไฟล์เกิน 3MB กรุณาเลือกไฟล์ที่เล็กกว่า");
                    return;
                }
                
                // อ่านไฟล์เป็น byte array
                using var stream = await file.OpenReadAsync();
                using var memoryStream = new System.IO.MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                var originalImageData = memoryStream.ToArray();
                
                System.Diagnostics.Debug.WriteLine($"✅ Image loaded: {originalImageData.Length} bytes");
                
                // เปิด dialog สำหรับ crop รูปภาพ
                var croppedImageData = await Helpers.ImageHelper.ShowImageCropperAsync(originalImageData);
                
                if (croppedImageData != null)
                {
                    _selectedImageData = croppedImageData;
                    System.Diagnostics.Debug.WriteLine($"✅ Image cropped: {_selectedImageData.Length} bytes");
                    
                    // แสดงรูปภาพที่ crop แล้ว
                    await LoadImageFromBytes(_selectedImageData);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️ User cancelled cropping");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error picking image: {ex.Message}");
            await ShowErrorDialog($"เกิดข้อผิดพลาดในการเลือกรูปภาพ: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task LoadImageFromBytes(byte[] imageData)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageData.AsBuffer());
            stream.Seek(0);
            
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            
            ProfileImage.Source = bitmap;
            ProfileImage.Visibility = Visibility.Visible;
            PlaceholderIcon.Visibility = Visibility.Collapsed;
            
            System.Diagnostics.Debug.WriteLine("✅ Image displayed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading image: {ex.Message}");
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
            await ShowErrorDialog("ไม่พบข้อมูลผู้เรียนที่จะแก้ไข");
            return;
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
        {
            await ShowErrorDialog("กรุณากรอกชื่อ");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLastName.Text))
        {
            await ShowErrorDialog("กรุณากรอกนามสกุล");
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
            _currentTrainee.Nickname = string.IsNullOrWhiteSpace(TxtNickname.Text) ? null : TxtNickname.Text.Trim();
            _currentTrainee.Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim();
            _currentTrainee.BirthDate = birthDate;
            _currentTrainee.ImageData = _selectedImageData; // อัปเดตรูปภาพ

            System.Diagnostics.Debug.WriteLine($"Updating trainee: {_currentTrainee.FullName} (ID: {_currentTrainee.TraineeId})");
            System.Diagnostics.Debug.WriteLine($"Image data: {_selectedImageData?.Length ?? 0} bytes");

            // Save to database
            var success = await _databaseService.Trainees.UpdateTraineeAsync(_currentTrainee);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("✅ Trainee updated successfully");
                await ShowSuccessDialog("บันทึกข้อมูลผู้เรียนเรียบร้อยแล้ว");
                
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to update trainee");
                await ShowErrorDialog("เกิดข้อผิดพลาดในการบันทึกข้อมูล");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating trainee: {ex.Message}");
            await ShowErrorDialog($"เกิดข้อผิดพลาด: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "ข้อผิดพลาด",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var contentTextBlock = new TextBlock
        {
            Text = message,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dialog = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            CloseButtonText = "ตรวจสอบ",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ShowSuccessDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "สำเร็จ",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var contentTextBlock = new TextBlock
        {
            Text = message,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dialog = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        await dialog.ShowAsync();
    }
}
