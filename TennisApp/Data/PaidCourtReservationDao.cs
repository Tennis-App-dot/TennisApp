using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// Data Access Object for PaidCourtReservation table
/// Handles all database operations for paid court reservations (การจองสนามแบบเช่า)
/// </summary>
public class PaidCourtReservationDao
{
    private readonly string _connectionString;

    public PaidCourtReservationDao(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        InitializeTable();
    }

    /// <summary>
    /// สร้างตาราง PaidCourtReservation ถ้ายังไม่มี
    /// </summary>
    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // ✅ สร้างสนาม dummy "00" ก่อน (ถ้ายังไม่มี)
        EnsureDummyCourtExists(connection);

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS PaidCourtReservation (
                p_reserve_id       TEXT(10) PRIMARY KEY NOT NULL,
                court_id           TEXT(2) NOT NULL,
                p_request_date     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                p_reserve_date     DATE NOT NULL,
                p_reserve_time     TIME NOT NULL,
                p_reserve_duration REAL NOT NULL,
                p_reserve_name     TEXT(50) NOT NULL,
                p_reserve_phone    TEXT(10) NULL,
                p_status           TEXT(20) NOT NULL DEFAULT 'booked'
            );
        ";
        createCommand.ExecuteNonQuery();

        // ✅ เพิ่มคอลัมน์ p_status ถ้ายังไม่มี (สำหรับ DB เดิม)
        try
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE PaidCourtReservation ADD COLUMN p_status TEXT(20) NOT NULL DEFAULT 'booked'";
            alterCommand.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine("✅ PaidCourtReservation: เพิ่มคอลัมน์ p_status สำเร็จ");
        }
        catch
        {
            // คอลัมน์มีอยู่แล้ว → ข้ามไป
        }

        // ✅ สร้าง indexes หลังจากมั่นใจว่าคอลัมน์ p_status มีแล้ว
        var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            CREATE INDEX IF NOT EXISTS IX_PaidCourtReservation_Court ON PaidCourtReservation(court_id);
            CREATE INDEX IF NOT EXISTS IX_PaidCourtReservation_Date ON PaidCourtReservation(p_reserve_date);
            CREATE INDEX IF NOT EXISTS IX_PaidCourtReservation_Request ON PaidCourtReservation(p_request_date);
            CREATE INDEX IF NOT EXISTS IX_PaidCourtReservation_Status ON PaidCourtReservation(p_status);
        ";
        indexCommand.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine("✅ PaidCourtReservation table initialized");
    }

    /// <summary>
    /// ตรวจสอบและสร้างสนาม dummy "00" ถ้ายังไม่มี
    /// </summary>
    private void EnsureDummyCourtExists(SqliteConnection connection)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔍 PaidReservationDao: ตรวจสอบสนาม dummy '00'...");
            
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Court WHERE court_id = '00'";
            var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

            if (!exists)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Court (court_id, court_img, court_status, last_updated)
                    VALUES ('00', NULL, '0', CURRENT_TIMESTAMP)
                ";
                insertCommand.ExecuteNonQuery();
                
                System.Diagnostics.Debug.WriteLine("✅ PaidReservationDao: สร้างสนาม dummy '00' สำเร็จ");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ PaidReservationDao: สนาม dummy '00' มีอยู่แล้ว");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ PaidReservationDao: ไม่สามารถสร้างสนาม dummy: {ex.Message}");
        }
    }

    // ========================================================================
    // CREATE
    // ========================================================================

    /// <summary>
    /// Add a new paid court reservation
    /// </summary>
    public async Task<bool> AddReservationAsync(PaidCourtReservationItem reservation)
    {
        const string sql = @"
            INSERT INTO PaidCourtReservation 
            (p_reserve_id, court_id, p_request_date, p_reserve_date, p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status)
            VALUES 
            (@ReserveId, @CourtId, @RequestDate, @ReserveDate, @ReserveTime, @Duration, @ReserveName, @ReservePhone, @Status)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reservation.ReserveId);
        cmd.Parameters.AddWithValue("@CourtId", reservation.CourtId);
        cmd.Parameters.AddWithValue("@RequestDate", reservation.RequestDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@ReserveDate", reservation.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ReserveTime", reservation.ReserveTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@Duration", reservation.Duration);
        cmd.Parameters.AddWithValue("@ReserveName", reservation.ReserveName);
        cmd.Parameters.AddWithValue("@ReservePhone", string.IsNullOrEmpty(reservation.ReservePhone) ? DBNull.Value : reservation.ReservePhone);
        cmd.Parameters.AddWithValue("@Status", reservation.Status ?? "booked");

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    // ========================================================================
    // READ
    // ========================================================================

    /// <summary>
    /// Get all paid court reservations
    /// </summary>
    public async Task<List<PaidCourtReservationItem>> GetAllReservationsAsync()
    {
        const string sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            ORDER BY p_reserve_date DESC, p_reserve_time DESC";

        var reservations = new List<PaidCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReader(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservation by ID
    /// </summary>
    public async Task<PaidCourtReservationItem?> GetReservationByIdAsync(string reserveId)
    {
        const string sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (await reader.ReadAsync().ConfigureAwait(false))
        {
            return MapFromReader(reader);
        }

        return null;
    }

    /// <summary>
    /// Get reservations by court ID
    /// </summary>
    public async Task<List<PaidCourtReservationItem>> GetReservationsByCourtAsync(string courtId)
    {
        const string sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            WHERE court_id = @CourtId
            ORDER BY p_reserve_date DESC, p_reserve_time DESC";

        var reservations = new List<PaidCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CourtId", courtId);

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReader(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservations by reserve date
    /// </summary>
    public async Task<List<PaidCourtReservationItem>> GetReservationsByDateAsync(DateTime reserveDate)
    {
        const string sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            WHERE p_reserve_date = @ReserveDate
            ORDER BY p_reserve_time";

        var reservations = new List<PaidCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReader(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservations by request date
    /// </summary>
    public async Task<List<PaidCourtReservationItem>> GetReservationsByRequestDateAsync(DateTime requestDate)
    {
        const string sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            WHERE date(p_request_date) = @RequestDate
            ORDER BY p_reserve_id";

        var reservations = new List<PaidCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RequestDate", requestDate.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReader(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Check if court is available at specific time
    /// Returns true if court is available (no overlapping reservations)
    /// </summary>
    public async Task<bool> IsCourtAvailableAsync(string courtId, DateTime reserveDate, TimeSpan startTime, double duration)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM PaidCourtReservation
            WHERE court_id = @CourtId
              AND p_reserve_date = @ReserveDate
              AND time(p_reserve_time) < time(@EndTime)
              AND time(p_reserve_time, '+' || p_reserve_duration || ' hours') > time(@StartTime)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CourtId", courtId);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@StartTime", startTime.ToString(@"hh\:mm\:ss")); // ✅ Fixed format
        
        var endTime = startTime.Add(TimeSpan.FromHours(duration));
        cmd.Parameters.AddWithValue("@EndTime", endTime.ToString(@"hh\:mm\:ss")); // ✅ Fixed format

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
        return count == 0; // Available if no overlapping reservations
    }

    /// <summary>
    /// ตรวจสอบว่ามีการจองซ้ำหรือไม่ (ชื่อเดียวกัน + วันเดียวกัน + เวลาซ้อนทับ)
    /// ใช้เช็คทั้ง Paid และ Course reservation
    /// </summary>
    public async Task<bool> HasDuplicateReservationAsync(string reserveName, DateTime reserveDate, TimeSpan startTime, double duration, string? excludeReserveId = null)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM PaidCourtReservation
            WHERE p_reserve_name = @ReserveName
              AND p_reserve_date = @ReserveDate
              AND p_status IN ('booked', 'in_use')
              AND time(p_reserve_time) < time(@EndTime)
              AND time(p_reserve_time, '+' || p_reserve_duration || ' hours') > time(@StartTime)";

        if (!string.IsNullOrEmpty(excludeReserveId))
            sql += " AND p_reserve_id != @ExcludeId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveName", reserveName);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@StartTime", startTime.ToString(@"hh\:mm\:ss"));
        var endTime = startTime.Add(TimeSpan.FromHours(duration));
        cmd.Parameters.AddWithValue("@EndTime", endTime.ToString(@"hh\:mm\:ss"));

        if (!string.IsNullOrEmpty(excludeReserveId))
            cmd.Parameters.AddWithValue("@ExcludeId", excludeReserveId);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
        return count > 0;
    }

    /// <summary>
    /// Search reservations with filters
    /// </summary>
    public async Task<List<PaidCourtReservationItem>> SearchReservationsAsync(
        string? courtId = null,
        DateTime? reserveDate = null,
        string? reserveName = null,
        string? reservePhone = null)
    {
        var sql = @"
            SELECT 
                p_reserve_id, court_id, p_request_date, p_reserve_date, 
                p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status
            FROM PaidCourtReservation
            WHERE 1=1";

        var conditions = new List<string>();
        var parameters = new List<(string name, object value)>();

        if (!string.IsNullOrEmpty(courtId))
        {
            conditions.Add("AND court_id = @CourtId");
            parameters.Add(("@CourtId", courtId));
        }

        if (reserveDate.HasValue)
        {
            conditions.Add("AND p_reserve_date = @ReserveDate");
            parameters.Add(("@ReserveDate", reserveDate.Value.ToString("yyyy-MM-dd")));
        }

        if (!string.IsNullOrEmpty(reserveName))
        {
            conditions.Add("AND p_reserve_name LIKE @ReserveName");
            parameters.Add(("@ReserveName", $"%{reserveName}%"));
        }

        if (!string.IsNullOrEmpty(reservePhone))
        {
            conditions.Add("AND p_reserve_phone LIKE @ReservePhone");
            parameters.Add(("@ReservePhone", $"%{reservePhone}%"));
        }

        sql += " " + string.Join(" ", conditions);
        sql += " ORDER BY p_reserve_date DESC, p_reserve_time DESC";

        var reservations = new List<PaidCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReader(reader));
        }

        return reservations;
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

    /// <summary>
    /// Update existing reservation
    /// </summary>
    public async Task<bool> UpdateReservationAsync(PaidCourtReservationItem reservation)
    {
        const string sql = @"
            UPDATE PaidCourtReservation
            SET
                court_id = @CourtId,
                p_reserve_date = @ReserveDate,
                p_reserve_time = @ReserveTime,
                p_reserve_duration = @Duration,
                p_reserve_name = @ReserveName,
                p_reserve_phone = @ReservePhone,
                p_status = @Status
            WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reservation.ReserveId);
        cmd.Parameters.AddWithValue("@CourtId", reservation.CourtId);
        cmd.Parameters.AddWithValue("@ReserveDate", reservation.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ReserveTime", reservation.ReserveTime.ToString(@"hh\:mm\:ss")); // ✅ Fixed format
        cmd.Parameters.AddWithValue("@Duration", reservation.Duration);
        cmd.Parameters.AddWithValue("@ReserveName", reservation.ReserveName);
        cmd.Parameters.AddWithValue("@ReservePhone", string.IsNullOrEmpty(reservation.ReservePhone) ? DBNull.Value : reservation.ReservePhone);
        cmd.Parameters.AddWithValue("@Status", reservation.Status ?? "booked");

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    /// <summary>
    /// อัปเดตสถานะการจอง
    /// </summary>
    public async Task<bool> UpdateStatusAsync(string reserveId, string status)
    {
        const string sql = "UPDATE PaidCourtReservation SET p_status = @Status WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);
        cmd.Parameters.AddWithValue("@Status", status);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    // ========================================================================
    // DELETE
    // ========================================================================

    /// <summary>
    /// Delete reservation by ID
    /// </summary>
    public async Task<bool> DeleteReservationAsync(string reserveId)
    {
        const string sql = "DELETE FROM PaidCourtReservation WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete all reservations (for testing/reset)
    /// </summary>
    public async Task<bool> DeleteAllReservationsAsync()
    {
        const string sql = "DELETE FROM PaidCourtReservation";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Map SQLite DataReader to PaidCourtReservationItem
    /// </summary>
    private static PaidCourtReservationItem MapFromReader(SqliteDataReader reader)
    {
        var timeStr = reader.GetString(4);
        TimeSpan reserveTime = TimeSpan.Parse(timeStr);

        return new PaidCourtReservationItem
        {
            ReserveId = reader.GetString(0),
            CourtId = reader.GetString(1),
            RequestDate = DateTime.Parse(reader.GetString(2)),
            ReserveDate = DateTime.Parse(reader.GetString(3)),
            ReserveTime = reserveTime,
            Duration = reader.GetDouble(5),
            ReserveName = reader.GetString(6),
            ReservePhone = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Status = reader.IsDBNull(8) ? "booked" : reader.GetString(8)
        };
    }
}
