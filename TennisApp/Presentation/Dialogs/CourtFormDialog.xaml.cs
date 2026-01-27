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
        
        InitializeComponent(); // ← Fonts loaded from App.xaml automatically
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
        System.Diagnostics.Debug.WriteLine($"   LastUpdated: {seed.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdatedForDatePicker: {seed.LastUpdatedForDatePicker:yyyy-MM-dd}");

        // ซ่อนข้อความ "แก้ไขข้อมูลล่าสุด" ในโหมดเพิ่ม
        this.Loaded += (s, e) => SetLastModifiedVisibility();
    }

    private void SetLastModifiedVisibility()
    {
        // ✅ หาค่า TextBlock ที่แสดง LastModifiedText พร้อม null check
        if (FindName("LastModifiedTextBlock") is TextBlock lastModifiedTextBlock)
        {
            lastModifiedTextBlock.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// จัดการการเลือกรูปภาพ
    /// </summary>
    private async void ImageUploadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // สร้าง File Picker
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // ✅ ตั้งค่า Window Handle สำหรับ Win32 พร้อม null checks
            try
            {
                // ตรวจสอบ XamlRoot และ Content ก่อนใช้งาน
                if (this.XamlRoot?.Content != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this.XamlRoot.Content);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }
            }
            catch
            {
                // Fallback - ลองใช้วิธีอื่น
                System.Diagnostics.Debug.WriteLine("ไม่สามารถตั้งค่า Window Handle ได้");
            }

            // เลือกไฟล์
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // ตรวจสอบขนาดไฟล์ (ไม่เกิน 3MB)
                if (!await TennisApp.Helpers.ImageHelper.IsFileSizeValidAsync(file))
                {
                    await ShowErrorMessage("ไฟล์ใหญ่เกินไป", "ขนาดไฟล์ต้องไม่เกิน 3MB");
                    return;
                }

                // ตรวจสอบนามสกุลไฟล์
                if (!TennisApp.Helpers.ImageHelper.IsValidImageExtension(file.Name))
                {
                    await ShowErrorMessage("ไฟล์ไม่ถูกต้อง", "กรุณาเลือกไฟล์ JPEG หรือ PNG");
                    return;
                }

                // อ่านไฟล์เป็น bytes
                var bytes = await TennisApp.Helpers.ImageHelper.ConvertToByteArrayAsync(file);

                // ✅ เปิด ImageCropperDialog เพื่อให้ผู้ใช้ปรับตำแหน่งรูปภาพ
                var cropDialog = new ImageCropperDialog(bytes, CropShape.Rectangle, 534, 300)
                {
                    XamlRoot = this.XamlRoot
                };
                
                var cropResult = await cropDialog.ShowAsync();
                
                if (cropResult == ContentDialogResult.Primary && cropDialog.CroppedImageData != null)
                {
                    // ✅ ตรวจสอบ Result ก่อนใช้งาน
                    if (Result != null)
                    {
                        // อัปเดต Model ด้วยรูปที่ถูก crop แล้ว
                        Result.ImageData = cropDialog.CroppedImageData;
                        Result.ImagePath = file.Path; // เก็บ path สำหรับแสดงผล

                        // อัปเดต UI
                        await UpdateImagePreview(cropDialog.CroppedImageData);

                        // ✅ เปลี่ยนข้อความปุ่มพร้อม null check
                        if (sender is Button button)
                        {
                            button.Content = "เลือกรูปภาพแล้ว - คลิกเพื่อเปลี่ยน";
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Court image cropped successfully: {cropDialog.CroppedImageData.Length} bytes");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ User cancelled image cropping");
                }
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
        
        // ✅ ตรวจสอบ Result ก่อนใช้งาน
        if (Result == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ Result is null");
            return;
        }
        
        // ✅ Validate input
        if (Result.Status != "0" && Result.Status != "1")
        {
            System.Diagnostics.Debug.WriteLine("❌ Status ไม่ถูกต้อง");
            return;
        }

        // ✅ บันทึก last_updated = วันที่จาก DatePicker + เวลาปัจจุบัน
        // (1 ฟิลด์เดียวแต่มี 2 ความหมาย)
        var selectedDate = Result.LastUpdatedForDatePicker.Date;  // วันที่ที่ผู้ใช้เลือก (วันที่ปรับปรุงสนาม)
        var currentTime = DateTime.Now.TimeOfDay;                  // เวลาปัจจุบัน (เวลาที่แก้ไขข้อมูล)
        var newDateTime = selectedDate.Add(currentTime);           // รวมกัน
        
        Result.LastUpdated = newDateTime;

        System.Diagnostics.Debug.WriteLine($"✅ กำลังบันทึก:");
        System.Diagnostics.Debug.WriteLine($"   CourtID: {Result.CourtID}");
        System.Diagnostics.Debug.WriteLine($"   Status: {Result.Status}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdated (รวม): {Result.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        System.Diagnostics.Debug.WriteLine($"   - วันที่ (ปรับปรุงสนาม): {selectedDate:yyyy-MM-dd}");
        System.Diagnostics.Debug.WriteLine($"   - เวลา (แก้ไขข้อมูล): {currentTime}");
        
        // ✅ กำหนดว่ากดปุ่ม Save แล้ว
        WasSaved = true;

        // ✅ ปิด dialog
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
}
