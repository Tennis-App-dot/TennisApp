using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

public class ClassRegisRecordDao
{
    private readonly string _connectionString;

    public ClassRegisRecordDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ClassRegisRecord (
                trainee_id         TEXT(10) NOT NULL,
                class_id           TEXT(4) NOT NULL,
                regis_date         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (trainee_id, class_id),
                FOREIGN KEY (trainee_id) REFERENCES Trainee(trainee_id) ON DELETE CASCADE,
                FOREIGN KEY (class_id) REFERENCES Course(class_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Trainee ON ClassRegisRecord(trainee_id);
            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Class ON ClassRegisRecord(class_id);
            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Date ON ClassRegisRecord(regis_date);
        ";

        command.ExecuteNonQuery();
        System.Diagnostics.Debug.WriteLine("✅ ClassRegisRecord table initialized");
    }

    /// <summary>
    /// Get all registrations with trainee and course details
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> GetAllRegistrationsAsync()
    {
        var registrations = new List<ClassRegisRecordItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                r.trainee_id,
                r.class_id,
                r.regis_date,
                t.trainee_fname || ' ' || t.trainee_lname AS trainee_name,
                t.trainee_phone,
                c.class_title,
                c.class_time,
                c.class_rate,
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            ORDER BY r.regis_date DESC
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var registration = ClassRegisRecordItem.FromDatabase(
                traineeId: reader.GetString(0),
                classId: reader.GetString(1),
                regisDate: reader.GetDateTime(2),
                traineeName: reader.GetString(3),
                traineePhone: reader.IsDBNull(4) ? "" : reader.GetString(4),
                className: reader.GetString(5),
                classTime: reader.GetInt32(6),
                classRate: reader.GetInt32(7),
                trainerName: reader.IsDBNull(8) ? "" : reader.GetString(8)
            );
            registrations.Add(registration);
        }

        System.Diagnostics.Debug.WriteLine($"📋 Loaded {registrations.Count} registrations from database");
        return registrations;
    }

    /// <summary>
    /// Get registrations by trainee ID
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> GetRegistrationsByTraineeIdAsync(string traineeId)
    {
        var registrations = new List<ClassRegisRecordItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                r.trainee_id,
                r.class_id,
                r.regis_date,
                t.trainee_fname || ' ' || t.trainee_lname AS trainee_name,
                t.trainee_phone,
                c.class_title,
                c.class_time,
                c.class_rate,
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE r.trainee_id = @trainee_id
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var registration = ClassRegisRecordItem.FromDatabase(
                traineeId: reader.GetString(0),
                classId: reader.GetString(1),
                regisDate: reader.GetDateTime(2),
                traineeName: reader.GetString(3),
                traineePhone: reader.IsDBNull(4) ? "" : reader.GetString(4),
                className: reader.GetString(5),
                classTime: reader.GetInt32(6),
                classRate: reader.GetInt32(7),
                trainerName: reader.IsDBNull(8) ? "" : reader.GetString(8)
            );
            registrations.Add(registration);
        }

        return registrations;
    }

    /// <summary>
    /// Get registrations by course ID
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> GetRegistrationsByCourseIdAsync(string classId)
    {
        var registrations = new List<ClassRegisRecordItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                r.trainee_id,
                r.class_id,
                r.regis_date,
                t.trainee_fname || ' ' || t.trainee_lname AS trainee_name,
                t.trainee_phone,
                c.class_title,
                c.class_time,
                c.class_rate,
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE r.class_id = @class_id
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@class_id", classId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var registration = ClassRegisRecordItem.FromDatabase(
                traineeId: reader.GetString(0),
                classId: reader.GetString(1),
                regisDate: reader.GetDateTime(2),
                traineeName: reader.GetString(3),
                traineePhone: reader.IsDBNull(4) ? "" : reader.GetString(4),
                className: reader.GetString(5),
                classTime: reader.GetInt32(6),
                classRate: reader.GetInt32(7),
                trainerName: reader.IsDBNull(8) ? "" : reader.GetString(8)
            );
            registrations.Add(registration);
        }

        return registrations;
    }

    /// <summary>
    /// Add new course registration
    /// </summary>
    public async Task<bool> AddRegistrationAsync(ClassRegisRecordItem registration)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ClassRegisRecord (trainee_id, class_id, regis_date)
            VALUES (@trainee_id, @class_id, @regis_date)
        ";

        command.Parameters.AddWithValue("@trainee_id", registration.TraineeId);
        command.Parameters.AddWithValue("@class_id", registration.ClassId);
        command.Parameters.AddWithValue("@regis_date", registration.RegisDate);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Added registration: {registration.TraineeId} -> {registration.ClassId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding registration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if registration exists
    /// </summary>
    public async Task<bool> RegistrationExistsAsync(string traineeId, string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM ClassRegisRecord 
            WHERE trainee_id = @trainee_id AND class_id = @class_id
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);
        command.Parameters.AddWithValue("@class_id", classId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /// <summary>
    /// Delete registration
    /// </summary>
    public async Task<bool> DeleteRegistrationAsync(string traineeId, string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM ClassRegisRecord 
            WHERE trainee_id = @trainee_id AND class_id = @class_id
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);
        command.Parameters.AddWithValue("@class_id", classId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Deleted registration: {traineeId} -> {classId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting registration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get total registration count
    /// </summary>
    public async Task<int> GetRegistrationCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM ClassRegisRecord";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count;
    }

    /// <summary>
    /// Search registrations by keyword
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> SearchRegistrationsAsync(string keyword)
    {
        var registrations = new List<ClassRegisRecordItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                r.trainee_id,
                r.class_id,
                r.regis_date,
                t.trainee_fname || ' ' || t.trainee_lname AS trainee_name,
                t.trainee_phone,
                c.class_title,
                c.class_time,
                c.class_rate,
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE 
                r.trainee_id LIKE @keyword OR
                t.trainee_fname LIKE @keyword OR
                t.trainee_lname LIKE @keyword OR
                r.class_id LIKE @keyword OR
                c.class_title LIKE @keyword
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var registration = ClassRegisRecordItem.FromDatabase(
                traineeId: reader.GetString(0),
                classId: reader.GetString(1),
                regisDate: reader.GetDateTime(2),
                traineeName: reader.GetString(3),
                traineePhone: reader.IsDBNull(4) ? "" : reader.GetString(4),
                className: reader.GetString(5),
                classTime: reader.GetInt32(6),
                classRate: reader.GetInt32(7),
                trainerName: reader.IsDBNull(8) ? "" : reader.GetString(8)
            );
            registrations.Add(registration);
        }

        return registrations;
    }
}
