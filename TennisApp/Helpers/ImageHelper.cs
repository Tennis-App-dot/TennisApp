using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using TennisApp.Presentation.Dialogs;
using SkiaSharp;

namespace TennisApp.Helpers;

/// <summary>
/// Helper class สำหรับจัดการรูปภาพ
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// แปลงไฟล์รูปภาพเป็น byte array พร้อม validation และ compression
    /// </summary>
    public static async Task<byte[]?> ConvertToByteArrayAsync(StorageFile file, int maxSizeKB = 3072)
    {
        try
        {
            // ตรวจสอบขนาดไฟล์ก่อน
            var properties = await file.GetBasicPropertiesAsync();
            System.Diagnostics.Debug.WriteLine($"📷 Original file size: {properties.Size / 1024}KB");

            var buffer = await FileIO.ReadBufferAsync(file);
            var bytes = new byte[buffer.Length];
            
            using (var dataReader = DataReader.FromBuffer(buffer))
            {
                dataReader.ReadBytes(bytes);
            }

            // ลองแปลงและ compress ด้วย SkiaSharp
            var compressedBytes = await CompressImageAsync(bytes, maxSizeKB);
            return compressedBytes ?? bytes; // fallback to original if compression fails
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ConvertToByteArrayAsync error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Compress รูปภาพให้มีขนาดไม่เกินที่กำหนด
    /// </summary>
    public static async Task<byte[]?> CompressImageAsync(byte[] imageData, int maxSizeKB = 3072)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var inputStream = new MemoryStream(imageData);
                using var originalBitmap = SKBitmap.Decode(inputStream);
                
                if (originalBitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Cannot decode image with SkiaSharp");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"📐 Original dimensions: {originalBitmap.Width}x{originalBitmap.Height}");

                // กำหนด max dimensions
                int maxWidth = 1920;
                int maxHeight = 1920;

                // คำนวณขนาดใหม่
                var (newWidth, newHeight) = CalculateResizeDimensions(
                    originalBitmap.Width, 
                    originalBitmap.Height, 
                    maxWidth, 
                    maxHeight
                );

                System.Diagnostics.Debug.WriteLine($"📐 Resizing to: {newWidth}x{newHeight}");

                // Resize
                var resizedInfo = new SKImageInfo(newWidth, newHeight);
                var resizedSampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                var resizedBitmap = originalBitmap.Resize(resizedInfo, resizedSampling);

                if (resizedBitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Resize failed");
                    return null;
                }

                // Encode เป็น JPEG ด้วย quality ที่ปรับได้
                byte[]? result = null;
                int quality = 90;

                while (quality >= 50)
                {
                    using var image = SKImage.FromBitmap(resizedBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                    result = data.ToArray();

                    var sizeKB = result.Length / 1024;
                    System.Diagnostics.Debug.WriteLine($"🔄 Quality {quality}: {sizeKB}KB");

                    if (sizeKB <= maxSizeKB || quality == 50)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Compressed to {sizeKB}KB (quality: {quality})");
                        break;
                    }

                    quality -= 10;
                }

                resizedBitmap.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ CompressImageAsync error: {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>
    /// คำนวณขนาดใหม่โดยรักษา aspect ratio
    /// </summary>
    private static (int width, int height) CalculateResizeDimensions(
        int originalWidth, 
        int originalHeight, 
        int maxWidth, 
        int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        double ratioX = (double)maxWidth / originalWidth;
        double ratioY = (double)maxHeight / originalHeight;
        double ratio = Math.Min(ratioX, ratioY);

        return (
            (int)(originalWidth * ratio),
            (int)(originalHeight * ratio)
        );
    }

    /// <summary>
    /// เปิด dialog สำหรับ crop รูปภาพเป็นวงกลม
    /// </summary>
    public static async Task<byte[]?> ShowImageCropperAsync(byte[] imageData)
    {
        try
        {
            // ลอง compress ก่อนถ้ารูปใหญ่เกินไป
            if (imageData.Length > 3 * 1024 * 1024)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Image too large ({imageData.Length / 1024}KB), compressing...");
                var compressed = await CompressImageAsync(imageData, 2048);
                if (compressed != null)
                {
                    imageData = compressed;
                }
            }

            var dialog = new ImageCropperDialog(imageData)
            {
                XamlRoot = App.MainWindow?.Content?.XamlRoot!
            };

            var result = await dialog.ShowAsync();
            
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return dialog.CroppedImageData;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error showing image cropper: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// สร้าง BitmapImage จาก byte array พร้อม error handling
    /// </summary>
    public static async Task<BitmapImage?> CreateBitmapFromBytesAsync(byte[] imageData)
    {
        try
        {
            if (imageData == null || imageData.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ ImageHelper: imageData is null or empty");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"📷 ImageHelper: Creating bitmap from {imageData.Length / 1024}KB");

            // ลองใช้ SkiaSharp decode ก่อน (รองรับ format มากกว่า)
            try
            {
                using var skBitmap = SKBitmap.Decode(imageData);
                if (skBitmap != null)
                {
                    // แปลงเป็น PNG เพื่อความเข้ากันได้
                    using var image = SKImage.FromBitmap(skBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    imageData = data.ToArray();
                    System.Diagnostics.Debug.WriteLine($"✅ Converted via SkiaSharp to PNG");
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ SkiaSharp decode failed, using original data");
            }

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageData.AsBuffer());
            stream.Seek(0);
            
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            
            System.Diagnostics.Debug.WriteLine($"✅ ImageHelper: Bitmap created successfully");
            
            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ImageHelper: Error creating bitmap: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// บันทึก byte array เป็นไฟล์ temp
    /// </summary>
    public static string SaveToTempFile(byte[] imageData, string extension = ".jpg")
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var fileName = $"tennis_court_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(tempPath, fileName);

            File.WriteAllBytes(fullPath, imageData);
            
            return fullPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// ตรวจสอบขนาดไฟล์ (ไม่เกิน 10MB — จะ compress ลงเหลือ 3MB ภายหลัง)
    /// </summary>
    public static async Task<bool> IsFileSizeValidAsync(StorageFile file, ulong maxSizeBytes = 10 * 1024 * 1024)
    {
        var properties = await file.GetBasicPropertiesAsync();
        return properties.Size <= maxSizeBytes;
    }

    /// <summary>
    /// ตรวจสอบนามสกุลไฟล์ (รองรับหลายรูปแบบ)
    /// </summary>
    public static bool IsValidImageExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".jpg" || 
               extension == ".jpeg" || 
               extension == ".png" ||
               extension == ".webp" ||
               extension == ".bmp" ||
               extension == ".gif" ||
               extension == ".heic";
    }

    /// <summary>
    /// ลบไฟล์ temp ที่เก่า
    /// </summary>
    public static void CleanupTempFiles()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var files = Directory.GetFiles(tempPath, "tennis_court_*.*");
            
            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    // ลบไฟล์ที่เก่ากว่า 1 ชั่วโมง
                    if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromHours(1))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // ไม่สามารถลบได้ (อาจถูกใช้งานอยู่)
                }
            }
        }
        catch
        {
            // Cleanup ล้มเหลว - ไม่เป็นปัญหา
        }
    }
}
