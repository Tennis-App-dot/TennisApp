using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// DAO สำหรับจัดการข้อมูล CourseCourtUseLog (บันทึกการใช้สนามจากคอร์ส)
/// </summary>
public class CourseCourtUseLogDao
{
    private readonly string _connectionString;

    public CourseCourtUseLogDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeTable();
    }

    /// <summary>
    /// สร้างตาราง CourseCourtUseLog ถ้ายังไม่มี
    /// </summary>
    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // ✅ Enable Foreign Key enforcement
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
        pragmaCommand.ExecuteNonQuery();

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS CourseCourtUseLog (
                c_log_id           TEXT(10) PRIMARY KEY NOT NULL,
                c_reserve_id       TEXT(10) NOT NULL,
                c_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                c_log_duration     REAL NOT NULL,
                c_log_status       TEXT(20) NOT NULL DEFAULT 'completed',
                
                FOREIGN KEY (c_reserve_id) REFERENCES CourseCourtReservation(c_reserve_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_CourseCourtUseLog_Reserve ON CourseCourtUseLog(c_reserve_id);
            CREATE INDEX IF NOT EXISTS IX_CourseCourtUseLog_CheckIn ON CourseCourtUseLog(c_checkin_time);
        ";
        createCommand.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine("✅ CourseCourtUseLog table initialized");
    }

    // ========================================================================
    // CREATE - Insert new usage log
    // ========================================================================

    /// <summary>
    /// เพิ่มบันทึกการใช้สนามใหม่ (จากคอร์ส)
    /// </summary>
    public async Task<bool> InsertAsync(CourseCourtUseLogItem log)
    {
        const string sql = @"
            INSERT INTO CourseCourtUseLog (
                c_log_id, c_reserve_id, c_checkin_time, 
                c_log_duration, c_log_status
            )
            VALUES (
                @log_id, @reserve_id, @checkin_time, 
                @log_duration, @log_status
            )";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@log_id", log.LogId);
            command.Parameters.AddWithValue("@reserve_id", log.ReserveId);
            command.Parameters.AddWithValue("@checkin_time", log.CheckInTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@log_duration", log.LogDuration);
            command.Parameters.AddWithValue("@log_status", log.LogStatus);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ CourseCourtUseLogDao.InsertAsync: {rowsAffected} row(s) inserted");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.InsertAsync error: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // READ - Select usage logs
    // ========================================================================

    // ─── Shared SQL fragment ──────────────────────────────────
    private const string SelectWithJoins = @"
        SELECT 
            l.c_log_id,
            l.c_reserve_id,
            l.c_checkin_time,
            l.c_log_duration,
            l.c_log_status,
            r.court_id,
            r.class_id,
            r.c_reserve_name,
            r.c_reserve_phone,
            r.c_reserve_date,
            r.c_reserve_time,
            co.class_title
        FROM CourseCourtUseLog l
        INNER JOIN CourseCourtReservation r ON l.c_reserve_id = r.c_reserve_id
        INNER JOIN Course co ON r.class_id = co.class_id AND r.trainer_id = co.trainer_id";

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามทั้งหมด (พร้อม JOIN กับ CourseCourtReservation และ Course)
    /// </summary>
    public async Task<List<CourseCourtUseLogItem>> GetAllAsync()
    {
        var sql = SelectWithJoins + " ORDER BY l.c_checkin_time DESC";

        var logs = new List<CourseCourtUseLogItem>();

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(MapFromReader(reader));
            }

            Debug.WriteLine($"✅ CourseCourtUseLogDao.GetAllAsync: {logs.Count} log(s) retrieved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.GetAllAsync error: {ex.Message}");
        }

        return logs;
    }

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามตาม Log ID
    /// </summary>
    public async Task<CourseCourtUseLogItem?> GetByIdAsync(string logId)
    {
        var sql = SelectWithJoins + " WHERE l.c_log_id = @log_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@log_id", logId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.GetByIdAsync error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามตาม Reserve ID
    /// </summary>
    public async Task<CourseCourtUseLogItem?> GetByReserveIdAsync(string reserveId)
    {
        var sql = SelectWithJoins + " WHERE l.c_reserve_id = @reserve_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@reserve_id", reserveId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.GetByReserveIdAsync error: {ex.Message}");
        }

        return null;
    }

    // ========================================================================
    // UPDATE - Update usage log
    // ========================================================================

    /// <summary>
    /// อัปเดตข้อมูลบันทึกการใช้สนาม
    /// </summary>
    public async Task<bool> UpdateAsync(CourseCourtUseLogItem log)
    {
        const string sql = @"
            UPDATE CourseCourtUseLog
            SET
                c_log_duration = @log_duration,
                c_log_status = @log_status
            WHERE c_log_id = @log_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@log_duration", log.LogDuration);
            command.Parameters.AddWithValue("@log_status", log.LogStatus);
            command.Parameters.AddWithValue("@log_id", log.LogId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ CourseCourtUseLogDao.UpdateAsync: {rowsAffected} row(s) updated");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.UpdateAsync error: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // DELETE - Delete usage log
    // ========================================================================

    /// <summary>
    /// ลบบันทึกการใช้สนาม
    /// </summary>
    public async Task<bool> DeleteAsync(string logId)
    {
        const string sql = "DELETE FROM CourseCourtUseLog WHERE c_log_id = @log_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // ✅ Enable Foreign Key enforcement
            await using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
            await pragmaCmd.ExecuteNonQueryAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@log_id", logId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ CourseCourtUseLogDao.DeleteAsync: {rowsAffected} row(s) deleted");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.DeleteAsync error: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    /// <summary>
    /// แปลงข้อมูลจาก DbDataReader เป็น CourseCourtUseLogItem
    /// </summary>
    private static CourseCourtUseLogItem MapFromReader(DbDataReader reader)
    {
        return new CourseCourtUseLogItem
        {
            LogId = reader.GetString(reader.GetOrdinal("c_log_id")),
            ReserveId = reader.GetString(reader.GetOrdinal("c_reserve_id")),
            CheckInTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("c_checkin_time"))),
            LogDuration = reader.GetDouble(reader.GetOrdinal("c_log_duration")),
            LogStatus = reader.GetString(reader.GetOrdinal("c_log_status")),
            
            // From CourseCourtReservation (JOIN)
            CourtId = reader.GetString(reader.GetOrdinal("court_id")),
            ClassId = reader.GetString(reader.GetOrdinal("class_id")),
            ReserveName = reader.GetString(reader.GetOrdinal("c_reserve_name")),
            ReservePhone = reader.IsDBNull(reader.GetOrdinal("c_reserve_phone")) 
                ? string.Empty 
                : reader.GetString(reader.GetOrdinal("c_reserve_phone")),
            ReserveDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("c_reserve_date"))),
            ReserveTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("c_reserve_time"))),
            
            // From Course (JOIN)
            ClassTitle = reader.GetString(reader.GetOrdinal("class_title"))
        };
    }

    /// <summary>
    /// หา max log_id ที่ขึ้นต้นด้วย prefix (สำหรับ ID generation)
    /// </summary>
    public async Task<string?> GetMaxLogIdByPrefixAsync(string prefix)
    {
        const string sql = "SELECT MAX(c_log_id) FROM CourseCourtUseLog WHERE c_log_id LIKE @prefix || '%'";
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@prefix", prefix);
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? null : result?.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ CourseCourtUseLogDao.GetMaxLogIdByPrefixAsync error: {ex.Message}");
            return null;
        }
    }
}
