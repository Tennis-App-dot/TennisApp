using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Test DAO สำหรับ CourseCourtUseLog
/// </summary>
public class TestCourseCourtUseLogDao
{
    private readonly string _connectionString;

    public TestCourseCourtUseLogDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS CourseCourtUseLog (
                c_log_id           TEXT(10) PRIMARY KEY NOT NULL,
                c_reserve_id       TEXT(10) NOT NULL,
                c_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                c_log_duration     REAL NOT NULL,
                c_log_status       TEXT(20) NOT NULL DEFAULT 'completed',
                FOREIGN KEY (c_reserve_id) REFERENCES CourseCourtReservation(c_reserve_id) ON DELETE CASCADE
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> InsertAsync(string logId, string reserveId, DateTime checkInTime, double duration, string status = "completed")
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO CourseCourtUseLog (c_log_id, c_reserve_id, c_checkin_time, c_log_duration, c_log_status)
            VALUES (@logId, @resId, @checkIn, @duration, @status)";
        cmd.Parameters.AddWithValue("@logId", logId);
        cmd.Parameters.AddWithValue("@resId", reserveId);
        cmd.Parameters.AddWithValue("@checkIn", checkInTime.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@duration", duration);
        cmd.Parameters.AddWithValue("@status", status);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> CountAllAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CourseCourtUseLog";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<(double Duration, string Status)?> GetByReserveIdAsync(string reserveId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT c_log_duration, c_log_status FROM CourseCourtUseLog WHERE c_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return (reader.GetDouble(0), reader.GetString(1));

        return null;
    }
}
