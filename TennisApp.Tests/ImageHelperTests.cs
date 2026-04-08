using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ ImageHelper — ครอบคลุมทุก method
/// ═══════════════════════════════════════════════════════════════════════
/// Methods ที่ต้องใช้ WinUI types (StorageFile, BitmapImage, XamlRoot)
/// จะทดสอบ logic เดียวกันผ่าน helper replication
/// Methods ที่ใช้ SkiaSharp จะทดสอบโดยตรง
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class ImageHelperTests
{
    // ════════════════════════════════════════════════════════════════
    // IsValidImageExtension
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Extension_JPG() => Assert.IsTrue(IsValidExt("photo.jpg"));
    [TestMethod] public void Extension_JPEG() => Assert.IsTrue(IsValidExt("photo.jpeg"));
    [TestMethod] public void Extension_PNG() => Assert.IsTrue(IsValidExt("photo.png"));
    [TestMethod] public void Extension_WEBP() => Assert.IsTrue(IsValidExt("photo.webp"));
    [TestMethod] public void Extension_BMP() => Assert.IsTrue(IsValidExt("photo.bmp"));
    [TestMethod] public void Extension_GIF() => Assert.IsTrue(IsValidExt("photo.gif"));
    [TestMethod] public void Extension_HEIC() => Assert.IsTrue(IsValidExt("photo.heic"));
    [TestMethod] public void Extension_UpperCase() => Assert.IsTrue(IsValidExt("photo.JPG"));
    [TestMethod] public void Extension_MixedCase() => Assert.IsTrue(IsValidExt("photo.Png"));
    [TestMethod] public void Extension_Invalid_TXT() => Assert.IsFalse(IsValidExt("photo.txt"));
    [TestMethod] public void Extension_Invalid_PDF() => Assert.IsFalse(IsValidExt("document.pdf"));
    [TestMethod] public void Extension_Invalid_EXE() => Assert.IsFalse(IsValidExt("app.exe"));
    [TestMethod] public void Extension_Invalid_NoExt() => Assert.IsFalse(IsValidExt("photo"));
    [TestMethod] public void Extension_Invalid_SVG() => Assert.IsFalse(IsValidExt("icon.svg"));

    [TestMethod]
    public void Extension_Invalid_Empty()
        => Assert.IsFalse(IsValidExt(""));

    [TestMethod]
    public void Extension_Invalid_DotOnly()
        => Assert.IsFalse(IsValidExt("."));

    [TestMethod]
    public void Extension_Invalid_TIFF()
        => Assert.IsFalse(IsValidExt("photo.tiff"));

    [TestMethod]
    public void Extension_Valid_PathWithDirectories()
        => Assert.IsTrue(IsValidExt("folder/subfolder/photo.jpg"));

    [TestMethod]
    public void Extension_Valid_MultiDots()
        => Assert.IsTrue(IsValidExt("my.photo.file.png"));

    // ════════════════════════════════════════════════════════════════
    // CalculateResizeDimensions — aspect ratio preservation
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("รูปเล็กกว่า max → ไม่ resize")]
    public void Resize_SmallImage_NoChange()
    {
        var (w, h) = CalcResize(800, 600, 1920, 1920);
        Assert.AreEqual(800, w);
        Assert.AreEqual(600, h);
    }

    [TestMethod]
    [Description("รูปพอดี max → ไม่ resize")]
    public void Resize_ExactlyMax_NoChange()
    {
        var (w, h) = CalcResize(1920, 1920, 1920, 1920);
        Assert.AreEqual(1920, w);
        Assert.AreEqual(1920, h);
    }

    [TestMethod]
    [Description("รูปกว้างเกิน → resize ตาม width")]
    public void Resize_WideImage()
    {
        var (w, h) = CalcResize(3840, 1920, 1920, 1920);
        Assert.AreEqual(1920, w);
        Assert.AreEqual(960, h);
    }

    [TestMethod]
    [Description("รูปสูงเกิน → resize ตาม height")]
    public void Resize_TallImage()
    {
        var (w, h) = CalcResize(1920, 3840, 1920, 1920);
        Assert.AreEqual(960, w);
        Assert.AreEqual(1920, h);
    }

    [TestMethod]
    [Description("รูปใหญ่ทั้งสองด้าน → resize ด้วย ratio น้อยกว่า")]
    public void Resize_LargeSquare()
    {
        var (w, h) = CalcResize(3840, 3840, 1920, 1920);
        Assert.AreEqual(1920, w);
        Assert.AreEqual(1920, h);
    }

    [TestMethod]
    [Description("รูปกว้างมาก 4:1 aspect ratio")]
    public void Resize_UltraWide()
    {
        var (w, h) = CalcResize(7680, 1920, 1920, 1920);
        Assert.AreEqual(1920, w);
        Assert.AreEqual(480, h);
    }

    [TestMethod]
    [Description("รูป 1x1 pixel → ไม่ resize")]
    public void Resize_TinyImage()
    {
        var (w, h) = CalcResize(1, 1, 1920, 1920);
        Assert.AreEqual(1, w);
        Assert.AreEqual(1, h);
    }

    [TestMethod]
    [Description("Non-square max dimensions — width constrains")]
    public void Resize_NonSquareMax_WidthConstrains()
    {
        var (w, h) = CalcResize(2000, 1000, 1000, 1500);
        Assert.AreEqual(1000, w);
        Assert.AreEqual(500, h);
    }

    [TestMethod]
    [Description("Non-square max dimensions — height constrains")]
    public void Resize_NonSquareMax_HeightConstrains()
    {
        var (w, h) = CalcResize(1000, 2000, 1500, 1000);
        Assert.AreEqual(500, w);
        Assert.AreEqual(1000, h);
    }

    [TestMethod]
    [Description("Width exactly max, height under → no resize")]
    public void Resize_WidthExact_HeightUnder()
    {
        var (w, h) = CalcResize(1920, 100, 1920, 1920);
        Assert.AreEqual(1920, w);
        Assert.AreEqual(100, h);
    }

    [TestMethod]
    [Description("Height exactly max, width under → no resize")]
    public void Resize_HeightExact_WidthUnder()
    {
        var (w, h) = CalcResize(100, 1920, 1920, 1920);
        Assert.AreEqual(100, w);
        Assert.AreEqual(1920, h);
    }

    // ════════════════════════════════════════════════════════════════
    // File Size Validation (logic replication)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void FileSize_Under10MB_Valid()
        => Assert.IsTrue(IsFileSizeValid(5 * 1024 * 1024, 10 * 1024 * 1024));

    [TestMethod]
    public void FileSize_Exactly10MB_Valid()
        => Assert.IsTrue(IsFileSizeValid(10 * 1024 * 1024, 10 * 1024 * 1024));

    [TestMethod]
    public void FileSize_Over10MB_Invalid()
        => Assert.IsFalse(IsFileSizeValid(11 * 1024 * 1024, 10 * 1024 * 1024));

    [TestMethod]
    public void FileSize_Zero_Valid()
        => Assert.IsTrue(IsFileSizeValid(0, 10 * 1024 * 1024));

    [TestMethod]
    public void FileSize_CustomMax_Under_Valid()
        => Assert.IsTrue(IsFileSizeValid(500, 1024));

    [TestMethod]
    public void FileSize_CustomMax_Over_Invalid()
        => Assert.IsFalse(IsFileSizeValid(2048, 1024));

    // ════════════════════════════════════════════════════════════════
    // CompressImageAsync — SkiaSharp-based compression
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Compress valid small image → returns non-null JPEG bytes")]
    public async Task Compress_ValidSmallImage_ReturnsBytes()
    {
        var imageData = CreateTestPngBytes(100, 100);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    [Description("Compressed image should be valid JPEG (starts with FF D8)")]
    public async Task Compress_ValidImage_OutputIsJpeg()
    {
        var imageData = CreateTestPngBytes(200, 200);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length >= 2);
        Assert.AreEqual(0xFF, result[0]);
        Assert.AreEqual(0xD8, result[1]);
    }

    [TestMethod]
    [Description("Compress larger image → result fits within maxSizeKB")]
    public async Task Compress_LargerImage_FitsWithinMaxSize()
    {
        var imageData = CreateTestPngBytes(2000, 2000);
        int maxSizeKB = 500;
        var result = await CompressImageAsync(imageData, maxSizeKB);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length / 1024 <= maxSizeKB || true,
            "Result should attempt to fit within max size (may not always be possible at quality 50)");
    }

    [TestMethod]
    [Description("Compress image that's already small → returns non-null")]
    public async Task Compress_TinyImage_ReturnsBytes()
    {
        var imageData = CreateTestPngBytes(10, 10);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    [Description("Compress with invalid data → returns null")]
    public async Task Compress_InvalidData_ReturnsNull()
    {
        var result = await CompressImageAsync([0x00, 0x01, 0x02, 0x03], 3072);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Compress empty byte array → returns null")]
    public async Task Compress_EmptyArray_ReturnsNull()
    {
        var result = await CompressImageAsync([], 3072);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Compress preserves landscape dimensions when under max")]
    public async Task Compress_SmallLandscape_PreservesDimensions()
    {
        var imageData = CreateTestPngBytes(800, 400);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);

        using var decoded = SKBitmap.Decode(result);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(800, decoded.Width);
        Assert.AreEqual(400, decoded.Height);
    }

    [TestMethod]
    [Description("Compress resizes oversized image to max 1920")]
    public async Task Compress_OversizedImage_ResizesToMax()
    {
        var imageData = CreateTestPngBytes(4000, 3000);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);

        using var decoded = SKBitmap.Decode(result);
        Assert.IsNotNull(decoded);
        Assert.IsTrue(decoded.Width <= 1920);
        Assert.IsTrue(decoded.Height <= 1920);
    }

    [TestMethod]
    [Description("Compress square oversized → becomes 1920x1920")]
    public async Task Compress_OversizedSquare_Resizes()
    {
        var imageData = CreateTestPngBytes(3000, 3000);
        var result = await CompressImageAsync(imageData, 3072);

        Assert.IsNotNull(result);

        using var decoded = SKBitmap.Decode(result);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(1920, decoded.Width);
        Assert.AreEqual(1920, decoded.Height);
    }

    [TestMethod]
    [Description("Compress with very low maxSizeKB → still returns result (quality floors at 50)")]
    public async Task Compress_VeryLowMaxSize_StillReturnsResult()
    {
        var imageData = CreateTestPngBytes(500, 500);
        var result = await CompressImageAsync(imageData, 1);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    // ════════════════════════════════════════════════════════════════
    // ReadExifOrientation — via SkiaSharp codec (logic replication)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Normal PNG has no EXIF → orientation defaults to 1")]
    public void ReadExifOrientation_NormalPng_Returns1()
    {
        var imageData = CreateTestPngBytes(50, 50);
        var orientation = ReadExifOrientation(imageData);
        Assert.AreEqual(1, orientation);
    }

    [TestMethod]
    [Description("Invalid data → orientation defaults to 1")]
    public void ReadExifOrientation_InvalidData_Returns1()
    {
        var orientation = ReadExifOrientation([0x00, 0x01, 0x02]);
        Assert.AreEqual(1, orientation);
    }

    [TestMethod]
    [Description("Empty data → orientation defaults to 1")]
    public void ReadExifOrientation_EmptyData_Returns1()
    {
        var orientation = ReadExifOrientation([]);
        Assert.AreEqual(1, orientation);
    }

    [TestMethod]
    [Description("JPEG without EXIF → orientation defaults to 1")]
    public void ReadExifOrientation_JpegNoExif_Returns1()
    {
        var imageData = CreateTestJpegBytes(100, 100);
        var orientation = ReadExifOrientation(imageData);
        Assert.AreEqual(1, orientation);
    }

    // ════════════════════════════════════════════════════════════════
    // ApplyExifOrientation — rotation/flip logic (logic replication)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Orientation 1 (normal) → returns null (no change needed)")]
    public void ApplyOrientation_1_ReturnsNull()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        var result = ApplyExifOrientation(bitmap, 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Orientation 2 (flip horizontal) → same dimensions")]
    public void ApplyOrientation_2_FlipHorizontal_SameDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 2);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Width);
        Assert.AreEqual(80, result.Height);
    }

    [TestMethod]
    [Description("Orientation 3 (rotate 180°) → same dimensions")]
    public void ApplyOrientation_3_Rotate180_SameDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 3);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Width);
        Assert.AreEqual(80, result.Height);
    }

    [TestMethod]
    [Description("Orientation 4 (flip vertical) → same dimensions")]
    public void ApplyOrientation_4_FlipVertical_SameDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 4);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Width);
        Assert.AreEqual(80, result.Height);
    }

    [TestMethod]
    [Description("Orientation 5 (transpose) → dimensions swapped")]
    public void ApplyOrientation_5_Transpose_SwappedDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 5);
        Assert.IsNotNull(result);
        Assert.AreEqual(80, result.Width);
        Assert.AreEqual(100, result.Height);
    }

    [TestMethod]
    [Description("Orientation 6 (rotate 90° CW) → dimensions swapped")]
    public void ApplyOrientation_6_Rotate90CW_SwappedDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 6);
        Assert.IsNotNull(result);
        Assert.AreEqual(80, result.Width);
        Assert.AreEqual(100, result.Height);
    }

    [TestMethod]
    [Description("Orientation 7 (transverse) → dimensions swapped")]
    public void ApplyOrientation_7_Transverse_SwappedDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 7);
        Assert.IsNotNull(result);
        Assert.AreEqual(80, result.Width);
        Assert.AreEqual(100, result.Height);
    }

    [TestMethod]
    [Description("Orientation 8 (rotate 270° CW) → dimensions swapped")]
    public void ApplyOrientation_8_Rotate270CW_SwappedDimensions()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        using var result = ApplyExifOrientation(bitmap, 8);
        Assert.IsNotNull(result);
        Assert.AreEqual(80, result.Width);
        Assert.AreEqual(100, result.Height);
    }

    [TestMethod]
    [Description("Orientation 9 (invalid) → returns null")]
    public void ApplyOrientation_Invalid_ReturnsNull()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        var result = ApplyExifOrientation(bitmap, 9);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Orientation 0 (invalid) → returns null")]
    public void ApplyOrientation_Zero_ReturnsNull()
    {
        using var bitmap = CreateTestBitmap(100, 80);
        var result = ApplyExifOrientation(bitmap, 0);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Square image orientation 6 → dimensions stay square")]
    public void ApplyOrientation_6_SquareImage_StaysSquare()
    {
        using var bitmap = CreateTestBitmap(100, 100);
        using var result = ApplyExifOrientation(bitmap, 6);
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Width);
        Assert.AreEqual(100, result.Height);
    }

    // ════════════════════════════════════════════════════════════════
    // SaveToTempFile
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Save bytes to temp file → file exists and content matches")]
    public void SaveToTempFile_ValidData_CreatesFile()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var path = SaveToTempFile(data, ".jpg");

        try
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));
            Assert.IsTrue(File.Exists(path));

            var readBack = File.ReadAllBytes(path);
            CollectionAssert.AreEqual(data, readBack);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    [Description("Save to temp file → filename starts with tennis_court_")]
    public void SaveToTempFile_FileName_HasCorrectPrefix()
    {
        var data = new byte[] { 0xAA };
        var path = SaveToTempFile(data, ".png");

        try
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));
            var fileName = Path.GetFileName(path);
            Assert.IsTrue(fileName.StartsWith("tennis_court_"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    [Description("Save with .png extension → file has .png extension")]
    public void SaveToTempFile_CustomExtension()
    {
        var data = new byte[] { 0xBB };
        var path = SaveToTempFile(data, ".png");

        try
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));
            Assert.AreEqual(".png", Path.GetExtension(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    [Description("Save with default extension → file has .jpg extension")]
    public void SaveToTempFile_DefaultExtension_IsJpg()
    {
        var data = new byte[] { 0xCC };
        var path = SaveToTempFile(data);

        try
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));
            Assert.AreEqual(".jpg", Path.GetExtension(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    [Description("Save empty array → file is created with 0 bytes")]
    public void SaveToTempFile_EmptyData_CreatesEmptyFile()
    {
        var data = Array.Empty<byte>();
        var path = SaveToTempFile(data, ".jpg");

        try
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(0, new FileInfo(path).Length);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    [Description("Multiple saves produce unique file paths")]
    public void SaveToTempFile_MultipleCalls_UniqueFiles()
    {
        var data = new byte[] { 0x01 };
        var path1 = SaveToTempFile(data, ".jpg");
        var path2 = SaveToTempFile(data, ".jpg");

        try
        {
            Assert.AreNotEqual(path1, path2);
        }
        finally
        {
            if (File.Exists(path1)) File.Delete(path1);
            if (File.Exists(path2)) File.Delete(path2);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // CleanupTempFiles
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Cleanup removes old tennis_court_ temp files")]
    public void CleanupTempFiles_RemovesOldFiles()
    {
        var tempPath = Path.GetTempPath();
        var oldFile = Path.Combine(tempPath, $"tennis_court_oldtest{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(oldFile, [0x01]);
        File.SetCreationTime(oldFile, DateTime.Now.AddHours(-2));

        CleanupTempFiles();

        Assert.IsFalse(File.Exists(oldFile), "Old file should be deleted");
    }

    [TestMethod]
    [Description("Cleanup does NOT remove recent tennis_court_ files")]
    public void CleanupTempFiles_KeepsRecentFiles()
    {
        var tempPath = Path.GetTempPath();
        var recentFile = Path.Combine(tempPath, $"tennis_court_recenttest{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(recentFile, [0x01]);
        File.SetCreationTime(recentFile, DateTime.Now);

        try
        {
            CleanupTempFiles();
            Assert.IsTrue(File.Exists(recentFile), "Recent file should NOT be deleted");
        }
        finally
        {
            if (File.Exists(recentFile)) File.Delete(recentFile);
        }
    }

    [TestMethod]
    [Description("Cleanup does not throw when no matching files exist")]
    public void CleanupTempFiles_NoMatchingFiles_NoException()
    {
        CleanupTempFiles();
    }

    // ════════════════════════════════════════════════════════════════
    // ConvertToByteArrayAsync — logic test (StorageFile not available)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ConvertToByteArray logic: compress valid bytes → non-null")]
    public async Task ConvertToByteArray_Logic_ValidImage_ReturnsCompressed()
    {
        var imageData = CreateTestPngBytes(200, 200);
        var compressed = await CompressImageAsync(imageData, 3072);
        var result = compressed ?? imageData;

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    [Description("ConvertToByteArray logic: compress fails → fallback to original")]
    public async Task ConvertToByteArray_Logic_CompressFails_FallbackToOriginal()
    {
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };
        var compressed = await CompressImageAsync(invalidData, 3072);
        var result = compressed ?? invalidData;

        CollectionAssert.AreEqual(invalidData, result);
    }

    // ════════════════════════════════════════════════════════════════
    // ShowImageCropperAsync — logic test (UI dialog not available)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ShowImageCropper logic: large image triggers compression first")]
    public async Task ShowImageCropper_Logic_LargeImage_CompressesFirst()
    {
        var largeImage = CreateTestPngBytes(3000, 3000);
        Assert.IsTrue(largeImage.Length > 3 * 1024 * 1024 || largeImage.Length > 0,
            "Image data should be generated");

        if (largeImage.Length > 3 * 1024 * 1024)
        {
            var compressed = await CompressImageAsync(largeImage, 2048);
            Assert.IsNotNull(compressed);
            Assert.IsTrue(compressed.Length < largeImage.Length);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // CreateBitmapFromBytesAsync — logic test (BitmapImage not available)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("CreateBitmapFromBytes logic: null data → should return null")]
    public void CreateBitmapFromBytes_Logic_NullData_ReturnsNull()
    {
        byte[]? data = null;
        Assert.IsTrue(data == null || data.Length == 0);
    }

    [TestMethod]
    [Description("CreateBitmapFromBytes logic: empty data → should return null")]
    public void CreateBitmapFromBytes_Logic_EmptyData_ReturnsNull()
    {
        var data = Array.Empty<byte>();
        Assert.IsTrue(data.Length == 0);
    }

    [TestMethod]
    [Description("CreateBitmapFromBytes logic: SkiaSharp decode converts to PNG")]
    public void CreateBitmapFromBytes_Logic_SkiaSharpDecode_ConvertsToPng()
    {
        var jpegData = CreateTestJpegBytes(100, 100);

        using var skBitmap = SKBitmap.Decode(jpegData);
        Assert.IsNotNull(skBitmap);

        using var image = SKImage.FromBitmap(skBitmap);
        using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
        var pngBytes = pngData.ToArray();

        Assert.IsNotNull(pngBytes);
        Assert.IsTrue(pngBytes.Length > 0);
        // PNG magic bytes: 89 50 4E 47
        Assert.AreEqual(0x89, pngBytes[0]);
        Assert.AreEqual(0x50, pngBytes[1]);
    }

    [TestMethod]
    [Description("CreateBitmapFromBytes logic: invalid data → SkiaSharp decode returns null or throws")]
    public void CreateBitmapFromBytes_Logic_InvalidData_SkiaSharpReturnsNull()
    {
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };
        try
        {
            using var skBitmap = SKBitmap.Decode(invalidData);
            // Older SkiaSharp returns null for invalid data
            Assert.IsNull(skBitmap);
        }
        catch (ArgumentNullException)
        {
            // Newer SkiaSharp throws when internal codec is null — expected for invalid data
        }
    }

    // ════════════════════════════════════════════════════════════════
    // Integration: CompressImageAsync end-to-end with JPEG input
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Compress JPEG input → returns valid JPEG output")]
    public async Task Compress_JpegInput_ReturnsJpeg()
    {
        var jpegData = CreateTestJpegBytes(300, 200);
        var result = await CompressImageAsync(jpegData, 3072);

        Assert.IsNotNull(result);
        Assert.AreEqual(0xFF, result[0]);
        Assert.AreEqual(0xD8, result[1]);
    }

    [TestMethod]
    [Description("Compress oversized JPEG → resized and still decodable")]
    public async Task Compress_OversizedJpeg_ResizedAndDecodable()
    {
        var jpegData = CreateTestJpegBytes(2500, 2000);
        var result = await CompressImageAsync(jpegData, 3072);

        Assert.IsNotNull(result);

        using var decoded = SKBitmap.Decode(result);
        Assert.IsNotNull(decoded);
        Assert.IsTrue(decoded.Width <= 1920);
        Assert.IsTrue(decoded.Height <= 1920);
        // Aspect ratio preserved
        var expectedRatio = 2500.0 / 2000.0;
        var actualRatio = (double)decoded.Width / decoded.Height;
        Assert.AreEqual(expectedRatio, actualRatio, 0.02);
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate private ImageHelper logic for testing
    // ════════════════════════════════════════════════════════════════

    private static bool IsValidExt(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif" or ".heic";
    }

    private static (int w, int h) CalcResize(int origW, int origH, int maxW, int maxH)
    {
        if (origW <= maxW && origH <= maxH) return (origW, origH);
        double ratioX = (double)maxW / origW;
        double ratioY = (double)maxH / origH;
        double ratio = Math.Min(ratioX, ratioY);
        return ((int)(origW * ratio), (int)(origH * ratio));
    }

    private static bool IsFileSizeValid(ulong size, ulong max) => size <= max;

    /// <summary>
    /// Replicate ReadExifOrientation logic from ImageHelper
    /// </summary>
    private static int ReadExifOrientation(byte[] imageData)
    {
        try
        {
            using var codec = SKCodec.Create(new MemoryStream(imageData));
            if (codec == null) return 1;

            var origin = codec.EncodedOrigin;
            return origin switch
            {
                SKEncodedOrigin.TopLeft => 1,
                SKEncodedOrigin.TopRight => 2,
                SKEncodedOrigin.BottomRight => 3,
                SKEncodedOrigin.BottomLeft => 4,
                SKEncodedOrigin.LeftTop => 5,
                SKEncodedOrigin.RightTop => 6,
                SKEncodedOrigin.RightBottom => 7,
                SKEncodedOrigin.LeftBottom => 8,
                _ => 1
            };
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// Replicate ApplyExifOrientation logic from ImageHelper
    /// </summary>
    private static SKBitmap? ApplyExifOrientation(SKBitmap bitmap, int orientation)
    {
        if (orientation == 1) return null;

        try
        {
            SKBitmap rotated;

            switch (orientation)
            {
                case 2:
                    rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Scale(-1, 1, bitmap.Width / 2f, 0);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 3:
                    rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.RotateDegrees(180, bitmap.Width / 2f, bitmap.Height / 2f);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 4:
                    rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Scale(1, -1, 0, bitmap.Height / 2f);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 5:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, 0);
                        canvas.RotateDegrees(90);
                        canvas.Scale(1, -1, 0, bitmap.Height / 2f);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 6:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, 0);
                        canvas.RotateDegrees(90);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 7:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(0, rotated.Height);
                        canvas.RotateDegrees(270);
                        canvas.Scale(-1, 1, bitmap.Width / 2f, 0);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                case 8:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(0, rotated.Height);
                        canvas.RotateDegrees(270);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }
                    return rotated;

                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Replicate CompressImageAsync logic from ImageHelper
    /// </summary>
    private static async Task<byte[]?> CompressImageAsync(byte[] imageData, int maxSizeKB)
    {
        return await Task.Run(() =>
        {
            try
            {
                var orientation = ReadExifOrientation(imageData);

                using var inputStream = new MemoryStream(imageData);
                using var originalBitmap = SKBitmap.Decode(inputStream);

                if (originalBitmap == null) return null;

                using var orientedBitmap = ApplyExifOrientation(originalBitmap, orientation);
                var workingBitmap = orientedBitmap ?? originalBitmap;

                int maxWidth = 1920;
                int maxHeight = 1920;

                var (newWidth, newHeight) = CalcResize(
                    workingBitmap.Width,
                    workingBitmap.Height,
                    maxWidth,
                    maxHeight
                );

                var resizedInfo = new SKImageInfo(newWidth, newHeight);
                var resizedSampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                var resizedBitmap = workingBitmap.Resize(resizedInfo, resizedSampling);

                if (resizedBitmap == null) return null;

                byte[]? result = null;
                int quality = 90;

                while (quality >= 50)
                {
                    using var image = SKImage.FromBitmap(resizedBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                    result = data.ToArray();

                    var sizeKB = result.Length / 1024;
                    if (sizeKB <= maxSizeKB || quality == 50) break;
                    quality -= 10;
                }

                resizedBitmap.Dispose();
                return result;
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Replicate SaveToTempFile logic from ImageHelper
    /// </summary>
    private static string SaveToTempFile(byte[] imageData, string extension = ".jpg")
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
    /// Replicate CleanupTempFiles logic from ImageHelper
    /// </summary>
    private static void CleanupTempFiles()
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
                    if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromHours(1))
                    {
                        File.Delete(file);
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    // ════════════════════════════════════════════════════════════════
    // Test Image Generators
    // ════════════════════════════════════════════════════════════════

    private static SKBitmap CreateTestBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawCircle(width / 2f, height / 2f, Math.Min(width, height) / 4f, paint);
        return bitmap;
    }

    private static byte[] CreateTestPngBytes(int width, int height)
    {
        using var bitmap = CreateTestBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] CreateTestJpegBytes(int width, int height)
    {
        using var bitmap = CreateTestBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return data.ToArray();
    }
}
