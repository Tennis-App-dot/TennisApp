using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.IO;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class CourtFormDialog : ContentDialog
{
    public CourtItem Result => (CourtItem)DataContext;
    private readonly bool _isEditMode;
    
    // ✅ เพิ่ม Property เพื่อตรวจสอบว่ากด Save หรือไม่
    public bool WasSaved { get; private set; } = false;

    public CourtFormDialog(CourtItem seed)
    {
        System.Diagnostics.Debug.WriteLine("CourtFormDialog constructor - เริ่มสร้าง dialog");
        
        InitializeComponent();
        DataContext = seed;

        // เช็คว่าเป็นโหมดแก้ไข (มี CourtID แล้ว) หรือโหมดเพิ่ม (ยังไม่มี CourtID)
        _isEditMode = !string.IsNullOrWhiteSpace(seed.CourtID);

        // สำหรับโหมดเพิ่ม ไม่ต้องกำหนด CourtID ให้ ViewModel จัดการ
        if (seed.LastUpdated == default)
            seed.LastUpdated = DateTime.Today;

        // ✅ Debug การโหลด DatePicker
        System.Diagnostics.Debug.WriteLine($"   CourtFormDialog initialized:");
        System.Diagnostics.Debug.WriteLine($"   Mode: {(_isEditMode ? "Edit" : "Add")}");
        System.Diagnostics.Debug.WriteLine($"   CourtID: {seed.CourtID}");
        System.Diagnostics.Debug.WriteLine($"   MaintenanceDate: {seed.MaintenanceDate:yyyy-MM-dd}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdated: {seed.LastUpdated:yyyy-MM-dd HH:mm:ss}");

        // ซ่อนข้อความ "แก้ไขข้อมูลล่าสุด" ในโหมดเพิ่ม + บังคับความกว้าง
        this.Loaded += (s, e) =>
        {
            SetLastModifiedVisibility();
            UpdateImagePlaceholder();
            ForceDialogWidth();
            UpdateMaintenanceDateDisplay();
        };
    }

    private void SetLastModifiedVisibility()
    {
        // ✅ หาค่า TextBlock ที่แสดง LastModifiedText พร้อม null check
        if (FindName("LastModifiedTextBlock") is TextBlock lastModifiedTextBlock)
        {
            lastModifiedTextBlock.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        }
        if (FindName("LastModifiedIcon") is FontIcon lastModifiedIcon)
        {
            lastModifiedIcon.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateImagePlaceholder()
    {
        if (FindName("ImagePlaceholder") is StackPanel placeholder)
        {
            bool hasImage = Result?.ImageData != null && Result.ImageData.Length > 0;
            placeholder.Visibility = hasImage ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// บังคับให้ dialog กว้างเต็มจอ (ลบ margin เล็กน้อย) บน Android
    /// ใช้ XamlRoot.Size ซึ่งให้ค่าเป็น DIPs (density-independent pixels)
    /// </summary>
    private void ForceDialogWidth()
    {
        try
        {
            double availableWidth = 0;

            // ✅ ใช้ XamlRoot.Size (DIPs) — ถูกต้องบน Android
            if (this.XamlRoot?.Size.Width > 0)
            {
                availableWidth = this.XamlRoot.Size.Width;
            }
            else if (this.ActualWidth > 0)
            {
                availableWidth = this.ActualWidth;
            }

            if (availableWidth > 0)
            {
                // ลบ margin ซ้าย-ขวา (24+24) เพื่อให้ไม่ชิดขอบ
                var targetWidth = availableWidth - 48;

                // จำกัดไม่ให้เล็กเกินหรือใหญ่เกิน
                if (targetWidth < 300) targetWidth = 300;
                if (targetWidth > 600) targetWidth = 600;

                this.MinWidth = targetWidth;
                this.MaxWidth = targetWidth;
                this.Width = targetWidth;

                System.Diagnostics.Debug.WriteLine($"✅ ForceDialogWidth: {targetWidth}px (available: {availableWidth}px)");
            }
            else
            {
                // Fallback: ใช้ค่าคงที่ที่เหมาะกับมือถือ
                this.MinWidth = 380;
                this.MaxWidth = 380;
                this.Width = 380;
                System.Diagnostics.Debug.WriteLine("✅ ForceDialogWidth: fallback 380px");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ ForceDialogWidth failed: {ex.Message}");
        }
    }

    /// <summary>
    /// จัดการการเลือกรูปภาพ
    /// </summary>
    private async void ImageUploadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await Services.ImagePickerService.PickAndCropRectangleAsync(this.XamlRoot!);

            if (result is null) return;

            if (result.IsSuccess && result.ImageData != null && Result != null)
            {
                Result.ImageData = result.ImageData;
                await UpdateImagePreview(result.ImageData);
                UpdateImagePlaceholder();

                if (sender is Button button && button.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            tb.Text = "เปลี่ยนรูปภาพ";
                            break;
                        }
                    }
                }
            }
            else if (!result.IsCancelled && result.ErrorTitle != null)
            {
                await ShowErrorMessage(result.ErrorTitle, result.ErrorMessage ?? "");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessage("เกิดข้อผิดพลาด", $"ไม่สามารถเลือกรูปภาพได้: {ex.Message}");
        }
    }

    /// <summary>
    /// อัปเดตการแสดงผลรูปภาพจาก byte array
    /// </summary>
    private async Task UpdateImagePreview(byte[] imageData)
    {
        try
        {
            // ✅ ตรวจสอบ FindName result ก่อนใช้งาน
            if (FindName("ImgPreview") is Image imgPreview)
            {
                var bitmap = await TennisApp.Helpers.ImageHelper.CreateBitmapFromBytesAsync(imageData);
                if (bitmap != null)
                {
                    imgPreview.Source = bitmap;
                }
            }
        }
        catch (Exception ex)
        {
            // หากโหลดไม่ได้ ให้แสดงรูปเดิม
            System.Diagnostics.Debug.WriteLine($" UpdateImagePreview Error: {ex.Message}");
        }
    }

    /// <summary>
    /// แสดงข้อความผิดพลาด
    /// </summary>
    private async Task ShowErrorMessage(string title, string message)
    {
        // ✅ ตรวจสอบ XamlRoot ก่อนใช้งาน
        if (this.XamlRoot != null)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "ตกลง",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
        else
        {
            // Fallback: แสดงใน Debug console
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("SaveButton_Click - เริ่มทำงาน");
        
        if (Result == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ Result is null");
            return;
        }
        
        if (Result.Status != "0" && Result.Status != "1")
        {
            System.Diagnostics.Debug.WriteLine("❌ Status ไม่ถูกต้อง");
            return;
        }

        // ✅ MaintenanceDate ถูกตั้งค่าจาก DatePicker โดยอัตโนมัติผ่าน Binding แล้ว
        // ✅ LastUpdated = วันที่+เวลาจริงที่กดบันทึก (ระบบตั้งอัตโนมัติ)
        Result.LastUpdated = DateTime.Now;

        System.Diagnostics.Debug.WriteLine($"✅ กำลังบันทึก:");
        System.Diagnostics.Debug.WriteLine($"   CourtID: {Result.CourtID}");
        System.Diagnostics.Debug.WriteLine($"   Status: {Result.Status}");
        System.Diagnostics.Debug.WriteLine($"   MaintenanceDate (ผู้ใช้เลือก): {Result.MaintenanceDate:yyyy-MM-dd}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdated (ระบบ): {Result.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        
        WasSaved = true;
        Hide();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("CancelButton_Click - ยกเลิก");
        
        // ✅ ไม่ได้กด Save
        WasSaved = false;
        
        // Close dialog without saving
        Hide();
    }

    private void UpdateMaintenanceDateDisplay()
    {
        if (Result is not null && Result.MaintenanceDate != default && Result.MaintenanceDate != DateTime.MinValue)
        {
            TxtMaintenanceDateDisplay.Text = Result.MaintenanceDate.ToString("dd/MM/yyyy");
            TxtMaintenanceDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        }
    }

    private async void BtnPickMaintenanceDate_Click(object sender, RoutedEventArgs e)
    {
        DateTime? currentDate = (Result is not null && Result.MaintenanceDate != default && Result.MaintenanceDate != DateTime.MinValue)
            ? Result.MaintenanceDate
            : null;

        var result = await DatePickerDialog.ShowAsync(this.XamlRoot!, currentDate, allowPastDates: true);
        if (result.HasValue && Result != null)
        {
            Result.MaintenanceDate = result.Value;
            TxtMaintenanceDateDisplay.Text = result.Value.ToString("dd/MM/yyyy");
            TxtMaintenanceDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        }
    }
}
