using System;
using System.IO;
using TennisApp.Data;

namespace TennisApp.Services;

/// <summary>
/// Service สำหรับจัดการ Database connection และ DAOs
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly string _databasePath;
    private readonly string _connectionString;
    private bool _disposed;
    
    public CourtDao Courts { get; private set; }
    public TraineeDao Trainees { get; private set; }
    public TrainerDao Trainers { get; private set; }
    public CourseDao Courses { get; private set; }
    public ClassRegisRecordDao Registrations { get; private set; }

    public DatabaseService()
    {
        // กำหนด path ของ database file
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "TennisApp");
        
        // สร้างโฟลเดอร์ถ้ายังไม่มี
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _databasePath = Path.Combine(appFolder, "tennis.db");
        _connectionString = $"Data Source={_databasePath}";

        InitializeDAOs();
    }

    private void InitializeDAOs()
    {
        Courts = new CourtDao(_connectionString);
        Trainees = new TraineeDao(_connectionString);
        Trainers = new TrainerDao(_connectionString);
        Courses = new CourseDao(_connectionString);
        Registrations = new ClassRegisRecordDao(_connectionString);
    }

    /// <summary>
    /// รีเซ็ตฐานข้อมูล (สำหรับ testing)
    /// </summary>
    public void ResetDatabase()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
        
        InitializeDAOs();
    }

    /// <summary>
    /// ได้ path ของ database file
    /// </summary>
    public string GetDatabasePath() => _databasePath;

    /// <summary>
    /// ตรวจสอบว่า database พร้อมใช้งานหรือไม่
    /// </summary>
    public bool IsDatabaseReady()
    {
        try
        {
            return File.Exists(_databasePath);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Cleanup if needed (DAOs don't currently implement IDisposable, but we're ready for it)
        _disposed = true;
    }
}
