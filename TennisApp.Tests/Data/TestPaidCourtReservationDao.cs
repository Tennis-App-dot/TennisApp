using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Lightweight model สำหรับ test — แทน PaidCourtReservationItem (ไม่ต้องพึ่ง WinUI)
/// </summary>
public class TestPaidReservation
{
    public string ReserveId { get; set; } = "";
    public string CourtId { get; set; } = "";
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public DateTime ReserveDate { get; set; } = DateTime.Today;
    public TimeSpan ReserveTime { get; set; } = TimeSpan.FromHours(8);
    public double Duration { get; set; } = 1.0;
    public string ReserveName { get; set; } = "";
    public string ReservePhone { get; set; } = "";
    public string Status { get; set; } = "booked";
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int? ActualPrice { get; set; }
}

/// <summary>
/// Test DAO สำหรับ PaidCourtReservation — CRUD เต็มรูปแบบ
/// </summary>
public class TestPaidCourtReservationDao
{
    private readonly string _connectionString;

    public TestPaidCourtReservationDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
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

            CREATE INDEX IF NOT EXISTS IX_PaidRes_Court ON PaidCourtReservation(court_id);
            CREATE INDEX IF NOT EXISTS IX_PaidRes_Date ON PaidCourtReservation(p_reserve_date);
            CREATE INDEX IF NOT EXISTS IX_PaidRes_Status ON PaidCourtReservation(p_status);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> AddAsync(TestPaidReservation r)
    {
        const string sql = @"
            INSERT INTO PaidCourtReservation 
            (p_reserve_id, court_id, p_request_date, p_reserve_date, p_reserve_time, 
             p_reserve_duration, p_reserve_name, p_reserve_phone, p_status,
             p_actual_start, p_actual_end, p_actual_price)
            VALUES 
            (@id, @court, @reqDate, @resDate, @resTime, 
             @duration, @name, @phone, @status,
             @actualStart, @actualEnd, @actualPrice)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", r.ReserveId);
        cmd.Parameters.AddWithValue("@court", r.CourtId);
        cmd.Parameters.AddWithValue("@reqDate", r.RequestDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@resDate", r.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@resTime", r.ReserveTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@duration", r.Duration);
        cmd.Parameters.AddWithValue("@name", r.ReserveName);
        cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(r.ReservePhone) ? DBNull.Value : r.ReservePhone);
        cmd.Parameters.AddWithValue("@status", r.Status);
        cmd.Parameters.AddWithValue("@actualStart", r.ActualStart.HasValue ? r.ActualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualEnd", r.ActualEnd.HasValue ? r.ActualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualPrice", r.ActualPrice.HasValue ? r.ActualPrice.Value : DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<TestPaidReservation?> GetByIdAsync(string reserveId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM PaidCourtReservation WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapFromReader(reader);

        return null;
    }

    public async Task<List<TestPaidReservation>> GetByDateAsync(DateTime date)
    {
        var list = new List<TestPaidReservation>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM PaidCourtReservation WHERE p_reserve_date = @date ORDER BY p_reserve_time";
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapFromReader(reader));

        return list;
    }

    public async Task<bool> UpdateStatusAsync(string reserveId, string status, DateTime? actualStart = null, DateTime? actualEnd = null, int? actualPrice = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE PaidCourtReservation 
            SET p_status = @status,
                p_actual_start = @actualStart,
                p_actual_end = @actualEnd,
                p_actual_price = @actualPrice
            WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@actualStart", actualStart.HasValue ? actualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualEnd", actualEnd.HasValue ? actualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualPrice", actualPrice.HasValue ? actualPrice.Value : DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateDurationAsync(string reserveId, double newDuration)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE PaidCourtReservation SET p_reserve_duration = @duration WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);
        cmd.Parameters.AddWithValue("@duration", newDuration);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> CountByDateAndStatusAsync(DateTime date, string courtId, string status)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM PaidCourtReservation 
            WHERE p_reserve_date = @date AND court_id = @court AND p_status = @status";
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@court", courtId);
        cmd.Parameters.AddWithValue("@status", status);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> IsCourtAvailableAsync(string courtId, DateTime date, TimeSpan startTime, double duration)
    {
        var endTime = startTime.Add(TimeSpan.FromHours(duration));

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM PaidCourtReservation
            WHERE court_id = @court
              AND p_reserve_date = @date
              AND p_status IN ('booked', 'in_use')
              AND p_reserve_time < @endTime
              AND TIME(p_reserve_time, '+' || CAST(CAST(p_reserve_duration * 3600 AS INTEGER) AS TEXT) || ' seconds') > @startTime";
        cmd.Parameters.AddWithValue("@court", courtId);
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@startTime", startTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@endTime", endTime.ToString(@"hh\:mm\:ss"));

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count == 0;
    }

    public async Task<bool> DeleteAsync(string reserveId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PaidCourtReservation WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static TestPaidReservation MapFromReader(SqliteDataReader reader)
    {
        return new TestPaidReservation
        {
            ReserveId = reader.GetString(reader.GetOrdinal("p_reserve_id")),
            CourtId = reader.GetString(reader.GetOrdinal("court_id")),
            RequestDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("p_request_date"))),
            ReserveDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("p_reserve_date"))),
            ReserveTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("p_reserve_time"))),
            Duration = reader.GetDouble(reader.GetOrdinal("p_reserve_duration")),
            ReserveName = reader.GetString(reader.GetOrdinal("p_reserve_name")),
            ReservePhone = reader.IsDBNull(reader.GetOrdinal("p_reserve_phone")) ? "" : reader.GetString(reader.GetOrdinal("p_reserve_phone")),
            Status = reader.GetString(reader.GetOrdinal("p_status")),
            ActualStart = reader.IsDBNull(reader.GetOrdinal("p_actual_start")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("p_actual_start"))),
            ActualEnd = reader.IsDBNull(reader.GetOrdinal("p_actual_end")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("p_actual_end"))),
            ActualPrice = reader.IsDBNull(reader.GetOrdinal("p_actual_price")) ? null : reader.GetInt32(reader.GetOrdinal("p_actual_price"))
        };
    }
}
