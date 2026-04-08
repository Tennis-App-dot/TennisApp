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
/// รองรับ: เลือกจากแกลเลอรี่ + ถ่ายรูปจากกล้อง (Android)
/// </summary>
public static class ImagePickerService
{
    private const ulong MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB (จะ compress ลงเหลือ 3MB)
    private const int MaxCompressedSizeKB = 3072; // 3MB หลัง compress

    /// <summary>
    /// แสดง dialog ให้เลือก "ถ่ายรูป" หรือ "เลือกจากแกลเลอรี่" แล้ว crop เป็นวงกลม
    /// </summary>
    public static async Task<ImagePickResult> PickAndCropCircleAsync(XamlRoot xamlRoot)
    {
        var bytes = await AcquireImageBytesAsync(xamlRoot);
        if (bytes == null) return ImagePickResult.Cancelled();
        return await CropImageAsync(xamlRoot, bytes, CropShape.Circle, 400, 400);
    }

    /// <summary>
    /// แสดง dialog ให้เลือก "ถ่ายรูป" หรือ "เลือกจากแกลเลอรี่" แล้ว crop เป็นสี่เหลี่ยม
    /// </summary>
    public static async Task<ImagePickResult> PickAndCropRectangleAsync(XamlRoot xamlRoot, int width = 534, int height = 300)
    {
        var bytes = await AcquireImageBytesAsync(xamlRoot);
        if (bytes == null) return ImagePickResult.Cancelled();
        return await CropImageAsync(xamlRoot, bytes, CropShape.Rectangle, width, height);
    }

    // ====================================================================
    // Source Selection (Camera vs Gallery)
    // ====================================================================

    /// <summary>
    /// แสดง dialog เลือกแหล่งรูปภาพ แล้ว return byte[] หรือ null ถ้ายกเลิก
    /// ✅ ทุก path (กล้อง/แกลเลอรี่) จะผ่าน EXIF orientation fix + compression
    /// </summary>
    private static async Task<byte[]?> AcquireImageBytesAsync(XamlRoot xamlRoot)
    {
        // บน Android → แสดง dialog เลือก กล้อง/แกลเลอรี่
        // บน Windows → ไปแกลเลอรี่โดยตรง (ไม่มีกล้อง)
#if __ANDROID__
        var choice = await ShowImageSourceDialogAsync(xamlRoot);
        byte[]? rawBytes = choice switch
        {
            "camera" => await CaptureFromCameraAsync(),
            "gallery" => await PickFromGalleryAsync(),
            _ => null
        };

        // ✅ Camera path: compress + fix EXIF orientation (gallery path already does this)
        if (rawBytes != null && choice == "camera")
        {
            System.Diagnostics.Debug.WriteLine($"📸 Camera raw: {rawBytes.Length / 1024}KB — compressing + fixing EXIF...");
            var processed = await ImageHelper.CompressImageAsync(rawBytes, MaxCompressedSizeKB);
            if (processed != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Camera processed: {processed.Length / 1024}KB");
                return processed;
            }
        }

        return rawBytes;
#else
        return await PickFromGalleryAsync();
#endif
    }

#if __ANDROID__
    private static async Task<string?> ShowImageSourceDialogAsync(XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = "เลือกรูปภาพ",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    CreateSourceButton("\uE722", "ถ่ายรูปจากกล้อง", "camera"),
                    CreateSourceButton("\uE8B9", "เลือกจากแกลเลอรี่", "gallery")
                }
            },
            CloseButtonText = "ยกเลิก",
            XamlRoot = xamlRoot
        };

        string? result = null;

        // Wire up button clicks
        if (dialog.Content is StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Button btn)
                {
                    btn.Click += (s, e) =>
                    {
                        result = (s as Button)?.Tag as string;
                        dialog.Hide();
                    };
                }
            }
        }

        await dialog.ShowAsync();
        return result;
    }

    private static Button CreateSourceButton(string glyph, string text, string tag)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new FontIcon
                {
                    Glyph = glyph, FontSize = 20,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(UIHelper.ParseColor("#4A148C"))
                },
                new TextBlock
                {
                    Text = text, FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(UIHelper.ParseColor("#333333"))
                }
            }
        };

        return new Button
        {
            Content = content,
            Tag = tag,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(UIHelper.ParseColor("#F5F5F5")),
            CornerRadius = new CornerRadius(10),
            Height = 56,
            Padding = new Thickness(16, 0, 16, 0),
            BorderThickness = new Thickness(0)
        };
    }
#endif

    // ====================================================================
    // Camera Capture (Android only)
    // ====================================================================

#if __ANDROID__
    private static TaskCompletionSource<byte[]?>? _cameraTcs;
    private static string? _cameraPhotoPath;

    private static async Task<byte[]?> CaptureFromCameraAsync()
    {
        try
        {
            var context = Android.App.Application.Context;

            // Check camera permission
            if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.Camera)
                != Android.Content.PM.Permission.Granted)
            {
                // Try to request permission
                var activity = TennisApp.Droid.MainActivity.Current;
                if (activity != null)
                {
                    AndroidX.Core.App.ActivityCompat.RequestPermissions(activity,
                        [Android.Manifest.Permission.Camera], 1001);

                    // Give user time to respond
                    await Task.Delay(2000);

                    if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.Camera)
                        != Android.Content.PM.Permission.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Camera permission denied");
                        return null;
                    }
                }
            }

            // Create temp file for photo
            var photoDir = context.GetExternalFilesDir(Android.OS.Environment.DirectoryPictures);
            if (photoDir == null) return null;

            var photoFile = new Java.IO.File(photoDir, $"camera_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            _cameraPhotoPath = photoFile.AbsolutePath;

            var photoUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                context.PackageName + ".fileprovider",
                photoFile);

            // Launch camera intent
            var intent = new Android.Content.Intent(Android.Provider.MediaStore.ActionImageCapture);
            intent.PutExtra(Android.Provider.MediaStore.ExtraOutput, photoUri);
            intent.AddFlags(Android.Content.ActivityFlags.GrantWriteUriPermission);

            _cameraTcs = new TaskCompletionSource<byte[]?>();

            var activity2 = TennisApp.Droid.MainActivity.Current;
            if (activity2 != null)
            {
                activity2.StartActivityForResult(intent, TennisApp.Droid.MainActivity.CameraRequestCode);
            }
            else
            {
                return null;
            }

            // Wait for result
            var result = await _cameraTcs.Task;
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CaptureFromCamera Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// เรียกจาก MainActivity.OnActivityResult เมื่อกล้องส่งรูปกลับมา
    /// </summary>
    public static void OnCameraResult(bool success)
    {
        try
        {
            if (success && _cameraPhotoPath != null && System.IO.File.Exists(_cameraPhotoPath))
            {
                var bytes = System.IO.File.ReadAllBytes(_cameraPhotoPath);
                System.Diagnostics.Debug.WriteLine($"📸 Camera captured: {bytes.Length / 1024}KB");
                _cameraTcs?.TrySetResult(bytes);

                // Cleanup temp file
                try { System.IO.File.Delete(_cameraPhotoPath); } catch { }
            }
            else
            {
                _cameraTcs?.TrySetResult(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ OnCameraResult Error: {ex.Message}");
            _cameraTcs?.TrySetResult(null);
        }
    }
#endif

    // ====================================================================
    // Gallery Pick
    // ====================================================================

    private static async Task<byte[]?> PickFromGalleryAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            InitializePickerForPlatform(picker);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return null;

            System.Diagnostics.Debug.WriteLine($"📸 Selected file: {file.Name}");

            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > MaxFileSizeBytes)
            {
                System.Diagnostics.Debug.WriteLine($"❌ File too large: {properties.Size}");
                return null;
            }

            if (!ImageHelper.IsValidImageExtension(file.Name)) return null;

            var bytes = await ImageHelper.ConvertToByteArrayAsync(file, MaxCompressedSizeKB);
            return bytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ PickFromGallery Error: {ex.Message}");
            return null;
        }
    }

    // ====================================================================
    // Crop
    // ====================================================================

    private static async Task<ImagePickResult> CropImageAsync(
        XamlRoot xamlRoot, byte[] bytes, CropShape cropShape, int cropWidth, int cropHeight)
    {
        try
        {
            if (bytes.Length == 0)
                return ImagePickResult.Failed("ไม่สามารถอ่านไฟล์ได้", "กรุณาลองใหม่อีกครั้ง");

            System.Diagnostics.Debug.WriteLine($"📦 Image: {bytes.Length / 1024}KB");

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
            System.Diagnostics.Debug.WriteLine($"❌ CropImage Error: {ex.Message}");
            return ImagePickResult.Failed("เกิดข้อผิดพลาด", $"ไม่สามารถตัดรูปภาพได้: {ex.Message}");
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
            if (App.MainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
#endif
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
