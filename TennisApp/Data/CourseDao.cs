using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

public class CourseDao
{
    private readonly string _connectionString;

    public CourseDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS Course (
                class_id              TEXT(4) PRIMARY KEY NOT NULL,
                class_title           TEXT(50) NOT NULL,
                class_time            INTEGER NOT NULL,
                class_duration        INTEGER NULL,
                class_rate            INTEGER NULL,
                class_rate_per_time   INTEGER NULL,
                class_rate_4          INTEGER NULL,
                class_rate_8          INTEGER NULL,
                class_rate_12         INTEGER NULL,
                class_rate_16         INTEGER NULL,
                class_rate_monthly    INTEGER NULL,
                class_rate_night      INTEGER NULL,
                trainer_id            TEXT(9) NULL,
                created_date          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_updated          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Course_Title ON Course(class_title);
            CREATE INDEX IF NOT EXISTS IX_Course_Trainer ON Course(trainer_id);
        ";

        command.ExecuteNonQuery();

        // Migrate: add tier pricing columns if missing (for existing databases)
        var migrationColumns = new[]
        {
            ("class_rate_per_time", "INTEGER NULL"),
            ("class_rate_4", "INTEGER NULL"),
            ("class_rate_8", "INTEGER NULL"),
            ("class_rate_12", "INTEGER NULL"),
            ("class_rate_16", "INTEGER NULL"),
            ("class_rate_monthly", "INTEGER NULL"),
            ("class_rate_night", "INTEGER NULL")
        };

        foreach (var (colName, colType) in migrationColumns)
        {
            try
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE Course ADD COLUMN {colName} {colType}";
                alterCmd.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists — ignore
            }
        }

        System.Diagnostics.Debug.WriteLine("✅ Course table initialized");
    }

    // ─── Shared SQL fragment ──────────────────────────────────
    private const string SelectColumns = @"
        c.class_id, c.class_title, c.class_time, c.class_duration, 
        c.class_rate, c.trainer_id,
        t.trainer_fname || ' ' || t.trainer_lname AS trainer_name,
        c.class_rate_per_time, c.class_rate_4, c.class_rate_8,
        c.class_rate_12, c.class_rate_16, c.class_rate_monthly,
        c.class_rate_night, c.last_updated
    ";

    private static CourseItem ReadCourseFromReader(SqliteDataReader reader)
    {
        DateTime? lastUpdated = null;
        if (!reader.IsDBNull(14))
        {
            try { lastUpdated = DateTime.Parse(reader.GetString(14)); }
            catch { /* ignore parse errors */ }
        }

        return CourseItem.FromDatabase(
            classId: reader.GetString(0),
            classTitle: reader.GetString(1),
            classTime: reader.GetInt32(2),
            classDuration: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            classRate: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            trainerId: reader.IsDBNull(5) ? null : reader.GetString(5),
            trainerName: reader.IsDBNull(6) ? "" : reader.GetString(6),
            classRatePerTime: reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            classRate4: reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            classRate8: reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
            classRate12: reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
            classRate16: reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
            classRateMonthly: reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
            classRateNight: reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
            lastUpdated: lastUpdated
        );
    }

    /// <summary>
    /// Get all courses with trainer information
    /// </summary>
    public async Task<List<CourseItem>> GetAllCoursesAsync()
    {
        var courses = new List<CourseItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            ORDER BY c.class_id
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(ReadCourseFromReader(reader));
        }

        System.Diagnostics.Debug.WriteLine($"📚 Loaded {courses.Count} courses from database");
        return courses;
    }

    /// <summary>
    /// Get course by ID
    /// </summary>
    public async Task<CourseItem?> GetCourseByIdAsync(string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE c.class_id = @class_id
        ";
        command.Parameters.AddWithValue("@class_id", classId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadCourseFromReader(reader);
        }

        return null;
    }

    /// <summary>
    /// Add new course
    /// </summary>
    public async Task<bool> AddCourseAsync(CourseItem course)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Course (class_id, class_title, class_time, class_duration, class_rate, 
                                class_rate_per_time, class_rate_4, class_rate_8, class_rate_12, 
                                class_rate_16, class_rate_monthly, class_rate_night, trainer_id)
            VALUES (@class_id, @class_title, @class_time, @class_duration, @class_rate,
                    @class_rate_per_time, @class_rate_4, @class_rate_8, @class_rate_12,
                    @class_rate_16, @class_rate_monthly, @class_rate_night, @trainer_id)
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);
        command.Parameters.AddWithValue("@class_rate_per_time", course.ClassRatePerTime > 0 ? course.ClassRatePerTime : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_4", course.ClassRate4 > 0 ? course.ClassRate4 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_8", course.ClassRate8 > 0 ? course.ClassRate8 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_12", course.ClassRate12 > 0 ? course.ClassRate12 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_16", course.ClassRate16 > 0 ? course.ClassRate16 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_monthly", course.ClassRateMonthly > 0 ? course.ClassRateMonthly : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_night", course.ClassRateNight > 0 ? course.ClassRateNight : DBNull.Value);
        command.Parameters.AddWithValue("@trainer_id", 
            string.IsNullOrWhiteSpace(course.TrainerId) ? DBNull.Value : course.TrainerId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Added course: {course.ClassId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding course: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update existing course
    /// </summary>
    public async Task<bool> UpdateCourseAsync(CourseItem course)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Course
            SET class_title = @class_title,
                class_time = @class_time,
                class_duration = @class_duration,
                class_rate = @class_rate,
                class_rate_per_time = @class_rate_per_time,
                class_rate_4 = @class_rate_4,
                class_rate_8 = @class_rate_8,
                class_rate_12 = @class_rate_12,
                class_rate_16 = @class_rate_16,
                class_rate_monthly = @class_rate_monthly,
                class_rate_night = @class_rate_night,
                trainer_id = @trainer_id,
                last_updated = CURRENT_TIMESTAMP
            WHERE class_id = @class_id
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);
        command.Parameters.AddWithValue("@class_rate_per_time", course.ClassRatePerTime > 0 ? course.ClassRatePerTime : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_4", course.ClassRate4 > 0 ? course.ClassRate4 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_8", course.ClassRate8 > 0 ? course.ClassRate8 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_12", course.ClassRate12 > 0 ? course.ClassRate12 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_16", course.ClassRate16 > 0 ? course.ClassRate16 : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_monthly", course.ClassRateMonthly > 0 ? course.ClassRateMonthly : DBNull.Value);
        command.Parameters.AddWithValue("@class_rate_night", course.ClassRateNight > 0 ? course.ClassRateNight : DBNull.Value);
        command.Parameters.AddWithValue("@trainer_id", 
            string.IsNullOrWhiteSpace(course.TrainerId) ? DBNull.Value : course.TrainerId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Updated course: {course.ClassId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating course: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete course
    /// </summary>
    public async Task<bool> DeleteCourseAsync(string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Course WHERE class_id = @class_id";
        command.Parameters.AddWithValue("@class_id", classId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Deleted course: {classId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting course: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if course exists
    /// </summary>
    public async Task<bool> CourseExistsAsync(string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Course WHERE class_id = @class_id";
        command.Parameters.AddWithValue("@class_id", classId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /// <summary>
    /// Search courses by keyword
    /// </summary>
    public async Task<List<CourseItem>> SearchCoursesAsync(string keyword, string searchField = "All")
    {
        var courses = new List<CourseItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        
        var whereClause = searchField switch
        {
            "รหัสคอร์ส" => "c.class_id LIKE @keyword",
            "ชื่อคอร์ส" => "c.class_title LIKE @keyword",
            "ผู้ฝึกสอน" => "(t.trainer_fname LIKE @keyword OR t.trainer_lname LIKE @keyword)",
            _ => "(c.class_id LIKE @keyword OR c.class_title LIKE @keyword OR t.trainer_fname LIKE @keyword OR t.trainer_lname LIKE @keyword)"
        };

        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE {whereClause}
            ORDER BY c.class_id
        ";
        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(ReadCourseFromReader(reader));
        }

        return courses;
    }

    /// <summary>
    /// Search courses with multi-field filter (AND logic)
    /// </summary>
    public async Task<List<CourseItem>> SearchCoursesMultiFieldAsync(
        string? classIdKeyword,
        string? classTitleKeyword,
        string? trainerNameFilter)
    {
        var courses = new List<CourseItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(classIdKeyword))
        {
            conditions.Add("c.class_id LIKE @classId");
            command.Parameters.AddWithValue("@classId", $"%{classIdKeyword}%");
        }

        if (!string.IsNullOrWhiteSpace(classTitleKeyword))
        {
            conditions.Add("c.class_title LIKE @classTitle");
            command.Parameters.AddWithValue("@classTitle", $"%{classTitleKeyword}%");
        }

        if (!string.IsNullOrWhiteSpace(trainerNameFilter) && trainerNameFilter != "ทั้งหมด")
        {
            conditions.Add("(t.trainer_fname || ' ' || t.trainer_lname) = @trainerName");
            command.Parameters.AddWithValue("@trainerName", trainerNameFilter);
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            {whereClause}
            ORDER BY c.class_id
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(ReadCourseFromReader(reader));
        }

        return courses;
    }

    /// <summary>
    /// Get distinct trainer names from courses (for filter ComboBox)
    /// </summary>
    public async Task<List<string>> GetAllTrainerNamesAsync()
    {
        var names = new List<string>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT t.trainer_fname || ' ' || t.trainer_lname AS trainer_name
            FROM Course c
            INNER JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE c.trainer_id IS NOT NULL
            ORDER BY trainer_name
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                names.Add(reader.GetString(0));
            }
        }

        return names;
    }

    /// <summary>
    /// Get total course count
    /// </summary>
    public async Task<int> GetCourseCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Course";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count;
    }
}
