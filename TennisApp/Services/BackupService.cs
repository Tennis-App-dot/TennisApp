using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Services;

/// <summary>
/// บริการสำรองข้อมูลและนำเข้าข้อมูล (Export/Import SQLite .db)
/// </summary>
public static class BackupService
{
    private const long MaxImportSizeBytes = 50 * 1024 * 1024; // 50MB

    // SQLite magic bytes: "SQLite format 3\0"
    private static readonly byte[] SqliteMagic = "SQLite format 3\0"u8.ToArray();

    /// <summary>
    /// สำรองข้อมูล: copy tennis.db → Downloads/TennisApp_Backup_{timestamp}.db
    /// คืน path ของไฟล์ backup
    /// </summary>
    public static async Task<string> ExportBackupAsync(string sourceDatabasePath)
    {
        if (!File.Exists(sourceDatabasePath))
            throw new FileNotFoundException("ไม่พบไฟล์ฐานข้อมูล", sourceDatabasePath);

        // ปิด connection pool ก่อน copy
        SqliteConnection.ClearAllPools();
        await Task.Delay(100);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"TennisApp_Backup_{timestamp}.db";

        // หา Downloads folder
        var downloadsPath = GetDownloadsPath();
        if (!Directory.Exists(downloadsPath))
            Directory.CreateDirectory(downloadsPath);

        var destPath = Path.Combine(downloadsPath, fileName);

        await Task.Run(() => File.Copy(sourceDatabasePath, destPath, overwrite: true));

        System.Diagnostics.Debug.WriteLine($"✅ Backup exported: {destPath} ({new FileInfo(destPath).Length / 1024}KB)");
        return destPath;
    }

    /// <summary>
    /// ตรวจสอบว่าไฟล์เป็น SQLite database จริงหรือไม่
    /// </summary>
    public static async Task<(bool IsValid, string? ErrorMessage)> ValidateBackupFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return (false, "ไม่พบไฟล์ที่เลือก");

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > MaxImportSizeBytes)
            return (false, $"ไฟล์ใหญ่เกินไป ({fileInfo.Length / 1024 / 1024}MB) — สูงสุด {MaxImportSizeBytes / 1024 / 1024}MB");

        if (fileInfo.Length < 100)
            return (false, "ไฟล์เล็กเกินไป — ไม่ใช่ฐานข้อมูล SQLite");

        // ตรวจ magic bytes
        var header = new byte[16];
        await using var fs = File.OpenRead(filePath);
        var bytesRead = await fs.ReadAsync(header.AsMemory(0, 16));
        if (bytesRead < 16 || !header.AsSpan(0, 16).SequenceEqual(SqliteMagic))
            return (false, "ไฟล์นี้ไม่ใช่ฐานข้อมูล SQLite");

        // ตรวจว่ามีตารางหลักครบ
        try
        {
            var connStr = $"Data Source={filePath};Mode=ReadOnly;Pooling=false";
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();

            string[] requiredTables = ["Court", "Trainer", "Trainee", "Course"];
            foreach (var table in requiredTables)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
                cmd.Parameters.AddWithValue("@name", table);
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (count == 0)
                    return (false, $"ไฟล์นี้ไม่มีตาราง '{table}' — ไม่ใช่ข้อมูล TennisApp");
            }
        }
        catch (Exception ex)
        {
            return (false, $"ไม่สามารถเปิดไฟล์ได้: {ex.Message}");
        }

        return (true, null);
    }

    /// <summary>
    /// นำเข้าข้อมูล: replace tennis.db ด้วยไฟล์ที่เลือก
    /// ⚠️ จะ auto-backup ก่อนเสมอ
    /// </summary>
    public static async Task<string> ImportBackupAsync(string importFilePath, string targetDatabasePath)
    {
        // 1. Validate
        var (isValid, error) = await ValidateBackupFileAsync(importFilePath);
        if (!isValid)
            throw new InvalidOperationException(error ?? "ไฟล์ไม่ถูกต้อง");

        // 2. Auto-backup ก่อน import
        var autoBackupPath = await ExportBackupAsync(targetDatabasePath);
        System.Diagnostics.Debug.WriteLine($"📦 Auto-backup before import: {autoBackupPath}");

        // 3. ปิด connections
        SqliteConnection.ClearAllPools();
        await Task.Delay(200);

        // 4. Replace database file
        await Task.Run(() => File.Copy(importFilePath, targetDatabasePath, overwrite: true));

        System.Diagnostics.Debug.WriteLine($"✅ Database replaced from: {importFilePath}");
        return autoBackupPath;
    }

    /// <summary>
    /// ขนาดไฟล์ database ปัจจุบัน
    /// </summary>
    public static string GetDatabaseSizeText(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath)) return "—";
            var size = new FileInfo(databasePath).Length;
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024.0:0.1} KB",
                _ => $"{size / 1024.0 / 1024.0:0.1} MB"
            };
        }
        catch
        {
            return "—";
        }
    }

    /// <summary>
    /// Share file via Android Share Sheet
    /// </summary>
    public static void ShareFileOnAndroid(string filePath)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var file = new Java.IO.File(filePath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                context.PackageName + ".fileprovider",
                file);

            var intent = new Android.Content.Intent(Android.Content.Intent.ActionSend);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Android.Content.Intent.ExtraStream, uri);
            intent.PutExtra(Android.Content.Intent.ExtraSubject, "TennisApp Backup");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);

            var chooser = Android.Content.Intent.CreateChooser(intent, "ส่งไฟล์สำรองข้อมูล");
            chooser!.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(chooser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Share failed: {ex.Message}");
        }
#endif
    }

    private static string GetDownloadsPath()
    {
#if ANDROID
        return Android.OS.Environment.GetExternalStoragePublicDirectory(
            Android.OS.Environment.DirectoryDownloads)!.AbsolutePath;
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#endif
    }
}
