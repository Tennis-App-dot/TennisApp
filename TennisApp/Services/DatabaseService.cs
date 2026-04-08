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
    private readonly object _initLock = new();

    public CourtDao Courts { get; private set; } = null!;
    public TraineeDao Trainees { get; private set; } = null!;
    public TrainerDao Trainers { get; private set; } = null!;
    public CourseDao Courses { get; private set; } = null!;
    public ClassRegisRecordDao Registrations { get; private set; } = null!;

    public PaidCourtReservationDao PaidCourtReservations { get; private set; } = null!;
    public CourseCourtReservationDao CourseCourtReservations { get; private set; } = null!;

    public PaidCourtUseLogDao PaidCourtUseLogs { get; private set; } = null!;
    public CourseCourtUseLogDao CourseCourtUseLogs { get; private set; } = null!;
    public CourseTypeDao CourseTypes { get; private set; } = null!;

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
        _connectionString = $"Data Source={_databasePath};Pooling=false";
    }

    /// <summary>
    /// ตั้งค่า SQLite PRAGMAs เพื่อเพิ่ม performance
    /// - WAL mode: อ่าน/เขียนพร้อมกันได้ ไม่ lock DB
    /// - synchronous=NORMAL: เร็วขึ้น แต่ยังปลอดภัย
    /// - cache_size: เพิ่ม in-memory cache
    /// - temp_store=MEMORY: temp tables อยู่ใน RAM
    /// - mmap_size: memory-mapped I/O
    /// </summary>
    private void ApplyPerformancePragmas()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string[] pragmas =
            [
                "PRAGMA journal_mode=WAL",
                "PRAGMA synchronous=NORMAL",
                "PRAGMA cache_size=-8000",       // 8MB cache
                "PRAGMA temp_store=MEMORY",
                "PRAGMA mmap_size=134217728",    // 128MB mmap
                "PRAGMA page_size=4096",
                "PRAGMA foreign_keys=ON"
            ];

            foreach (var pragma in pragmas)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = pragma;
                var result = cmd.ExecuteScalar();
                System.Diagnostics.Debug.WriteLine($"   📊 {pragma} → {result}");
            }

            System.Diagnostics.Debug.WriteLine("✅ SQLite performance PRAGMAs applied");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ PRAGMA error (non-fatal): {ex.Message}");
        }
    }

    /// <summary>
    /// Lazy initialization — สร้าง DAO ครั้งแรกที่เรียกใช้ (thread-safe)
    /// </summary>
    public void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            InitializeDAOs();
            _initialized = true;
        }
    }

    private void InitializeDAOs()
    {
        // ✅ ตั้งค่า SQLite performance PRAGMAs
        ApplyPerformancePragmas();

        // ✅ สร้าง Court DAO ก่อน (เพื่อให้มีสนาม "00" สำหรับ Foreign Key)
        Courts = new CourtDao(_connectionString);
        
        // สร้าง DAOs อื่น ๆ
        Trainees = new TraineeDao(_connectionString);
        Trainers = new TrainerDao(_connectionString);
        CourseTypes = new CourseTypeDao(_connectionString);
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

        await Task.Run(() => EnsureInitialized());

        System.Diagnostics.Debug.WriteLine("✅ DatabaseService initialized asynchronously");
    }

    /// <summary>
    /// รีเซ็ตฐานข้อมูล — ลบไฟล์ .db ทิ้งแล้วสร้างใหม่เปล่าๆ (ทุกตาราง + dummy court "00")
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        System.Diagnostics.Debug.WriteLine("🗑️ ResetDatabaseAsync: เริ่มรีเซ็ตฐานข้อมูล...");

        lock (_initLock)
        {
            _initialized = false;
        }

        // ✅ Clear connection pool (safety net — Pooling=false ทำให้ไม่ค่อยจำเป็น)
        SqliteConnection.ClearAllPools();

        // ✅ ลบไฟล์ database — retry ในกรณีที่ยังมี handle ค้าง
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                    System.Diagnostics.Debug.WriteLine($"   ✅ ลบไฟล์ database สำเร็จ (attempt {attempt})");
                }
                break;
            }
            catch (IOException) when (attempt < 5)
            {
                System.Diagnostics.Debug.WriteLine($"   ⏳ ไฟล์ยังถูก lock (attempt {attempt}/5) รอ...");
                await Task.Delay(200 * attempt);
                SqliteConnection.ClearAllPools();
            }
        }

        // ✅ ยืนยันว่าไฟล์ถูกลบจริง
        if (File.Exists(_databasePath))
        {
            var msg = $"ไม่สามารถลบไฟล์ database ได้: {_databasePath}";
            System.Diagnostics.Debug.WriteLine($"❌ {msg}");
            throw new IOException(msg);
        }

        // ✅ สร้าง database ใหม่เปล่าๆ
        await Task.Run(() => EnsureInitialized());

        // ✅ ยืนยันว่า database ใหม่ถูกสร้างจริง
        if (!File.Exists(_databasePath))
        {
            System.Diagnostics.Debug.WriteLine("❌ ไม่สามารถสร้าง database ใหม่ได้");
            throw new InvalidOperationException("ไม่สามารถสร้างฐานข้อมูลใหม่ได้");
        }

        System.Diagnostics.Debug.WriteLine("✅ ResetDatabaseAsync: ฐานข้อมูลใหม่พร้อมใช้งาน (เปล่า)");
    }

    /// <summary>
    /// รีเซ็ตฐานข้อมูลแบบ synchronous (สำหรับ testing)
    /// </summary>
    public void ResetDatabase()
    {
        lock (_initLock)
        {
            _initialized = false;
        }

        SqliteConnection.ClearAllPools();

        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (File.Exists(_databasePath))
                    File.Delete(_databasePath);
                break;
            }
            catch (IOException) when (attempt < 5)
            {
                System.Threading.Thread.Sleep(200 * attempt);
                SqliteConnection.ClearAllPools();
            }
        }

        EnsureInitialized();
    }

    /// <summary>
    /// ลบข้อมูลทั้งหมดในทุกตาราง (เก็บโครงสร้างตารางไว้ + สร้าง dummy court "00" ใหม่)
    /// ลำดับ DELETE ต้องถูกต้องตาม FK: ลูกก่อน → พ่อทีหลัง
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        System.Diagnostics.Debug.WriteLine("🧹 ClearAllDataAsync: เริ่มลบข้อมูลทั้งหมด...");

        EnsureInitialized();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // ✅ เปิด FK เพื่อให้ CASCADE ทำงาน
        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        // ✅ ลบตามลำดับ FK: ลูกสุด → พ่อสุด
        string[] deleteOrder =
        [
            "DELETE FROM PaidCourtUseLog",
            "DELETE FROM CourseCourtUseLog",
            "DELETE FROM PaidCourtReservation",
            "DELETE FROM CourseCourtReservation",
            "DELETE FROM ClassRegisRecord",
            "DELETE FROM Course",
            "DELETE FROM CoursePackage",
            "DELETE FROM CourseType",
            "DELETE FROM Trainee",
            "DELETE FROM Trainer",
            "DELETE FROM Court WHERE court_id != '00'"   // เก็บ dummy "00" ไว้
        ];

        foreach (var sql in deleteOrder)
        {
            try
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                var rows = await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"   ✅ {sql} → {rows} rows deleted");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ {sql} → {ex.Message}");
            }
        }

        // ✅ VACUUM เพื่อคืนพื้นที่ disk (SQLite ไม่ย่อไฟล์อัตโนมัติหลัง DELETE)
        try
        {
            var vacuumCmd = connection.CreateCommand();
            vacuumCmd.CommandText = "VACUUM";
            await vacuumCmd.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine("   ✅ VACUUM สำเร็จ — คืนพื้นที่ disk แล้ว");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"   ⚠️ VACUUM ล้มเหลว (ไม่ร้ายแรง): {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine("✅ ClearAllDataAsync: ลบข้อมูลทั้งหมดเรียบร้อย");
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
