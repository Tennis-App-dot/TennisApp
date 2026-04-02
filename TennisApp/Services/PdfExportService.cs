using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace TennisApp.Services;

/// <summary>
/// บริการสร้างไฟล์ PDF รายงานสรุปผลการดำเนินงาน ด้วย SkiaSharp
/// </summary>
public sealed class PdfExportService
{
    // ====================================================================
    // Data Records
    // ====================================================================

    public record SummaryData(
        string PeriodLabel,
        int TotalRevenue, int RevenueCount,
        int TotalBookings, int PaidBookings, int CourseBookings,
        int TotalUsage, double AvgDuration,
        int TotalCancelled, double CancelPercent);

    public record ReservationDetail(
        int Total, int Booked, int InUse, int Completed, int Cancelled,
        int Revenue, double AvgDuration, double AvgPrice);

    public record CourtRank(string CourtId, int Count);

    public record SystemInfo(
        int TotalCourts, int ActiveCourts, int Trainers,
        int Trainees, int Courses, int Registrations);

    public record ReportData(
        SummaryData Summary,
        ReservationDetail PaidDetail,
        ReservationDetail CourseDetail,
        List<CourtRank> CourtRanking,
        SystemInfo System);

    // ====================================================================
    // Page Constants
    // ====================================================================

    private const float PageWidth = 595f;  // A4
    private const float PageHeight = 842f;
    private const float MarginLeft = 40f;
    private const float MarginRight = 40f;
    private const float MarginTop = 40f;
    private const float MarginBottom = 40f;
    private const float ContentWidth = PageWidth - MarginLeft - MarginRight;

    // ====================================================================
    // Generate PDF
    // ====================================================================

    public async Task<string> GeneratePdfAsync(ReportData data)
    {
        return await Task.Run(() => GeneratePdf(data));
    }

    private string GeneratePdf(ReportData data)
    {
        var downloadsPath = GetDownloadsPath();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filePath = Path.Combine(downloadsPath, $"TennisReport_{timestamp}.pdf");

        using var fontRegular = LoadFont("NotoSansThai-Regular.ttf");
        using var fontBold = LoadFont("NotoSansThai-Bold.ttf");

        using var stream = new SKFileWStream(filePath);
        using var document = SKDocument.CreatePdf(stream);

        var pages = new List<Action<SKCanvas>>
        {
            canvas => DrawPage1(canvas, data, fontRegular, fontBold),
            canvas => DrawPage2(canvas, data, fontRegular, fontBold)
        };

        foreach (var drawPage in pages)
        {
            using var canvas = document.BeginPage(PageWidth, PageHeight);
            drawPage(canvas);
            document.EndPage();
        }

        document.Close();
        return filePath;
    }

    // ====================================================================
    // Page 1: Header + Summary + Paid + Course
    // ====================================================================

    private void DrawPage1(SKCanvas canvas, ReportData data, SKTypeface fontRegular, SKTypeface fontBold)
    {
        canvas.Clear(SKColors.White);
        float y = MarginTop;

        // Title
        y = DrawTitle(canvas, "สรุปผลการดำเนินงาน", y, fontBold);

        // Period + Date
        using var periodFont = MakeFont(fontRegular, 11);
        using var periodPaint = MakePaint(0xFF666666);
        DrawTextOnCanvas(canvas, $"ช่วงเวลา: {data.Summary.PeriodLabel}  |  วันที่ออกรายงาน: {DateTime.Now:dd/MM/yyyy HH:mm}", MarginLeft, y, periodFont, periodPaint);
        y += 24;

        // Separator
        y = DrawSeparator(canvas, y);

        // ── Summary Section ──
        y = DrawSectionHeader(canvas, "ภาพรวม", y, fontBold, 0xFF4A148C);

        var summaryRows = new List<(string, string)>
        {
            ("รายได้รวม", $"฿{data.Summary.TotalRevenue:N0} (จาก {data.Summary.RevenueCount} รายการ)"),
            ("การจองทั้งหมด", $"{data.Summary.TotalBookings} รายการ (เช่า {data.Summary.PaidBookings} / คอร์ส {data.Summary.CourseBookings})"),
            ("ใช้สนามจริง", $"{data.Summary.TotalUsage} ครั้ง (เฉลี่ย {data.Summary.AvgDuration:0.0} ชม./ครั้ง)"),
            ("ยกเลิก", $"{data.Summary.TotalCancelled} รายการ ({data.Summary.CancelPercent:0.0}%)")
        };
        y = DrawKeyValueRows(canvas, summaryRows, y, fontRegular, fontBold);
        y += 10;

        // ── Paid Detail ──
        y = DrawSeparator(canvas, y);
        y = DrawSectionHeader(canvas, "เช่าสนาม (Paid)", y, fontBold, 0xFF4A148C);
        y = DrawReservationDetail(canvas, data.PaidDetail, true, y, fontRegular, fontBold);
        y += 10;

        // ── Course Detail ──
        y = DrawSeparator(canvas, y);
        y = DrawSectionHeader(canvas, "คอร์สเรียน (Course)", y, fontBold, 0xFF7B1FA2);
        y = DrawReservationDetail(canvas, data.CourseDetail, true, y, fontRegular, fontBold);

        // Footer
        DrawFooter(canvas, 1, 2, fontRegular);
    }

    // ====================================================================
    // Page 2: Court Ranking + System Stats
    // ====================================================================

    private void DrawPage2(SKCanvas canvas, ReportData data, SKTypeface fontRegular, SKTypeface fontBold)
    {
        canvas.Clear(SKColors.White);
        float y = MarginTop;

        // ── Court Ranking ──
        y = DrawSectionHeader(canvas, "สถิติการใช้สนาม", y, fontBold, 0xFF00695C);

        if (data.CourtRanking.Count == 0)
        {
            using var emptyFont = MakeFont(fontRegular, 12);
            using var emptyPaint = MakePaint(0xFF999999);
            DrawTextOnCanvas(canvas, "ยังไม่มีข้อมูลการใช้สนาม", MarginLeft + 20, y, emptyFont, emptyPaint);
            y += 30;
        }
        else
        {
            var maxCount = 1;
            foreach (var r in data.CourtRanking)
                if (r.Count > maxCount) maxCount = r.Count;

            foreach (var item in data.CourtRanking)
            {
                y = DrawCourtBar(canvas, item, maxCount, y, fontRegular, fontBold);
            }
            y += 10;
        }

        // ── System Stats ──
        y = DrawSeparator(canvas, y);
        y = DrawSectionHeader(canvas, "ข้อมูลระบบ", y, fontBold, 0xFF37474F);

        var sysRows = new List<(string, string)>
        {
            ("สนาม", $"{data.System.TotalCourts} (เปิดใช้ {data.System.ActiveCourts})"),
            ("ผู้ฝึกสอน", $"{data.System.Trainers} คน"),
            ("ผู้เรียน", $"{data.System.Trainees} คน"),
            ("คอร์ส", $"{data.System.Courses} คอร์ส"),
            ("สมัครคอร์ส", $"{data.System.Registrations} รายการ")
        };
        y = DrawKeyValueRows(canvas, sysRows, y, fontRegular, fontBold);

        DrawFooter(canvas, 2, 2, fontRegular);
    }

    // ====================================================================
    // Drawing Helpers
    // ====================================================================

    private float DrawTitle(SKCanvas canvas, string text, float y, SKTypeface fontBold)
    {
        using var font = MakeFont(fontBold, 22);
        using var paint = MakePaint(0xFF1A1A2E);
        DrawTextOnCanvas(canvas, text, MarginLeft, y + 22, font, paint);
        return y + 36;
    }

    private float DrawSectionHeader(SKCanvas canvas, string text, float y, SKTypeface fontBold, uint color)
    {
        // Accent bar
        using var barPaint = new SKPaint { Color = new SKColor(color), IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(MarginLeft, y, MarginLeft + 4, y + 18), 2), barPaint);

        using var font = MakeFont(fontBold, 15);
        using var textPaint = MakePaint(color);
        DrawTextOnCanvas(canvas, text, MarginLeft + 12, y + 15, font, textPaint);
        return y + 30;
    }

    private float DrawSeparator(SKCanvas canvas, float y)
    {
        using var paint = new SKPaint { Color = new SKColor(0xFFE0E0E0), StrokeWidth = 1 };
        canvas.DrawLine(MarginLeft, y, PageWidth - MarginRight, y, paint);
        return y + 12;
    }

    private float DrawKeyValueRows(SKCanvas canvas, List<(string Label, string Value)> rows,
        float y, SKTypeface fontRegular, SKTypeface fontBold)
    {
        foreach (var (label, value) in rows)
        {
            using var labelFont = MakeFont(fontRegular, 12);
            using var labelPaint = MakePaint(0xFF555555);
            using var valueFont = MakeFont(fontBold, 12);
            using var valuePaint = MakePaint(0xFF222222);

            DrawTextOnCanvas(canvas, label, MarginLeft + 8, y + 12, labelFont, labelPaint);

            var valueWidth = MeasureTextWidth(value, valueFont);
            DrawTextOnCanvas(canvas, value, PageWidth - MarginRight - valueWidth, y + 12, valueFont, valuePaint);
            y += 22;
        }
        return y;
    }

    private float DrawReservationDetail(SKCanvas canvas, ReservationDetail detail,
        bool showRevenue, float y, SKTypeface fontRegular, SKTypeface fontBold)
    {
        var statusRows = new (string Label, string Value, uint Color)[]
        {
            ("จองทั้งหมด", $"{detail.Total} รายการ", 0xFF222222),
            ("เสร็จสิ้น", $"{detail.Completed}", 0xFF2E7D32),
            ("กำลังใช้งาน", $"{detail.InUse}", 0xFFE65100),
            ("รอใช้งาน", $"{detail.Booked}", 0xFF1565C0),
            ("ยกเลิก", $"{detail.Cancelled}", 0xFFC62828),
        };

        foreach (var (label, value, color) in statusRows)
        {
            using var labelFont = MakeFont(fontRegular, 11);
            using var labelPaint = MakePaint(0xFF666666);
            using var valueFont = MakeFont(fontBold, 11);
            using var valuePaint = MakePaint(color);

            // Status dot
            using var dotPaint = new SKPaint { Color = new SKColor(color), IsAntialias = true };
            canvas.DrawCircle(MarginLeft + 14, y + 8, 3, dotPaint);

            DrawTextOnCanvas(canvas, label, MarginLeft + 24, y + 12, labelFont, labelPaint);
            var vw = MeasureTextWidth(value, valueFont);
            DrawTextOnCanvas(canvas, value, PageWidth - MarginRight - vw, y + 12, valueFont, valuePaint);
            y += 20;
        }

        if (showRevenue)
        {
            y += 4;
            using var linePaint = new SKPaint { Color = new SKColor(0xFFF0F0F0), StrokeWidth = 1 };
            canvas.DrawLine(MarginLeft + 8, y, PageWidth - MarginRight, y, linePaint);
            y += 8;

            var revenueRows = new List<(string, string)>
            {
                ("รายได้รวม", $"฿{detail.Revenue:N0}"),
                ("เวลาใช้เฉลี่ย/ครั้ง", $"{detail.AvgDuration:0.0} ชม."),
                ("รายได้เฉลี่ย/ครั้ง", detail.AvgPrice > 0 ? $"฿{detail.AvgPrice:N0}" : "—"),
            };

            foreach (var (label, value) in revenueRows)
            {
                using var lf = MakeFont(fontRegular, 11);
                using var lp = MakePaint(0xFF666666);
                using var vf = MakeFont(fontBold, 11);
                using var vp = MakePaint(0xFFD32F2F);
                DrawTextOnCanvas(canvas, label, MarginLeft + 8, y + 12, lf, lp);
                var vw = MeasureTextWidth(value, vf);
                DrawTextOnCanvas(canvas, value, PageWidth - MarginRight - vw, y + 12, vf, vp);
                y += 20;
            }
        }

        return y;
    }

    private float DrawCourtBar(SKCanvas canvas, CourtRank item, int maxCount,
        float y, SKTypeface fontRegular, SKTypeface fontBold)
    {
        var barWidth = ContentWidth - 120;
        var fillWidth = maxCount > 0 ? barWidth * item.Count / maxCount : 0;
        var percent = maxCount > 0 ? (double)item.Count / maxCount * 100 : 0;

        // Label
        using var labelFont = MakeFont(fontRegular, 11);
        using var labelPaint = MakePaint(0xFF333333);
        DrawTextOnCanvas(canvas, $"สนาม {item.CourtId}", MarginLeft + 8, y + 12, labelFont, labelPaint);

        // Count
        using var countFont = MakeFont(fontBold, 11);
        using var countPaint = MakePaint(0xFF666666);
        var countText = $"{item.Count} ครั้ง ({percent:0}%)";
        var cw = MeasureTextWidth(countText, countFont);
        DrawTextOnCanvas(canvas, countText, PageWidth - MarginRight - cw, y + 12, countFont, countPaint);
        y += 18;

        // Bar background
        var barLeft = MarginLeft + 8;
        using var bgPaint = new SKPaint { Color = new SKColor(0xFFF5F5F5), IsAntialias = true };
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(barLeft, y, barLeft + barWidth, y + 8), 4), bgPaint);

        // Bar fill
        if (fillWidth > 0)
        {
            using var fillPaint = new SKPaint { Color = new SKColor(0xFF4A148C), IsAntialias = true };
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(barLeft, y, barLeft + (float)fillWidth, y + 8), 4), fillPaint);
        }

        return y + 20;
    }

    private void DrawFooter(SKCanvas canvas, int page, int totalPages, SKTypeface fontRegular)
    {
        var footerY = PageHeight - MarginBottom + 10;
        using var font = MakeFont(fontRegular, 9);
        using var paint = MakePaint(0xFFAAAAAA);

        DrawTextOnCanvas(canvas, "TennisApp — สรุปผลการดำเนินงาน", MarginLeft, footerY, font, paint);

        var pageText = $"หน้า {page}/{totalPages}";
        var pw = MeasureTextWidth(pageText, font);
        DrawTextOnCanvas(canvas, pageText, PageWidth - MarginRight - pw, footerY, font, paint);
    }

    // ====================================================================
    // Text Drawing Helpers (using non-obsolete SKFont API)
    // ====================================================================

    private static void DrawTextOnCanvas(SKCanvas canvas, string text, float x, float y, SKFont font, SKPaint paint)
    {
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static float MeasureTextWidth(string text, SKFont font)
    {
        return font.MeasureText(text);
    }

    // ====================================================================
    // Font & Paint Helpers
    // ====================================================================

    private static SKTypeface LoadFont(string fileName)
    {
        // Try load from app local data (cached from previous successful load)
        var localFontDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TennisApp", "Fonts");
        var localFontPath = Path.Combine(localFontDir, fileName);

        if (File.Exists(localFontPath))
        {
            var tf = SKTypeface.FromFile(localFontPath);
            if (tf != null) return tf;
        }

        // Try common paths
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", fileName),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                var tf = SKTypeface.FromFile(path);
                if (tf != null) return tf;
            }
        }

#if ANDROID
        // Android + Uno Platform: fonts packaged as Content items are in the APK's assets
        // but under different possible paths. Try multiple asset paths.
        try
        {
            var context = Android.App.Application.Context;
            var assetPaths = new[]
            {
                $"Fonts/{fileName}",
                $"Assets/Fonts/{fileName}",
                fileName,
            };

            foreach (var assetPath in assetPaths)
            {
                try
                {
                    using var assetStream = context.Assets?.Open(assetPath);
                    if (assetStream != null)
                    {
                        if (!Directory.Exists(localFontDir))
                            Directory.CreateDirectory(localFontDir);

                        using (var fileStream = File.Create(localFontPath))
                        {
                            assetStream.CopyTo(fileStream);
                        }

                        var tf = SKTypeface.FromFile(localFontPath);
                        if (tf != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Font loaded from Android assets: {assetPath}");
                            return tf;
                        }
                    }
                }
                catch (Java.IO.FileNotFoundException)
                {
                    // Try next path
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Android asset font load error: {ex.Message}");
        }

        // Android: Try reading the font from the native data directory where Uno deploys content
        try
        {
            var context = Android.App.Application.Context;
            var dataDir = context.FilesDir?.AbsolutePath;
            if (dataDir != null)
            {
                var unoPaths = new[]
                {
                    Path.Combine(dataDir, ".__override__", "Assets", "Fonts", fileName),
                    Path.Combine(dataDir, "Assets", "Fonts", fileName),
                };

                foreach (var unoPath in unoPaths)
                {
                    if (File.Exists(unoPath))
                    {
                        var tf = SKTypeface.FromFile(unoPath);
                        if (tf != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Font loaded from Uno data dir: {unoPath}");
                            return tf;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Uno data dir font load error: {ex.Message}");
        }

        // Android: Try loading system Thai font as a better fallback than default
        try
        {
            var systemThaiPaths = new[]
            {
                "/system/fonts/NotoSansThai-Regular.ttf",
                "/system/fonts/NotoSansThai-Bold.ttf",
                "/system/fonts/NotoSansThai-Regular.otf",
            };

            // Match bold/regular based on the requested file
            var isBold = fileName.Contains("Bold", StringComparison.OrdinalIgnoreCase);
            foreach (var sysPath in systemThaiPaths)
            {
                var pathIsBold = sysPath.Contains("Bold", StringComparison.OrdinalIgnoreCase);
                if (isBold == pathIsBold && File.Exists(sysPath))
                {
                    var tf = SKTypeface.FromFile(sysPath);
                    if (tf != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Font loaded from system: {sysPath}");
                        return tf;
                    }
                }
            }

            // Try any system Thai font as fallback
            foreach (var sysPath in systemThaiPaths)
            {
                if (File.Exists(sysPath))
                {
                    var tf = SKTypeface.FromFile(sysPath);
                    if (tf != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Font loaded from system (fallback): {sysPath}");
                        return tf;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ System font load error: {ex.Message}");
        }
#endif

        // Fallback
        System.Diagnostics.Debug.WriteLine($"⚠️ Font not found: {fileName}, using default");
        return SKTypeface.Default;
    }

    private static SKFont MakeFont(SKTypeface typeface, float size)
    {
        return new SKFont(typeface, size);
    }

    private static SKPaint MakePaint(uint color)
    {
        return new SKPaint
        {
            Color = new SKColor(color),
            IsAntialias = true
        };
    }

    private static string GetDownloadsPath()
    {
#if ANDROID
        var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(
            Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
        if (!string.IsNullOrEmpty(downloadsDir))
        {
            if (!Directory.Exists(downloadsDir))
                Directory.CreateDirectory(downloadsDir);
            return downloadsDir;
        }
#endif
        // Fallback: app local folder
        var localPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TennisApp", "Reports");
        if (!Directory.Exists(localPath))
            Directory.CreateDirectory(localPath);
        return localPath;
    }
}
