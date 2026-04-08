using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Test DAO สำหรับ Court table — สร้างตารางและ CRUD สำหรับ test
/// </summary>
public class TestCourtDao
{
    private readonly string _connectionString;

    public TestCourtDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS Court (
                court_id         TEXT(2) PRIMARY KEY NOT NULL,
                court_img        BLOB NULL,
                court_status     TEXT(1) NOT NULL DEFAULT '1',
                maintenance_date DATE NULL,
                last_updated     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            INSERT OR IGNORE INTO Court (court_id, court_img, court_status, last_updated)
            VALUES ('00', NULL, '0', CURRENT_TIMESTAMP);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task AddCourtAsync(string courtId, string status = "1")
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Court (court_id, court_img, court_status, last_updated)
            VALUES (@id, NULL, @status, CURRENT_TIMESTAMP)";
        cmd.Parameters.AddWithValue("@id", courtId);
        cmd.Parameters.AddWithValue("@status", status);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<(string CourtId, string Status)>> GetCourtsByStatusAsync(string status)
    {
        var courts = new List<(string, string)>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT court_id, court_status FROM Court WHERE court_status = @status ORDER BY court_id";
        cmd.Parameters.AddWithValue("@status", status);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courts.Add((reader.GetString(0), reader.GetString(1)));
        }

        return courts;
    }
}
