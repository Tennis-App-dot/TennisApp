using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// Data Access Object for CourseCourtReservation table
/// Handles all database operations for course court reservations (การจองสนามสำหรับคอร์ส)
/// </summary>
public class CourseCourtReservationDao
{
    private readonly string _connectionString;

    public CourseCourtReservationDao(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        InitializeTable();
    }

    /// <summary>
    /// สร้างตาราง CourseCourtReservation ถ้ายังไม่มี
    /// </summary>
    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // ✅ สร้างสนาม dummy "00" ก่อน (ถ้ายังไม่มี)
        EnsureDummyCourtExists(connection);

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS CourseCourtReservation (
                c_reserve_id       TEXT(10) PRIMARY KEY NOT NULL,
                court_id           TEXT(2) NOT NULL,
                class_id           TEXT(8) NOT NULL,
                c_request_date     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                c_reserve_date     DATE NOT NULL,
                c_reserve_time     TIME NOT NULL,
                c_reserve_duration REAL NOT NULL,
                c_reserve_name     TEXT(50) NOT NULL,
                c_reserve_phone    TEXT(10) NULL,
                c_status           TEXT(20) NOT NULL DEFAULT 'booked'
            );
        ";
        createCommand.ExecuteNonQuery();

        // ✅ เพิ่มคอลัมน์ c_status ถ้ายังไม่มี (สำหรับ DB เดิม)
        try
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE CourseCourtReservation ADD COLUMN c_status TEXT(20) NOT NULL DEFAULT 'booked'";
            alterCommand.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine("✅ CourseCourtReservation: เพิ่มคอลัมน์ c_status สำเร็จ");
        }
        catch
        {
            // คอลัมน์มีอยู่แล้ว → ข้ามไป
        }

        // ✅ สร้าง indexes หลังจากมั่นใจว่าคอลัมน์ c_status มีแล้ว
        var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = @"
            CREATE INDEX IF NOT EXISTS IX_CourseCourtReservation_Court ON CourseCourtReservation(court_id);
            CREATE INDEX IF NOT EXISTS IX_CourseCourtReservation_Class ON CourseCourtReservation(class_id);
            CREATE INDEX IF NOT EXISTS IX_CourseCourtReservation_Date ON CourseCourtReservation(c_reserve_date);
            CREATE INDEX IF NOT EXISTS IX_CourseCourtReservation_Request ON CourseCourtReservation(c_request_date);
            CREATE INDEX IF NOT EXISTS IX_CourseCourtReservation_Status ON CourseCourtReservation(c_status);
        ";
        indexCommand.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine("✅ CourseCourtReservation table initialized");
    }

    /// <summary>
    /// ตรวจสอบและสร้างสนาม dummy "00" ถ้ายังไม่มี
    /// </summary>
    private void EnsureDummyCourtExists(SqliteConnection connection)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔍 CourseReservationDao: ตรวจสอบสนาม dummy '00'...");
            
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
                
                System.Diagnostics.Debug.WriteLine("✅ CourseReservationDao: สร้างสนาม dummy '00' สำเร็จ");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ CourseReservationDao: สนาม dummy '00' มีอยู่แล้ว");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ CourseReservationDao: ไม่สามารถสร้างสนาม dummy: {ex.Message}");
        }
    }

    // ========================================================================
    // CREATE
    // ========================================================================

    /// <summary>
    /// Add a new course court reservation
    /// </summary>
    public async Task<bool> AddReservationAsync(CourseCourtReservationItem reservation)
    {
        const string sql = @"
            INSERT INTO CourseCourtReservation 
            (c_reserve_id, court_id, class_id, c_request_date, c_reserve_date, c_reserve_time, c_reserve_duration, c_reserve_name, c_reserve_phone, c_status)
            VALUES 
            (@ReserveId, @CourtId, @ClassId, @RequestDate, @ReserveDate, @ReserveTime, @Duration, @ReserveName, @ReservePhone, @Status)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reservation.ReserveId);
        cmd.Parameters.AddWithValue("@CourtId", reservation.CourtId);
        cmd.Parameters.AddWithValue("@ClassId", reservation.ClassId);
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
    /// Get all course court reservations with course details
    /// </summary>
    public async Task<List<CourseCourtReservationItem>> GetAllReservationsAsync()
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id, r.court_id, r.class_id, r.c_request_date,
                r.c_reserve_date, r.c_reserve_time, r.c_reserve_name, r.c_reserve_phone,
                co.class_title, co.class_duration, r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            ORDER BY r.c_reserve_date DESC, r.c_reserve_time DESC";

        var reservations = new List<CourseCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservation by ID
    /// </summary>
    public async Task<CourseCourtReservationItem?> GetReservationByIdAsync(string reserveId)
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id, r.court_id, r.class_id, r.c_request_date,
                r.c_reserve_date, r.c_reserve_time, r.c_reserve_name, r.c_reserve_phone,
                co.class_title, co.class_duration, r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE r.c_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (await reader.ReadAsync().ConfigureAwait(false))
        {
            return MapFromReaderWithCourseInfo(reader);
        }

        return null;
    }

    /// <summary>
    /// Get reservations by court ID
    /// </summary>
    public async Task<List<CourseCourtReservationItem>> GetReservationsByCourtAsync(string courtId)
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id,
                r.court_id,
                r.class_id,
                r.c_request_date,
                r.c_reserve_date,
                r.c_reserve_time,
                r.c_reserve_name,
                r.c_reserve_phone,
                co.class_title,
                co.class_duration,
                r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE r.court_id = @CourtId
            ORDER BY r.c_reserve_date DESC, r.c_reserve_time DESC";

        var reservations = new List<CourseCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CourtId", courtId);

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservations by class ID (course)
    /// </summary>
    public async Task<List<CourseCourtReservationItem>> GetReservationsByClassAsync(string classId)
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id,
                r.court_id,
                r.class_id,
                r.c_request_date,
                r.c_reserve_date,
                r.c_reserve_time,
                r.c_reserve_name,
                r.c_reserve_phone,
                co.class_title,
                co.class_duration,
                r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE r.class_id = @ClassId
            ORDER BY r.c_reserve_date DESC, r.c_reserve_time DESC";

        var reservations = new List<CourseCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ClassId", classId);

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservations by reserve date (วันที่เรียน)
    /// </summary>
    public async Task<List<CourseCourtReservationItem>> GetReservationsByDateAsync(DateTime reserveDate)
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id,
                r.court_id,
                r.class_id,
                r.c_request_date,
                r.c_reserve_date,
                r.c_reserve_time,
                r.c_reserve_name,
                r.c_reserve_phone,
                co.class_title,
                co.class_duration,
                r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE r.c_reserve_date = @ReserveDate
            ORDER BY r.c_reserve_time";

        var reservations = new List<CourseCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Get reservations by request date (วันที่ติดต่อมาจอง)
    /// Used by ReservationIdGenerator to count daily bookings
    /// </summary>
    public async Task<List<CourseCourtReservationItem>> GetReservationsByRequestDateAsync(DateTime requestDate)
    {
        const string sql = @"
            SELECT 
                r.c_reserve_id,
                r.court_id,
                r.class_id,
                r.c_request_date,
                r.c_reserve_date,
                r.c_reserve_time,
                r.c_reserve_name,
                r.c_reserve_phone,
                co.class_title,
                co.class_duration,
                r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE date(r.c_request_date) = @RequestDate
            ORDER BY r.c_reserve_id";

        var reservations = new List<CourseCourtReservationItem>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@RequestDate", requestDate.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    /// <summary>
    /// Check if court is available at specific time (for course reservations)
    /// Returns true if court is available (no overlapping reservations)
    /// </summary>
    public async Task<bool> IsCourtAvailableAsync(string courtId, DateTime reserveDate, TimeSpan startTime, double duration)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE r.court_id = @CourtId
              AND r.c_reserve_date = @ReserveDate
              AND time(r.c_reserve_time) < time(@EndTime)
              AND time(r.c_reserve_time, '+' || co.class_duration || ' hours') > time(@StartTime)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@CourtId", courtId);
        cmd.Parameters.AddWithValue("@ReserveDate", reserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@StartTime", startTime.ToString(@"hh\:mm\:ss")); // ✅ Fixed
        
        var endTime = startTime.Add(TimeSpan.FromHours(duration));
        cmd.Parameters.AddWithValue("@EndTime", endTime.ToString(@"hh\:mm\:ss")); // ✅ Fixed

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync().ConfigureAwait(false));
        return count == 0; // Available if no overlapping reservations
    }

    /// <summary>
    /// ตรวจสอบว่ามีการจองคอร์สซ้ำหรือไม่ (ชื่อเดียวกัน + วันเดียวกัน + เวลาซ้อนทับ)
    /// </summary>
    public async Task<bool> HasDuplicateReservationAsync(string reserveName, DateTime reserveDate, TimeSpan startTime, double duration, string? excludeReserveId = null)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM CourseCourtReservation
            WHERE c_reserve_name = @ReserveName
              AND c_reserve_date = @ReserveDate
              AND c_status IN ('booked', 'in_use')
              AND time(c_reserve_time) < time(@EndTime)
              AND time(c_reserve_time, '+' || c_reserve_duration || ' hours') > time(@StartTime)";

        if (!string.IsNullOrEmpty(excludeReserveId))
            sql += " AND c_reserve_id != @ExcludeId";

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
    public async Task<List<CourseCourtReservationItem>> SearchReservationsAsync(
        string? courtId = null,
        string? classId = null,
        DateTime? reserveDate = null,
        string? reserveName = null)
    {
        var sql = @"
            SELECT 
                r.c_reserve_id,
                r.court_id,
                r.class_id,
                r.c_request_date,
                r.c_reserve_date,
                r.c_reserve_time,
                r.c_reserve_name,
                r.c_reserve_phone,
                co.class_title,
                co.class_duration,
                r.c_status
            FROM CourseCourtReservation r
            INNER JOIN Course co ON r.class_id = co.class_id
            WHERE 1=1";

        var conditions = new List<string>();
        var parameters = new List<(string name, object value)>();

        if (!string.IsNullOrEmpty(courtId))
        {
            conditions.Add("AND r.court_id = @CourtId");
            parameters.Add(("@CourtId", courtId));
        }

        if (!string.IsNullOrEmpty(classId))
        {
            conditions.Add("AND r.class_id = @ClassId");
            parameters.Add(("@ClassId", classId));
        }

        if (reserveDate.HasValue)
        {
            conditions.Add("AND r.c_reserve_date = @ReserveDate");
            parameters.Add(("@ReserveDate", reserveDate.Value.ToString("yyyy-MM-dd")));
        }

        if (!string.IsNullOrEmpty(reserveName))
        {
            conditions.Add("AND r.c_reserve_name LIKE @ReserveName");
            parameters.Add(("@ReserveName", $"%{reserveName}%"));
        }

        sql += " " + string.Join(" ", conditions);
        sql += " ORDER BY r.c_reserve_date DESC, r.c_reserve_time DESC";

        var reservations = new List<CourseCourtReservationItem>();

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
            reservations.Add(MapFromReaderWithCourseInfo(reader));
        }

        return reservations;
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

    /// <summary>
    /// Update existing course reservation
    /// </summary>
    public async Task<bool> UpdateReservationAsync(CourseCourtReservationItem reservation)
    {
        const string sql = @"
            UPDATE CourseCourtReservation
            SET
                court_id = @CourtId,
                class_id = @ClassId,
                c_reserve_date = @ReserveDate,
                c_reserve_time = @ReserveTime,
                c_reserve_duration = @Duration,
                c_reserve_name = @ReserveName,
                c_reserve_phone = @ReservePhone,
                c_status = @Status
            WHERE c_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reservation.ReserveId);
        cmd.Parameters.AddWithValue("@CourtId", reservation.CourtId);
        cmd.Parameters.AddWithValue("@ClassId", reservation.ClassId);
        cmd.Parameters.AddWithValue("@ReserveDate", reservation.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@ReserveTime", reservation.ReserveTime.ToString(@"hh\:mm\:ss"));
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
        const string sql = "UPDATE CourseCourtReservation SET c_status = @Status WHERE c_reserve_id = @ReserveId";

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
        const string sql = "DELETE FROM CourseCourtReservation WHERE c_reserve_id = @ReserveId";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ReserveId", reserveId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete all course reservations (for testing/reset)
    /// </summary>
    public async Task<bool> DeleteAllReservationsAsync()
    {
        const string sql = "DELETE FROM CourseCourtReservation";

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
    /// Map SQLite DataReader to CourseCourtReservationItem (with Course info)
    /// </summary>
    private static CourseCourtReservationItem MapFromReaderWithCourseInfo(SqliteDataReader reader)
    {
        // Parse reserve_time (HH:MM format)
        var timeStr = reader.GetString(5); // c_reserve_time
        TimeSpan reserveTime = TimeSpan.Parse(timeStr);

        return new CourseCourtReservationItem
        {
            ReserveId = reader.GetString(0),        // c_reserve_id
            CourtId = reader.GetString(1),          // court_id
            ClassId = reader.GetString(2),          // class_id
            RequestDate = DateTime.Parse(reader.GetString(3)), // c_request_date
            ReserveDate = DateTime.Parse(reader.GetString(4)), // c_reserve_date
            ReserveTime = reserveTime,              // c_reserve_time
            ReserveName = reader.GetString(6),      // c_reserve_name
            ReservePhone = reader.IsDBNull(7) ? string.Empty : reader.GetString(7), // c_reserve_phone
            ClassTitle = reader.GetString(8),       // class_title (from Course)
            ClassDuration = reader.GetInt32(9),      // class_duration (from Course)
            Status = reader.IsDBNull(10) ? "booked" : reader.GetString(10)
        };
    }
}
