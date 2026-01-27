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
                class_id           TEXT(4) PRIMARY KEY NOT NULL,
                class_title        TEXT(50) NOT NULL,
                class_time         INTEGER NOT NULL,
                class_duration     INTEGER NULL,
                class_rate         INTEGER NULL,
                trainer_id         TEXT(9) NULL,
                created_date       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_updated       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Course_Title ON Course(class_title);
            CREATE INDEX IF NOT EXISTS IX_Course_Trainer ON Course(trainer_id);
        ";

        command.ExecuteNonQuery();
        System.Diagnostics.Debug.WriteLine("✅ Course table initialized");
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
        command.CommandText = @"
            SELECT c.class_id, c.class_title, c.class_time, c.class_duration, 
                   c.class_rate, c.trainer_id,
                   t.trainer_fname || ' ' || t.trainer_lname AS trainer_name
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            ORDER BY c.class_id
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var course = CourseItem.FromDatabase(
                classId: reader.GetString(0),
                classTitle: reader.GetString(1),
                classTime: reader.GetInt32(2),
                classDuration: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                classRate: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                trainerId: reader.IsDBNull(5) ? null : reader.GetString(5),
                trainerName: reader.IsDBNull(6) ? "" : reader.GetString(6)
            );
            courses.Add(course);
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
        command.CommandText = @"
            SELECT c.class_id, c.class_title, c.class_time, c.class_duration, 
                   c.class_rate, c.trainer_id,
                   t.trainer_fname || ' ' || t.trainer_lname AS trainer_name
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE c.class_id = @class_id
        ";
        command.Parameters.AddWithValue("@class_id", classId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return CourseItem.FromDatabase(
                classId: reader.GetString(0),
                classTitle: reader.GetString(1),
                classTime: reader.GetInt32(2),
                classDuration: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                classRate: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                trainerId: reader.IsDBNull(5) ? null : reader.GetString(5),
                trainerName: reader.IsDBNull(6) ? "" : reader.GetString(6)
            );
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
            INSERT INTO Course (class_id, class_title, class_time, class_duration, class_rate, trainer_id)
            VALUES (@class_id, @class_title, @class_time, @class_duration, @class_rate, @trainer_id)
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);
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
                trainer_id = @trainer_id,
                last_updated = CURRENT_TIMESTAMP
            WHERE class_id = @class_id
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);
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
            SELECT c.class_id, c.class_title, c.class_time, c.class_duration, 
                   c.class_rate, c.trainer_id,
                   t.trainer_fname || ' ' || t.trainer_lname AS trainer_name
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE {whereClause}
            ORDER BY c.class_id
        ";
        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var course = CourseItem.FromDatabase(
                classId: reader.GetString(0),
                classTitle: reader.GetString(1),
                classTime: reader.GetInt32(2),
                classDuration: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                classRate: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                trainerId: reader.IsDBNull(5) ? null : reader.GetString(5),
                trainerName: reader.IsDBNull(6) ? "" : reader.GetString(6)
            );
            courses.Add(course);
        }

        return courses;
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
