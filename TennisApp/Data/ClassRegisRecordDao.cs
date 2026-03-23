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

        // ✅ Enable Foreign Key enforcement
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
        pragmaCommand.ExecuteNonQuery();

        // ─── ตรวจสอบว่าตาราง ClassRegisRecord มีอยู่หรือไม่ ───
        var checkTable = connection.CreateCommand();
        checkTable.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ClassRegisRecord'";
        var hasTable = Convert.ToInt32(checkTable.ExecuteScalar()) > 0;

        if (hasTable)
        {
            // ตรวจสอบว่ามี trainer_id column หรือไม่
            var checkCol = connection.CreateCommand();
            checkCol.CommandText = "PRAGMA table_info(ClassRegisRecord)";
            bool hasTrainerIdCol = false;

            using var reader = checkCol.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1) == "trainer_id")
                {
                    hasTrainerIdCol = true;
                    break;
                }
            }

            if (!hasTrainerIdCol)
            {
                System.Diagnostics.Debug.WriteLine("🔄 Migrating ClassRegisRecord: adding trainer_id column...");
                MigrateAddTrainerId(connection);
            }
        }
        else
        {
            // ไม่มีตาราง → สร้างใหม่
            CreateTable(connection);
        }

        System.Diagnostics.Debug.WriteLine("✅ ClassRegisRecord table initialized");
    }

    private void CreateTable(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ClassRegisRecord (
                trainee_id         TEXT(10) NOT NULL,
                class_id           TEXT(4) NOT NULL,
                trainer_id         TEXT(9) NOT NULL DEFAULT '',
                regis_date         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (trainee_id, class_id, trainer_id),
                FOREIGN KEY (trainee_id) REFERENCES Trainee(trainee_id) ON DELETE CASCADE,
                FOREIGN KEY (class_id, trainer_id) REFERENCES Course(class_id, trainer_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Trainee ON ClassRegisRecord(trainee_id);
            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Class ON ClassRegisRecord(class_id);
            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Trainer ON ClassRegisRecord(trainer_id);
            CREATE INDEX IF NOT EXISTS IX_ClassRegisRecord_Date ON ClassRegisRecord(regis_date);
        ";
        command.ExecuteNonQuery();
    }

    private void MigrateAddTrainerId(SqliteConnection connection)
    {
        try
        {
            // 1) Rename old table
            var rename = connection.CreateCommand();
            rename.CommandText = "ALTER TABLE ClassRegisRecord RENAME TO ClassRegisRecord_old";
            rename.ExecuteNonQuery();

            // 2) Create new table with trainer_id
            CreateTable(connection);

            // 3) Copy data — fill trainer_id from Course table
            var copy = connection.CreateCommand();
            copy.CommandText = @"
                INSERT OR IGNORE INTO ClassRegisRecord (trainee_id, class_id, trainer_id, regis_date)
                SELECT r.trainee_id, r.class_id, COALESCE(c.trainer_id, ''), r.regis_date
                FROM ClassRegisRecord_old r
                LEFT JOIN Course c ON r.class_id = c.class_id
            ";
            var copied = copy.ExecuteNonQuery();

            // 4) Drop old table
            var drop = connection.CreateCommand();
            drop.CommandText = "DROP TABLE IF EXISTS ClassRegisRecord_old";
            drop.ExecuteNonQuery();

            System.Diagnostics.Debug.WriteLine($"✅ ClassRegisRecord migration complete: {copied} records migrated");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ClassRegisRecord migration error: {ex.Message}");
            try
            {
                var restore = connection.CreateCommand();
                restore.CommandText = @"
                    DROP TABLE IF EXISTS ClassRegisRecord;
                    ALTER TABLE ClassRegisRecord_old RENAME TO ClassRegisRecord;
                ";
                restore.ExecuteNonQuery();
            }
            catch { /* ignore */ }
        }
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
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name,
                r.trainer_id
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            ORDER BY r.regis_date DESC
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            registrations.Add(ReadRegistrationFromReader(reader));
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
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name,
                r.trainer_id
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE r.trainee_id = @trainee_id
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            registrations.Add(ReadRegistrationFromReader(reader));
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
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name,
                r.trainer_id
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE r.class_id = @class_id
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@class_id", classId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            registrations.Add(ReadRegistrationFromReader(reader));
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

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ClassRegisRecord (trainee_id, class_id, trainer_id, regis_date)
            VALUES (@trainee_id, @class_id, @trainer_id, @regis_date)
        ";

        command.Parameters.AddWithValue("@trainee_id", registration.TraineeId);
        command.Parameters.AddWithValue("@class_id", registration.ClassId);
        command.Parameters.AddWithValue("@trainer_id", registration.TrainerId);
        command.Parameters.AddWithValue("@regis_date", registration.RegisDate);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Added registration: {registration.TraineeId} -> {registration.ClassId} (trainer: {registration.TrainerId})");
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
    /// Check if registration exists (composite key: trainee + class + trainer)
    /// </summary>
    public async Task<bool> RegistrationExistsAsync(string traineeId, string classId, string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM ClassRegisRecord 
            WHERE trainee_id = @trainee_id AND class_id = @class_id AND trainer_id = @trainer_id
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

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

        // ✅ Enable Foreign Key enforcement
        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

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
    /// Delete registration (composite key: trainee + class + trainer)
    /// </summary>
    public async Task<bool> DeleteRegistrationAsync(string traineeId, string classId, string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM ClassRegisRecord 
            WHERE trainee_id = @trainee_id AND class_id = @class_id AND trainer_id = @trainer_id
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Deleted registration: {traineeId} -> {classId} (trainer: {trainerId})");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting registration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get registrations by composite key (class_id + trainer_id)
    /// </summary>
    public async Task<List<ClassRegisRecordItem>> GetRegistrationsByCompositeKeyAsync(string classId, string trainerId)
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
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name,
                r.trainer_id
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id
            LEFT JOIN Trainer tr ON c.trainer_id = tr.trainer_id
            WHERE r.class_id = @class_id AND r.trainer_id = @trainer_id
            ORDER BY r.regis_date DESC
        ";
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            registrations.Add(ReadRegistrationFromReader(reader));
        }

        return registrations;
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
                tr.trainer_fname || ' ' || tr.trainer_lname AS trainer_name,
                r.trainer_id
            FROM ClassRegisRecord r
            INNER JOIN Trainee t ON r.trainee_id = t.trainee_id
            INNER JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id
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
            registrations.Add(ReadRegistrationFromReader(reader));
        }

        return registrations;
    }

    /// <summary>
    /// Shared reader helper
    /// </summary>
    private static ClassRegisRecordItem ReadRegistrationFromReader(SqliteDataReader reader)
    {
        return ClassRegisRecordItem.FromDatabase(
            traineeId: reader.GetString(0),
            classId: reader.GetString(1),
            regisDate: reader.GetDateTime(2),
            traineeName: reader.GetString(3),
            traineePhone: reader.IsDBNull(4) ? "" : reader.GetString(4),
            className: reader.GetString(5),
            classTime: reader.GetInt32(6),
            classRate: reader.GetInt32(7),
            trainerName: reader.IsDBNull(8) ? "" : reader.GetString(8),
            trainerId: reader.IsDBNull(9) ? "" : reader.GetString(9)
        );
    }
}
