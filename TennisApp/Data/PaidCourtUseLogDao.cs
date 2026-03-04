using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// DAO สำหรับจัดการข้อมูล PaidCourtUseLog (บันทึกการใช้สนามแบบเช่า)
/// </summary>
public class PaidCourtUseLogDao
{
    private readonly string _connectionString;

    public PaidCourtUseLogDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeTable();
    }

    /// <summary>
    /// สร้างตาราง PaidCourtUseLog ถ้ายังไม่มี
    /// </summary>
    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS PaidCourtUseLog (
                p_log_id           TEXT(10) PRIMARY KEY NOT NULL,
                p_reserve_id       TEXT(10) NOT NULL,
                p_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                p_log_duration     REAL NOT NULL,
                p_log_price        INTEGER NOT NULL,
                p_log_status       TEXT(20) NOT NULL DEFAULT 'completed',
                
                FOREIGN KEY (p_reserve_id) REFERENCES PaidCourtReservation(p_reserve_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_PaidCourtUseLog_Reserve ON PaidCourtUseLog(p_reserve_id);
            CREATE INDEX IF NOT EXISTS IX_PaidCourtUseLog_CheckIn ON PaidCourtUseLog(p_checkin_time);
        ";
        createCommand.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine("✅ PaidCourtUseLog table initialized");
    }

    // ========================================================================
    // CREATE - Insert new usage log
    // ========================================================================

    /// <summary>
    /// เพิ่มบันทึกการใช้สนามใหม่
    /// </summary>
    public async Task<bool> InsertAsync(PaidCourtUseLogItem log)
    {
        const string sql = @"
            INSERT INTO PaidCourtUseLog (
                p_log_id, p_reserve_id, p_checkin_time, 
                p_log_duration, p_log_price, p_log_status
            )
            VALUES (
                @log_id, @reserve_id, @checkin_time, 
                @log_duration, @log_price, @log_status
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
            command.Parameters.AddWithValue("@log_price", log.LogPrice);
            command.Parameters.AddWithValue("@log_status", log.LogStatus);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ PaidCourtUseLogDao.InsertAsync: {rowsAffected} row(s) inserted");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ PaidCourtUseLogDao.InsertAsync error: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // READ - Select usage logs
    // ========================================================================

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามทั้งหมด (พร้อม JOIN กับ PaidCourtReservation)
    /// </summary>
    public async Task<List<PaidCourtUseLogItem>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                l.p_log_id,
                l.p_reserve_id,
                l.p_checkin_time,
                l.p_log_duration,
                l.p_log_price,
                l.p_log_status,
                r.court_id,
                r.p_reserve_name,
                r.p_reserve_phone,
                r.p_reserve_date,
                r.p_reserve_time,
                r.p_reserve_duration
            FROM PaidCourtUseLog l
            INNER JOIN PaidCourtReservation r ON l.p_reserve_id = r.p_reserve_id
            ORDER BY l.p_checkin_time DESC";

        var logs = new List<PaidCourtUseLogItem>();

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

            Debug.WriteLine($"✅ PaidCourtUseLogDao.GetAllAsync: {logs.Count} log(s) retrieved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ PaidCourtUseLogDao.GetAllAsync error: {ex.Message}");
        }

        return logs;
    }

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามตาม Log ID
    /// </summary>
    public async Task<PaidCourtUseLogItem?> GetByIdAsync(string logId)
    {
        const string sql = @"
            SELECT 
                l.p_log_id,
                l.p_reserve_id,
                l.p_checkin_time,
                l.p_log_duration,
                l.p_log_price,
                l.p_log_status,
                r.court_id,
                r.p_reserve_name,
                r.p_reserve_phone,
                r.p_reserve_date,
                r.p_reserve_time,
                r.p_reserve_duration
            FROM PaidCourtUseLog l
            INNER JOIN PaidCourtReservation r ON l.p_reserve_id = r.p_reserve_id
            WHERE l.p_log_id = @log_id";

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
            Debug.WriteLine($"❌ PaidCourtUseLogDao.GetByIdAsync error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// ดึงข้อมูลบันทึกการใช้สนามตาม Reserve ID
    /// </summary>
    public async Task<PaidCourtUseLogItem?> GetByReserveIdAsync(string reserveId)
    {
        const string sql = @"
            SELECT 
                l.p_log_id,
                l.p_reserve_id,
                l.p_checkin_time,
                l.p_log_duration,
                l.p_log_price,
                l.p_log_status,
                r.court_id,
                r.p_reserve_name,
                r.p_reserve_phone,
                r.p_reserve_date,
                r.p_reserve_time,
                r.p_reserve_duration
            FROM PaidCourtUseLog l
            INNER JOIN PaidCourtReservation r ON l.p_reserve_id = r.p_reserve_id
            WHERE l.p_reserve_id = @reserve_id";

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
            Debug.WriteLine($"❌ PaidCourtUseLogDao.GetByReserveIdAsync error: {ex.Message}");
        }

        return null;
    }

    // ========================================================================
    // UPDATE - Update usage log
    // ========================================================================

    /// <summary>
    /// อัปเดตข้อมูลบันทึกการใช้สนาม
    /// </summary>
    public async Task<bool> UpdateAsync(PaidCourtUseLogItem log)
    {
        const string sql = @"
            UPDATE PaidCourtUseLog
            SET
                p_log_duration = @log_duration,
                p_log_price = @log_price,
                p_log_status = @log_status
            WHERE p_log_id = @log_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@log_duration", log.LogDuration);
            command.Parameters.AddWithValue("@log_price", log.LogPrice);
            command.Parameters.AddWithValue("@log_status", log.LogStatus);
            command.Parameters.AddWithValue("@log_id", log.LogId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ PaidCourtUseLogDao.UpdateAsync: {rowsAffected} row(s) updated");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ PaidCourtUseLogDao.UpdateAsync error: {ex.Message}");
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
        const string sql = "DELETE FROM PaidCourtUseLog WHERE p_log_id = @log_id";

        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@log_id", logId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"✅ PaidCourtUseLogDao.DeleteAsync: {rowsAffected} row(s) deleted");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ PaidCourtUseLogDao.DeleteAsync error: {ex.Message}");
            return false;
        }
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    /// <summary>
    /// แปลงข้อมูลจาก DbDataReader เป็น PaidCourtUseLogItem
    /// </summary>
    private static PaidCourtUseLogItem MapFromReader(DbDataReader reader)
    {
        return new PaidCourtUseLogItem
        {
            LogId = reader.GetString(reader.GetOrdinal("p_log_id")),
            ReserveId = reader.GetString(reader.GetOrdinal("p_reserve_id")),
            CheckInTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("p_checkin_time"))),
            LogDuration = reader.GetDouble(reader.GetOrdinal("p_log_duration")),
            LogPrice = reader.GetInt32(reader.GetOrdinal("p_log_price")),
            LogStatus = reader.GetString(reader.GetOrdinal("p_log_status")),
            
            // From PaidCourtReservation (JOIN)
            CourtId = reader.GetString(reader.GetOrdinal("court_id")),
            ReserveName = reader.GetString(reader.GetOrdinal("p_reserve_name")),
            ReservePhone = reader.IsDBNull(reader.GetOrdinal("p_reserve_phone")) 
                ? string.Empty 
                : reader.GetString(reader.GetOrdinal("p_reserve_phone")),
            ReserveDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("p_reserve_date"))),
            ReserveTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("p_reserve_time"))),
            ReserveDuration = reader.GetDouble(reader.GetOrdinal("p_reserve_duration"))
        };
    }
}
