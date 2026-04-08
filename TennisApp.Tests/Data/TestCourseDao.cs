using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace TennisApp.Tests.Data;

/// <summary>
/// Test DAO สำหรับ Trainer + Course tables
/// </summary>
public class TestCourseDao
{
    private readonly string _connectionString;

    public TestCourseDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeTables();
    }

    private void InitializeTables()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Trainer (
                trainer_id     TEXT(10) PRIMARY KEY NOT NULL,
                trainer_fname  TEXT(50) NOT NULL,
                trainer_lname  TEXT(50) NULL,
                trainer_phone  TEXT(10) NULL,
                trainer_img    BLOB NULL,
                last_updated   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS CourseType (
                ct_id   TEXT(2) PRIMARY KEY NOT NULL,
                ct_name TEXT(50) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Course (
                class_id       TEXT(4) NOT NULL,
                trainer_id     TEXT(10) NOT NULL,
                class_title    TEXT(100) NOT NULL,
                class_time     INTEGER NOT NULL DEFAULT 1,
                class_duration INTEGER NOT NULL DEFAULT 1,
                class_rate     INTEGER NOT NULL DEFAULT 0,
                last_updated   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (class_id, trainer_id),
                FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE CASCADE
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task AddTrainerAsync(string trainerId, string firstName, string? phone = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Trainer (trainer_id, trainer_fname, trainer_lname, trainer_phone, last_updated)
            VALUES (@id, @fname, '', @phone, CURRENT_TIMESTAMP)";
        cmd.Parameters.AddWithValue("@id", trainerId);
        cmd.Parameters.AddWithValue("@fname", firstName);
        cmd.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddCourseAsync(string classId, string trainerId, string classTitle, int classTime = 4, int classDuration = 1, int classRate = 2200)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Course (class_id, trainer_id, class_title, class_time, class_duration, class_rate, last_updated)
            VALUES (@classId, @trainerId, @title, @time, @duration, @rate, CURRENT_TIMESTAMP)";
        cmd.Parameters.AddWithValue("@classId", classId);
        cmd.Parameters.AddWithValue("@trainerId", trainerId);
        cmd.Parameters.AddWithValue("@title", classTitle);
        cmd.Parameters.AddWithValue("@time", classTime);
        cmd.Parameters.AddWithValue("@duration", classDuration);
        cmd.Parameters.AddWithValue("@rate", classRate);
        await cmd.ExecuteNonQueryAsync();
    }
}
