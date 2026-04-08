using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Test DAO สำหรับ PaidCourtUseLog
/// </summary>
public class TestPaidCourtUseLogDao
{
    private readonly string _connectionString;

    public TestPaidCourtUseLogDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS PaidCourtUseLog (
                p_log_id           TEXT(10) PRIMARY KEY NOT NULL,
                p_reserve_id       TEXT(10) NOT NULL,
                p_checkin_time     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                p_log_duration     REAL NOT NULL,
                p_log_price        INTEGER NOT NULL DEFAULT 0,
                p_log_status       TEXT(20) NOT NULL DEFAULT 'completed',
                FOREIGN KEY (p_reserve_id) REFERENCES PaidCourtReservation(p_reserve_id) ON DELETE CASCADE
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<bool> InsertAsync(string logId, string reserveId, DateTime checkInTime, double duration, int price, string status = "completed")
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO PaidCourtUseLog (p_log_id, p_reserve_id, p_checkin_time, p_log_duration, p_log_price, p_log_status)
            VALUES (@logId, @resId, @checkIn, @duration, @price, @status)";
        cmd.Parameters.AddWithValue("@logId", logId);
        cmd.Parameters.AddWithValue("@resId", reserveId);
        cmd.Parameters.AddWithValue("@checkIn", checkInTime.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@duration", duration);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.AddWithValue("@status", status);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<int> CountAllAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM PaidCourtUseLog";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<(double Duration, int Price, string Status)?> GetByReserveIdAsync(string reserveId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT p_log_duration, p_log_price, p_log_status FROM PaidCourtUseLog WHERE p_reserve_id = @id";
        cmd.Parameters.AddWithValue("@id", reserveId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return (reader.GetDouble(0), reader.GetInt32(1), reader.GetString(2));

        return null;
    }
}
