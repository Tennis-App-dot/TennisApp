using System;
using System.IO;
using Microsoft.Data.Sqlite;
using TennisApp.Tests.Data;

namespace TennisApp.Tests.Helpers;

/// <summary>
/// Helper สร้าง SQLite database จริงสำหรับ test (ไม่ใช่ in-memory เพราะ DAO เปิด connection ใหม่ทุกครั้ง)
/// ใช้ temp file แทน — ลบหลัง test เสร็จ
/// </summary>
public sealed class TestDatabaseHelper : IDisposable
{
    public string DatabasePath { get; }
    public string ConnectionString { get; }

    public TestPaidCourtReservationDao PaidReservations { get; }
    public TestCourseCourtReservationDao CourseReservations { get; }
    public TestPaidCourtUseLogDao PaidUseLogs { get; }
    public TestCourseCourtUseLogDao CourseUseLogs { get; }
    public TestCourtDao Courts { get; }
    public TestCourseDao Courses { get; }

    public TestDatabaseHelper()
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"tennistest_{Guid.NewGuid():N}.db");
        ConnectionString = $"Data Source={DatabasePath};Pooling=false";

        // Initialize tables in correct FK order
        Courts = new TestCourtDao(ConnectionString);
        Courses = new TestCourseDao(ConnectionString);
        PaidReservations = new TestPaidCourtReservationDao(ConnectionString);
        CourseReservations = new TestCourseCourtReservationDao(ConnectionString);
        PaidUseLogs = new TestPaidCourtUseLogDao(ConnectionString);
        CourseUseLogs = new TestCourseCourtUseLogDao(ConnectionString);
    }

    /// <summary>
    /// ลบข้อมูลทั้งหมด (เก็บโครงสร้างตาราง)
    /// </summary>
    public void ClearAllData()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM PaidCourtUseLog;
            DELETE FROM CourseCourtUseLog;
            DELETE FROM PaidCourtReservation;
            DELETE FROM CourseCourtReservation;
            DELETE FROM Course;
            DELETE FROM Trainer;
            DELETE FROM Court WHERE court_id != '00';
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(DatabasePath))
                File.Delete(DatabasePath);
        }
        catch { /* ignore cleanup failure in tests */ }
    }
}
