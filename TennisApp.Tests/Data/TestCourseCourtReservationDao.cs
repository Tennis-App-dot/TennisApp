using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Lightweight model สำหรับ test — แทน CourseCourtReservationItem
/// </summary>
public class TestCourseReservation
{
    public string ReserveId { get; set; } = "";
    public string CourtId { get; set; } = "";
    public string ClassId { get; set; } = "";
    public string TrainerId { get; set; } = "";
    public DateTime RequestDate { get; set; } = DateTime.Now;
    public DateTime ReserveDate { get; set; } = DateTime.Today;
    public TimeSpan ReserveTime { get; set; } = TimeSpan.FromHours(8);
    public double Duration { get; set; } = 1.0;
    public string ReserveName { get; set; } = "";
    public string ReservePhone { get; set; } = "";
    public string Status { get; set; } = "booked";
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
}

/// <summary>
/// Test DAO สำหรับ CourseCourtReservation
/// </summary>
public class TestCourseCourtReservationDao
{
    private readonly string _connectionString;

    public TestCourseCourtReservationDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS CourseCourtReservation (
                c_reserve_id       TEXT(10) PRIMARY KEY NOT NULL,
                court_id           TEXT(2) NOT NULL,
                class_id           TEXT(4) NOT NULL,
                trainer_id         TEXT(10) NOT NULL,
                c_request_date     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                c_reserve_date     DATE NOT NULL,
                c_reserve_time     TIME NOT NULL,
                c_reserve_duration REAL NOT NULL DEFAULT 1.0,
                c_reserve_name     TEXT(50) NOT NULL,
                c_reserve_phone    TEXT(10) NULL,
                c_status           TEXT(20) NOT NULL DEFAULT 'booked',
                c_actual_start     DATETIME NULL,
                c_actual_end       DATETIME NULL
            );

            CREATE INDEX IF NOT EXISTS IX_CourseRes_Court ON CourseCourtReservation(court_id);
            CREATE INDEX IF NOT EXISTS IX_CourseRes_Date ON CourseCourtReservation(c_reserve_date);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> AddAsync(TestCourseReservation r)
    {
        const string sql = @"
            INSERT INTO CourseCourtReservation 
            (c_reserve_id, court_id, class_id, trainer_id, c_request_date, c_reserve_date, 
             c_reserve_time, c_reserve_duration, c_reserve_name, c_reserve_phone, c_status,
             c_actual_start, c_actual_end)
            VALUES 
            (@id, @court, @classId, @trainerId, @reqDate, @resDate, 
             @resTime, @duration, @name, @phone, @status,
             @actualStart, @actualEnd)";

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", r.ReserveId);
        cmd.Parameters.AddWithValue("@court", r.CourtId);
        cmd.Parameters.AddWithValue("@classId", r.ClassId);
        cmd.Parameters.AddWithValue("@trainerId", r.TrainerId);
        cmd.Parameters.AddWithValue("@reqDate", r.RequestDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@resDate", r.ReserveDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@resTime", r.ReserveTime.ToString(@"hh\:mm\:ss"));
        cmd.Parameters.AddWithValue("@duration", r.Duration);
        cmd.Parameters.AddWithValue("@name", r.ReserveName);
        cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(r.ReservePhone) ? DBNull.Value : r.ReservePhone);
        cmd.Parameters.AddWithValue("@status", r.Status);
        cmd.Parameters.AddWithValue("@actualStart", r.ActualStart.HasValue ? r.ActualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualEnd", r.ActualEnd.HasValue ? r.ActualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<TestCourseReservation?> GetByIdAsync(string reserveId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM CourseCourtReservation WHERE c_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapFromReader(reader);

        return null;
    }

    public async Task<List<TestCourseReservation>> GetByDateAsync(DateTime date)
    {
        var list = new List<TestCourseReservation>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM CourseCourtReservation WHERE c_reserve_date = @date ORDER BY c_reserve_time";
        cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapFromReader(reader));

        return list;
    }

    public async Task<bool> UpdateStatusAsync(string reserveId, string status, DateTime? actualStart = null, DateTime? actualEnd = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE CourseCourtReservation 
            SET c_status = @status,
                c_actual_start = @actualStart,
                c_actual_end = @actualEnd
            WHERE c_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@actualStart", actualStart.HasValue ? actualStart.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.Parameters.AddWithValue("@actualEnd", actualEnd.HasValue ? actualEnd.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static TestCourseReservation MapFromReader(SqliteDataReader reader)
    {
        return new TestCourseReservation
        {
            ReserveId = reader.GetString(reader.GetOrdinal("c_reserve_id")),
            CourtId = reader.GetString(reader.GetOrdinal("court_id")),
            ClassId = reader.GetString(reader.GetOrdinal("class_id")),
            TrainerId = reader.GetString(reader.GetOrdinal("trainer_id")),
            RequestDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("c_request_date"))),
            ReserveDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("c_reserve_date"))),
            ReserveTime = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("c_reserve_time"))),
            Duration = reader.GetDouble(reader.GetOrdinal("c_reserve_duration")),
            ReserveName = reader.GetString(reader.GetOrdinal("c_reserve_name")),
            ReservePhone = reader.IsDBNull(reader.GetOrdinal("c_reserve_phone")) ? "" : reader.GetString(reader.GetOrdinal("c_reserve_phone")),
            Status = reader.GetString(reader.GetOrdinal("c_status")),
            ActualStart = reader.IsDBNull(reader.GetOrdinal("c_actual_start")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("c_actual_start"))),
            ActualEnd = reader.IsDBNull(reader.GetOrdinal("c_actual_end")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("c_actual_end")))
        };
    }
}
