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

        // ✅ Enable Foreign Key enforcement
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
        pragmaCommand.ExecuteNonQuery();

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
                p_status           TEXT(20) NOT NULL DEFAULT 'booked',
                p_actual_start     DATETIME NULL,
                p_actual_end       DATETIME NULL,
                p_actual_price     INTEGER NULL
            );
        ";
        createCommand.ExecuteNonQuery();

        // ✅ เพิ่มคอลัมน์ p_status ถ้ายังไม่มี (สำหรับ DB เดิม)
        TryAddColumn(connection, "ALTER TABLE PaidCourtReservation ADD COLUMN p_status TEXT(20) NOT NULL DEFAULT 'booked'");

        // ✅ เพิ่มคอลัมน์ Start-Stop สำหรับบันทึกเวลาเข้า-ออกจริง
        TryAddColumn(connection, "ALTER TABLE PaidCourtReservation ADD COLUMN p_actual_start DATETIME NULL");
        TryAddColumn(connection, "ALTER TABLE PaidCourtReservation ADD COLUMN p_actual_end DATETIME NULL");
        TryAddColumn(connection, "ALTER TABLE PaidCourtReservation ADD COLUMN p_actual_price INTEGER NULL");

        // ✅ สร้าง indexes
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

    private static void TryAddColumn(SqliteConnection connection, string alterSql)
    {
        try
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = alterSql;
            cmd.ExecuteNonQuery();
        }
        catch { /* คอลัมน์มีอยู่แล้ว */ }
    }

    /// <summary>
    /// ตรวจสอบและสร้างสนาม dummy "00" ถ้ายังไม่มี
    /// </summary>
    private void EnsureDummyCourtExists(SqliteConnection connection)
    {
        try
        {
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

    public async Task<bool> AddReservationAsync(PaidCourtReservationItem reservation)
    {
        const string sql = @"
            INSERT INTO PaidCourtReservation 
            (p_reserve_id, court_id, p_request_date, p_reserve_date, p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status, p_actual_start, p_actual_end, p_actual_price)
            VALUES 
            (@ReserveId, @CourtId, @RequestDate, @ReserveDate, @ReserveTime, @Duration, @ReserveName, @ReservePhone, @Status, @ActualStart, @ActualEnd, @ActualPrice)";

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
        cmd.Parameters.AddWithValue("@ActualStart", reservation.ActualStart.HasValue ? reservation.ActualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@ActualEnd", reservation.ActualEnd.HasValue ? reservation.ActualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@ActualPrice", reservation.ActualPrice.HasValue ? reservation.ActualPrice.Value : DBNull.Value);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    // ========================================================================
    // READ
    // ========================================================================

    private const string SelectColumns = @"
        SELECT 
            p_reserve_id, court_id, p_request_date, p_reserve_date, 
            p_reserve_time, p_reserve_duration, p_reserve_name, p_reserve_phone, p_status,
            p_actual_start, p_actual_end, p_actual_price
        FROM PaidCourtReservation";

    public async Task<List<PaidCourtReservationItem>> GetAllReservationsAsync()
    {
        var sql = SelectColumns + " ORDER BY p_reserve_date DESC, p_reserve_time DESC";
        return await ExecuteListQueryAsync(sql);
    }

    public async Task<PaidCourtReservationItem?> GetReservationByIdAsync(string reserveId)
    {
        var sql = SelectColumns + " WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        return await reader.ReadAsync().ConfigureAwait(false) ? MapFromReader(reader) : null;
    }

    public async Task<List<PaidCourtReservationItem>> GetReservationsByCourtAsync(string courtId)
    {
        var sql = SelectColumns + " WHERE court_id = @CourtId ORDER BY p_reserve_date DESC, p_reserve_time DESC";
        return await ExecuteListQueryAsync(sql, ("@CourtId", courtId));
    }

    public async Task<List<PaidCourtReservationItem>> GetReservationsByDateAsync(DateTime reserveDate)
    {
        var sql = SelectColumns + " WHERE p_reserve_date = @ReserveDate ORDER BY p_reserve_time";
        return await ExecuteListQueryAsync(sql, ("@ReserveDate", reserveDate.ToString("yyyy-MM-dd")));
    }

    public async Task<List<PaidCourtReservationItem>> GetReservationsByRequestDateAsync(DateTime requestDate)
    {
        var sql = SelectColumns + " WHERE date(p_request_date) = @RequestDate ORDER BY p_reserve_id";
        return await ExecuteListQueryAsync(sql, ("@RequestDate", requestDate.ToString("yyyy-MM-dd")));
    }

    public async Task<bool> IsCourtAvailableAsync(string courtId, DateTime reserveDate, TimeSpan startTime, double duration)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM PaidCourtReservation
            WHERE court_id = @CourtId
              AND p_reserve_date = @ReserveDate
              AND p_status IN ('booked', 'in_use')
              AND time(p_reserve_time) < time(@EndTime)
              AND time(p_reserve_time, '+' || p_reserve_duration || ' hours') > time(@StartTime)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CourtId", courtId);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@StartTime", startTime.ToString(@"hh\:mm\:ss"));
        var endTime = startTime.Add(TimeSpan.FromHours(duration));
        cmd.Parameters.AddWithValue("@EndTime", endTime.ToString(@"hh\:mm\:ss"));

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
        return count == 0;
    }

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

    public async Task<List<PaidCourtReservationItem>> SearchReservationsAsync(
        string? courtId = null, DateTime? reserveDate = null,
        string? reserveName = null, string? reservePhone = null)
    {
        var sql = SelectColumns + " WHERE 1=1";
        var parameters = new List<(string name, object value)>();

        if (!string.IsNullOrEmpty(courtId))
        {
            sql += " AND court_id = @CourtId";
            parameters.Add(("@CourtId", courtId));
        }
        if (reserveDate.HasValue)
        {
            sql += " AND p_reserve_date = @ReserveDate";
            parameters.Add(("@ReserveDate", reserveDate.Value.ToString("yyyy-MM-dd")));
        }
        if (!string.IsNullOrEmpty(reserveName))
        {
            sql += " AND p_reserve_name LIKE @ReserveName";
            parameters.Add(("@ReserveName", $"%{reserveName}%"));
        }
        if (!string.IsNullOrEmpty(reservePhone))
        {
            sql += " AND p_reserve_phone LIKE @ReservePhone";
            parameters.Add(("@ReservePhone", $"%{reservePhone}%"));
        }

        sql += " ORDER BY p_reserve_date DESC, p_reserve_time DESC";
        return await ExecuteListQueryAsync(sql, [.. parameters]);
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

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
                p_status = @Status,
                p_actual_start = @ActualStart,
                p_actual_end = @ActualEnd,
                p_actual_price = @ActualPrice
            WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reservation.ReserveId);
        cmd.Parameters.AddWithValue("@CourtId", reservation.CourtId);
        cmd.Parameters.AddWithValue("@ReserveDate", reservation.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ReserveTime", reservation.ReserveTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@Duration", reservation.Duration);
        cmd.Parameters.AddWithValue("@ReserveName", reservation.ReserveName);
        cmd.Parameters.AddWithValue("@ReservePhone", string.IsNullOrEmpty(reservation.ReservePhone) ? DBNull.Value : reservation.ReservePhone);
        cmd.Parameters.AddWithValue("@Status", reservation.Status ?? "booked");
        cmd.Parameters.AddWithValue("@ActualStart", reservation.ActualStart.HasValue ? reservation.ActualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@ActualEnd", reservation.ActualEnd.HasValue ? reservation.ActualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@ActualPrice", reservation.ActualPrice.HasValue ? reservation.ActualPrice.Value : DBNull.Value);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

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

    public async Task<bool> DeleteReservationAsync(string reserveId)
    {
        const string sql = "DELETE FROM PaidCourtReservation WHERE p_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON", connection);
        await pragmaCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAllReservationsAsync()
    {
        const string sql = "DELETE FROM PaidCourtReservation";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON", connection);
        await pragmaCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    private async Task<List<PaidCourtReservationItem>> ExecuteListQueryAsync(string sql, params (string name, object value)[] parameters)
    {
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
            Status = reader.IsDBNull(8) ? "booked" : reader.GetString(8),
            ActualStart = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
            ActualEnd = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10)),
            ActualPrice = reader.IsDBNull(11) ? null : reader.GetInt32(11)
        };
    }
}
