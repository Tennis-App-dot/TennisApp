using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using TennisApp.Presentation.Dialogs;
using TennisApp.Helpers;

namespace TennisApp.Services;

/// <summary>
/// บริการเลือกและจัดการรูปภาพแบบ cross-platform (Windows + Android)
/// รวม logic: picker → validate → compress → crop → return byte[]
/// </summary>
public static class ImagePickerService
{
    private const ulong MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB (จะ compress ลงเหลือ 3MB)
    private const int MaxCompressedSizeKB = 3072; // 3MB หลัง compress

    /// <summary>
    /// เลือกรูปภาพ + crop เป็นวงกลม (สำหรับ profile: Trainer, Trainee)
    /// </summary>
    public static async Task<ImagePickResult> PickAndCropCircleAsync(XamlRoot xamlRoot)
    {
        return await PickAndCropAsync(xamlRoot, CropShape.Circle, 400, 400);
    }

    /// <summary>
    /// เลือกรูปภาพ + crop เป็นสี่เหลี่ยม (สำหรับ Court)
    /// </summary>
    public static async Task<ImagePickResult> PickAndCropRectangleAsync(XamlRoot xamlRoot, int width = 534, int height = 300)
    {
        return await PickAndCropAsync(xamlRoot, CropShape.Rectangle, width, height);
    }

    /// <summary>
    /// Core method: เลือกรูปภาพ → validate → compress → crop → return
    /// </summary>
    private static async Task<ImagePickResult> PickAndCropAsync(
        XamlRoot xamlRoot, CropShape cropShape, int cropWidth, int cropHeight)
    {
        try
        {
            // 1. สร้าง File Picker
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            // 2. ตั้งค่า Window Handle (เฉพาะ Windows — Android ข้ามขั้นตอนนี้)
            InitializePickerForPlatform(picker);

            // 3. เลือกไฟล์
            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return ImagePickResult.Cancelled();
            }

            System.Diagnostics.Debug.WriteLine($"📸 Selected file: {file.Name}");

            // 4. Validate ขนาดไฟล์
            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > MaxFileSizeBytes)
            {
                return ImagePickResult.Failed("ไฟล์ใหญ่เกินไป", $"ขนาดไฟล์ต้องไม่เกิน {MaxFileSizeBytes / 1024 / 1024}MB");
            }

            // 5. Validate นามสกุลไฟล์
            if (!ImageHelper.IsValidImageExtension(file.Name))
            {
                return ImagePickResult.Failed("ไฟล์ไม่ถูกต้อง", "กรุณาเลือกไฟล์ JPEG หรือ PNG");
            }

            // 6. อ่านไฟล์ + compress
            var bytes = await ImageHelper.ConvertToByteArrayAsync(file, MaxCompressedSizeKB);
            if (bytes == null || bytes.Length == 0)
            {
                return ImagePickResult.Failed("ไม่สามารถอ่านไฟล์ได้", "กรุณาลองใหม่อีกครั้ง");
            }

            System.Diagnostics.Debug.WriteLine($"📦 Compressed: {bytes.Length / 1024}KB");

            // 7. เปิด Crop Dialog
            var cropDialog = new ImageCropperDialog(bytes, cropShape, cropWidth, cropHeight)
            {
                XamlRoot = xamlRoot
            };

            var cropResult = await cropDialog.ShowAsync();

            if (cropResult == ContentDialogResult.Primary && cropDialog.CroppedImageData != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Cropped: {cropDialog.CroppedImageData.Length / 1024}KB");
                return ImagePickResult.Success(cropDialog.CroppedImageData);
            }

            return ImagePickResult.Cancelled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ImagePickerService error: {ex.Message}");
            return ImagePickResult.Failed("เกิดข้อผิดพลาด", $"ไม่สามารถเลือกรูปภาพได้: {ex.Message}");
        }
    }

    /// <summary>
    /// ตั้งค่า FileOpenPicker ตาม platform
    /// </summary>
    private static void InitializePickerForPlatform(FileOpenPicker picker)
    {
        try
        {
#if !__ANDROID__
            // Windows: ต้องตั้ง Window Handle
            if (App.MainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
#endif
            // Android: Uno Platform จัดการ FileOpenPicker ให้อัตโนมัติ — ไม่ต้องทำอะไร
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ InitializePickerForPlatform: {ex.Message}");
        }
    }

    /// <summary>
    /// สร้าง BitmapImage จาก byte[] สำหรับแสดงผลใน UI
    /// </summary>
    public static async Task<BitmapImage?> CreateBitmapAsync(byte[] imageData)
    {
        return await ImageHelper.CreateBitmapFromBytesAsync(imageData);
    }
}

/// <summary>
/// ผลลัพธ์จากการเลือกรูปภาพ
/// </summary>
public class ImagePickResult
{
    public bool IsSuccess { get; private set; }
    public bool IsCancelled { get; private set; }
    public byte[]? ImageData { get; private set; }
    public string? ErrorTitle { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ImagePickResult Success(byte[] imageData) => new()
    {
        IsSuccess = true,
        ImageData = imageData
    };

    public static ImagePickResult Cancelled() => new()
    {
        IsCancelled = true
    };

    public static ImagePickResult Failed(string title, string message) => new()
    {
        ErrorTitle = title,
        ErrorMessage = message
    };
}
