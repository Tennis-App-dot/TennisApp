using System;
using System.IO;
using TennisApp.Data;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TennisApp.Services;

/// <summary>
/// Service สำหรับจัดการ Database connection และ DAOs
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private readonly string _databasePath;
    private readonly string _connectionString;
    private bool _disposed;
    private bool _initialized;

    public CourtDao Courts { get; private set; } = null!;
    public TraineeDao Trainees { get; private set; } = null!;
    public TrainerDao Trainers { get; private set; } = null!;
    public CourseDao Courses { get; private set; } = null!;
    public ClassRegisRecordDao Registrations { get; private set; } = null!;

    public PaidCourtReservationDao PaidCourtReservations { get; private set; } = null!;
    public CourseCourtReservationDao CourseCourtReservations { get; private set; } = null!;

    public PaidCourtUseLogDao PaidCourtUseLogs { get; private set; } = null!;
    public CourseCourtUseLogDao CourseCourtUseLogs { get; private set; } = null!;

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
    }

    /// <summary>
    /// Lazy initialization — สร้าง DAO ครั้งแรกที่เรียกใช้ (ไม่ block constructor)
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized) return;

        InitializeDAOs();
        _initialized = true;
    }

    private void InitializeDAOs()
    {
        // ✅ สร้าง Court DAO ก่อน (เพื่อให้มีสนาม "00" สำหรับ Foreign Key)
        Courts = new CourtDao(_connectionString);
        
        // สร้าง DAOs อื่น ๆ
        Trainees = new TraineeDao(_connectionString);
        Trainers = new TrainerDao(_connectionString);
        Courses = new CourseDao(_connectionString);
        Registrations = new ClassRegisRecordDao(_connectionString);
        
        // ✅ Initialize Reservation DAOs (ต้องสร้างหลัง Court เพราะมี Foreign Key)
        PaidCourtReservations = new PaidCourtReservationDao(_connectionString);
        CourseCourtReservations = new CourseCourtReservationDao(_connectionString);
        
        // ✅ Initialize Usage Log DAOs (ต้องสร้างหลัง Reservation เพราะมี Foreign Key)
        PaidCourtUseLogs = new PaidCourtUseLogDao(_connectionString);
        CourseCourtUseLogs = new CourseCourtUseLogDao(_connectionString);
        
        System.Diagnostics.Debug.WriteLine("✅ All DAOs initialized in correct order");
    }

    /// <summary>
    /// Async initialization — เรียกจาก background thread ได้
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await Task.Run(() => InitializeDAOs());
        _initialized = true;

        System.Diagnostics.Debug.WriteLine("✅ DatabaseService initialized asynchronously");
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

        _initialized = false;
        EnsureInitialized();
    }

    /// <summary>
    /// ลบข้อมูลทั้งหมดในฐานข้อมูล แต่เก็บโครงสร้างไว้
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        EnsureInitialized();

        System.Diagnostics.Debug.WriteLine("🗑️ เริ่มลบข้อมูลทั้งหมด...");
        
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // ✅ ดึงรายชื่อตารางที่มีอยู่จริงในฐานข้อมูล
            var existingTables = new List<string>();
            var getTablesCommand = connection.CreateCommand();
            getTablesCommand.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' 
                AND name NOT LIKE 'sqlite_%'
                ORDER BY name
            ";
            
            using var reader = await getTablesCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingTables.Add(reader.GetString(0));
            }

            System.Diagnostics.Debug.WriteLine($"📋 ตารางที่พบในฐานข้อมูล: {string.Join(", ", existingTables)}");

            // ลบข้อมูลตามลำดับ Foreign Key (ลบจากตารางลูกก่อน)
            var tablesToClear = new[]
            {
                "CourseCourtUseLog",       // ลูกของ CourseCourtReservation
                "PaidCourtUseLog",         // ลูกของ PaidCourtReservation
                "CourseCourtReservation",  // ลูกของ Court และ Course
                "PaidCourtReservation",    // ลูกของ Court
                "ClassRegisRecord",        // ลูกของ Trainee และ Course
                "Course",                  // ลูกของ Trainer
                "Trainee",
                "Trainer",
                "Court"
            };

            foreach (var table in tablesToClear)
            {
                // ตรวจสอบว่าตารางมีอยู่จริงหรือไม่
                if (!existingTables.Contains(table))
                {
                    System.Diagnostics.Debug.WriteLine($"   ⚠️ {table}: ไม่พบตาราง (ข้าม)");
                    continue;
                }

                var command = connection.CreateCommand();
                command.CommandText = $"DELETE FROM {table}";
                var rowsAffected = await command.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"   ✅ {table}: ลบ {rowsAffected} แถว");
            }

            // ✅ เพิ่มสนาม dummy "00" กลับคืนมา (สำหรับการจองที่ยังไม่ได้จัดสรรสนาม)
            if (existingTables.Contains("Court"))
            {
                var insertDummyCommand = connection.CreateCommand();
                insertDummyCommand.CommandText = @"
                    INSERT INTO Court (court_id, court_img, court_status, last_updated)
                    VALUES ('00', NULL, '0', CURRENT_TIMESTAMP)
                ";
                await insertDummyCommand.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine("   ✅ สร้างสนาม dummy '00' กลับคืนมา");
            }

            // ✅ Run VACUUM to reclaim disk space
            var vacuumCommand = connection.CreateCommand();
            vacuumCommand.CommandText = "VACUUM";
            await vacuumCommand.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("   ✅ Database vacuumed (reclaimed disk space)");

            System.Diagnostics.Debug.WriteLine("✅ ลบข้อมูลทั้งหมดเสร็จสิ้น");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error clearing database: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// ดึงรายชื่อตารางทั้งหมดในฐานข้อมูล (สำหรับ debug)
    /// </summary>
    public async Task<List<string>> GetAllTableNamesAsync()
    {
        var tables = new List<string>();
        
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' 
                AND name NOT LIKE 'sqlite_%'
                ORDER BY name
            ";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting table names: {ex.Message}");
        }

        return tables;
    }

    /// <summary>
    /// นับจำนวนแถวในแต่ละตาราง (สำหรับ debug)
    /// </summary>
    public async Task<Dictionary<string, int>> GetTableRowCountsAsync()
    {
        var counts = new Dictionary<string, int>();
        
        try
        {
            var tables = await GetAllTableNamesAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var table in tables)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {table}";
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                counts[table] = count;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting row counts: {ex.Message}");
        }

        return counts;
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
