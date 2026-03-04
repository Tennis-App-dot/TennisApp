using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;
using System.IO;

namespace TennisApp.Data;

/// <summary>
/// Data Access Object สำหรับจัดการข้อมูลสนามตาม Official Court Table Schema
/// </summary>
public class CourtDao
{
    private readonly string _connectionString;

    public CourtDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    /// <summary>
    /// สร้างตาราง Court และ trigger ตาม official schema
    /// </summary>
    private void InitializeDatabase()
    {
        System.Diagnostics.Debug.WriteLine("🗄️ เริ่ม InitializeDatabase...");
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            -- Main Court table (Official Schema)
            CREATE TABLE IF NOT EXISTS Court (
                court_id      TEXT(2) PRIMARY KEY NOT NULL,
                court_img     BLOB NULL,
                court_status  TEXT(1) NOT NULL DEFAULT '1',
                last_updated  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- Performance indexes
            CREATE INDEX IF NOT EXISTS IX_Court_Status ON Court(court_status);
            CREATE INDEX IF NOT EXISTS IX_Court_LastUpdated ON Court(last_updated);
        ";
        createCommand.ExecuteNonQuery();
        
        System.Diagnostics.Debug.WriteLine("✅ สร้างตารางเสร็จ");

        // ✅ เพิ่มสนาม dummy "00" สำหรับการจองที่ยังไม่ได้จัดสรรสนาม (ต้องทำก่อน PaidCourtReservation)
        System.Diagnostics.Debug.WriteLine("🔍 ตรวจสอบสนาม dummy '00'...");
        
        var checkDummyCommand = connection.CreateCommand();
        checkDummyCommand.CommandText = "SELECT COUNT(*) FROM Court WHERE court_id = '00'";
        var existsDummy = Convert.ToInt32(checkDummyCommand.ExecuteScalar()) > 0;

        if (!existsDummy)
        {
            var insertDummyCommand = connection.CreateCommand();
            insertDummyCommand.CommandText = @"
                INSERT INTO Court (court_id, court_img, court_status, last_updated)
                VALUES ('00', NULL, '0', CURRENT_TIMESTAMP)
            ";
            insertDummyCommand.ExecuteNonQuery();
            
            System.Diagnostics.Debug.WriteLine("✅ สร้างสนาม dummy '00' สำเร็จ (สถานะ: รอจัดสรรสนาม)");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("✅ สนาม dummy '00' มีอยู่แล้ว");
        }

        // ตรวจสอบข้อมูลปัจจุบัน
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Court";
        var count = Convert.ToInt32(checkCommand.ExecuteScalar());

        System.Diagnostics.Debug.WriteLine($"📊 จำนวนสนามปัจจุบัน (รวม dummy): {count}");
    }

    /// <summary>
    /// ดึงข้อมูลสนามทั้งหมด (ไม่รวมสนาม dummy "00")
    /// </summary>
    public async Task<List<CourtItem>> GetAllCourtsAsync()
    {
        System.Diagnostics.Debug.WriteLine("🔍 GetAllCourtsAsync เริ่มทำงาน...");
        
        var courts = new List<CourtItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        
        System.Diagnostics.Debug.WriteLine("✅ Database connection เปิดสำเร็จ");

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT court_id, court_img, court_status, last_updated
            FROM Court
            WHERE court_id != '00'
            ORDER BY court_id
        ";

        System.Diagnostics.Debug.WriteLine($"🔧 SQL: {command.CommandText}");

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        int rowCount = 0;
        
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            rowCount++;
            
            var courtId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var imageData = reader.IsDBNull(1) ? null : (byte[])reader.GetValue(1);
            var status = reader.IsDBNull(2) ? "1" : reader.GetString(2);
            var lastUpdatedStr = reader.IsDBNull(3) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(3);
            
            System.Diagnostics.Debug.WriteLine($"   📋 Row {rowCount}: Court {courtId}, Status {status}, Updated: {lastUpdatedStr}");
            
            // ✅ ใช้ static factory method สำหรับสร้าง CourtItem จาก database
            var court = CourtItem.FromDatabase(courtId, imageData, status, lastUpdatedStr);
            court.ImagePath = GetImagePathFromData(imageData);
            
            courts.Add(court);
            
            System.Diagnostics.Debug.WriteLine($"   ✅ Created CourtItem: {court.DisplayName}, LastUpdated: {court.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        }

        System.Diagnostics.Debug.WriteLine($"📊 GetAllCourtsAsync เสร็จ - พบ {courts.Count} สนาม (ไม่รวม dummy)");
        return courts;
    }

    /// <summary>
    /// ดึงข้อมูลสนามตาม Status
    /// </summary>
    public async Task<List<CourtItem>> GetCourtsByStatusAsync(string status)
    {
        var courts = new List<CourtItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT court_id, court_img, court_status, last_updated
            FROM Court
            WHERE court_status = @status AND court_id != '00'
            ORDER BY court_id
        ";
        command.Parameters.AddWithValue("@status", status);

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var courtId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            var imageData = reader.IsDBNull(1) ? null : (byte[])reader.GetValue(1);
            var courtStatus = reader.IsDBNull(2) ? "1" : reader.GetString(2);
            var lastUpdatedStr = reader.IsDBNull(3) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(3);
            
            // ✅ ใช้ static factory method
            var court = CourtItem.FromDatabase(courtId, imageData, courtStatus, lastUpdatedStr);
            court.ImagePath = GetImagePathFromData(imageData);
            
            courts.Add(court);
        }

        return courts;
    }

    /// <summary>
    /// เพิ่มสนามใหม่ (Insert) - ใช้ LastUpdated จาก UI
    /// </summary>
    public async Task<bool> AddCourtAsync(CourtItem court)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ CourtDao.AddCourtAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   CourtID: {court.CourtID}");
        System.Diagnostics.Debug.WriteLine($"   Status: {court.Status}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdated: {court.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        System.Diagnostics.Debug.WriteLine($"   ImageData: {court.ImageData?.Length ?? 0} bytes");
        
        await using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine("✅ Database connection เปิดสำเร็จ");

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Court (court_id, court_img, court_status, last_updated)
                VALUES (@court_id, @court_img, @court_status, @last_updated)
            ";

            command.Parameters.AddWithValue("@court_id", court.CourtID);
            command.Parameters.AddWithValue("@court_img", court.ImageData ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@court_status", court.Status);
            command.Parameters.AddWithValue("@last_updated", court.LastUpdatedForDatabase);

            System.Diagnostics.Debug.WriteLine($"🔧 SQL: {command.CommandText}");
            System.Diagnostics.Debug.WriteLine($"📋 Parameters: court_id={court.CourtID}, court_status={court.Status}, last_updated={court.LastUpdatedForDatabase}");

            var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"📈 Rows affected: {result}");
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 AddCourtAsync result: {success}");
            
            return success;
        }
        catch (SqliteException sqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SQLite Error: {sqlEx.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 SQLite ErrorCode: {sqlEx.SqliteErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ General Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// แก้ไขข้อมูลสนาม (Update) - ใช้ LastUpdated จาก UI
    /// </summary>
    public async Task<bool> UpdateCourtAsync(CourtItem court)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ CourtDao.UpdateCourtAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   CourtID: {court.CourtID}");
        System.Diagnostics.Debug.WriteLine($"   Status: {court.Status}");
        System.Diagnostics.Debug.WriteLine($"   LastUpdated: {court.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        
        await using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync().ConfigureAwait(false);

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Court
                SET
                    court_img    = @court_img,
                    court_status = @court_status,
                    last_updated = @last_updated
                WHERE court_id = @court_id
            ";

            command.Parameters.AddWithValue("@court_id", court.CourtID);
            command.Parameters.AddWithValue("@court_img", court.ImageData ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@court_status", court.Status);
            command.Parameters.AddWithValue("@last_updated", court.LastUpdatedForDatabase);

            System.Diagnostics.Debug.WriteLine($"🔧 SQL: {command.CommandText}");
            System.Diagnostics.Debug.WriteLine($"📋 Parameters: court_id={court.CourtID}, last_updated={court.LastUpdatedForDatabase}");

            var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"📈 Rows affected: {result}");
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 UpdateCourtAsync result: {success}");
            
            return success;
        }
        catch (SqliteException sqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SQLite Error: {sqlEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ General Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ลบสนาม (Delete)
    /// </summary>
    public async Task<bool> DeleteCourtAsync(string courtId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Court WHERE court_id = @court_id";
        command.Parameters.AddWithValue("@court_id", courtId);

        var result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        return result > 0;
    }

    /// <summary>
    /// ตรวจสอบว่ามี CourtID นี้อยู่แล้วหรือไม่
    /// </summary>
    public async Task<bool> CourtExistsAsync(string courtId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Court WHERE court_id = @court_id";
        command.Parameters.AddWithValue("@court_id", courtId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
        return count > 0;
    }

    /// <summary>
    /// หา CourtID ถัดไปที่ว่าง
    /// </summary>
    public async Task<string> GetNextAvailableCourtIdAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT court_id FROM Court ORDER BY court_id";

        var existingIds = new HashSet<int>();
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (int.TryParse(reader.GetString(0), out var id)) // court_id is column 0
            {
                existingIds.Add(id);
            }
        }

        // หา ID ว่างแรก
        for (int i = 1; i <= 99; i++)
        {
            if (!existingIds.Contains(i))
            {
                return i.ToString("00");
            }
        }

        throw new InvalidOperationException("ไม่มี CourtID ว่างเหลือ (เต็ม 99 สนาม)");
    }

    /// <summary>
    /// แปลง ImageData เป็น ImagePath สำหรับ UI
    /// </summary>
    private static string GetImagePathFromData(byte[]? imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            return "ms-appx:///Assets/Courts/court1.jpg"; // Default image
        }

        try
        {
            // สร้างไฟล์ temp สำหรับแสดงผลรูปภาพ
            var tempPath = Path.GetTempPath();
            var fileName = $"court_image_{Guid.NewGuid():N}.jpg";
            var fullPath = Path.Combine(tempPath, fileName);

            // บันทึก BLOB เป็นไฟล์
            File.WriteAllBytes(fullPath, imageData);

            return fullPath;
        }
        catch
        {
            // หากมีปัญหา ให้ใช้รูปเดิม
            return "ms-appx:///Assets/Courts/court1.jpg";
        }
    }
}
