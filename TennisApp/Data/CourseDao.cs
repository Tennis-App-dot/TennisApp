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

        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
        pragmaCommand.ExecuteNonQuery();

        // ─── Cleanup: ลบ trigger/table ที่ค้างจาก migration เก่า ───
        CleanupStaleArtifacts(connection);

        // ─── ตรวจสอบว่าตาราง Course เดิมมีอยู่หรือไม่ ───
        var checkOld = connection.CreateCommand();
        checkOld.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Course'";
        var hasOldTable = Convert.ToInt32(checkOld.ExecuteScalar()) > 0;

        if (hasOldTable)
        {
            // ตรวจสอบว่าเป็น schema เก่า (PK = class_id เดี่ยว) หรือ schema ใหม่ (composite PK)
            var checkSchema = connection.CreateCommand();
            checkSchema.CommandText = "PRAGMA table_info(Course)";
            bool hasTrainerIdInPK = false;

            using var reader = checkSchema.ExecuteReader();
            while (reader.Read())
            {
                var colName = reader.GetString(1);  // column name
                var pk = reader.GetInt32(5);         // pk index (0 = not PK)
                var notNull = reader.GetInt32(3);    // notnull

                if (colName == "trainer_id")
                {
                    if (pk > 0) hasTrainerIdInPK = true;
                }
            }

            if (!hasTrainerIdInPK)
            {
                // Schema เก่า → migrate: สร้างตารางใหม่ + ย้ายข้อมูล
                System.Diagnostics.Debug.WriteLine("🔄 Migrating Course table to composite PK (class_id, trainer_id)...");
                MigrateToCompositePK(connection);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✅ Course table already has composite PK");
            }
        }
        else
        {
            // ไม่มีตาราง → สร้างใหม่เลย
            CreateCourseTable(connection);
        }

        System.Diagnostics.Debug.WriteLine("✅ Course table initialized");
    }

    /// <summary>
    /// ลบ trigger และตาราง Course_old ที่อาจค้างอยู่จากการ migration เก่า
    /// </summary>
    private void CleanupStaleArtifacts(SqliteConnection connection)
    {
        try
        {
            // ลบ trigger ที่ reference Course_old (ถ้ามี)
            var findTriggers = connection.CreateCommand();
            findTriggers.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type = 'trigger' 
                AND sql LIKE '%Course_old%'
            ";
            var triggerNames = new List<string>();
            using (var reader = findTriggers.ExecuteReader())
            {
                while (reader.Read())
                    triggerNames.Add(reader.GetString(0));
            }

            foreach (var triggerName in triggerNames)
            {
                var dropTrigger = connection.CreateCommand();
                dropTrigger.CommandText = $"DROP TRIGGER IF EXISTS [{triggerName}]";
                dropTrigger.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine($"🧹 Dropped stale trigger: {triggerName}");
            }

            // ลบตาราง Course_old ถ้ายังค้างอยู่
            var dropOld = connection.CreateCommand();
            dropOld.CommandText = "DROP TABLE IF EXISTS Course_old";
            dropOld.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ CleanupStaleArtifacts: {ex.Message}");
        }
    }

    private void CreateCourseTable(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Course (
                class_id              TEXT(4) NOT NULL,
                trainer_id            TEXT(9) NOT NULL,
                class_title           TEXT(50) NOT NULL,
                class_time            INTEGER NOT NULL,
                class_duration        INTEGER NULL DEFAULT 1,
                class_rate            INTEGER NULL,
                created_date          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_updated          DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (class_id, trainer_id),
                FOREIGN KEY (trainer_id) REFERENCES Trainer(trainer_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Course_Title ON Course(class_title);
            CREATE INDEX IF NOT EXISTS IX_Course_Trainer ON Course(trainer_id);
            CREATE INDEX IF NOT EXISTS IX_Course_ClassId ON Course(class_id);
        ";
        command.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine("✅ Created Course table with composite PK (class_id, trainer_id)");
    }

    private void MigrateToCompositePK(SqliteConnection connection)
    {
        try
        {
            // 1) Rename old table
            var rename = connection.CreateCommand();
            rename.CommandText = "ALTER TABLE Course RENAME TO Course_old";
            rename.ExecuteNonQuery();

            // 2) Create new table with composite PK
            CreateCourseTable(connection);

            // 3) Copy data (skip rows with NULL trainer_id — they can't be in composite PK)
            var copy = connection.CreateCommand();
            copy.CommandText = @"
                INSERT OR IGNORE INTO Course (class_id, trainer_id, class_title, class_time, class_duration, class_rate, created_date, last_updated)
                SELECT class_id, trainer_id, class_title, class_time, 
                       COALESCE(class_duration, 1), COALESCE(class_rate, 0),
                       COALESCE(created_date, CURRENT_TIMESTAMP), COALESCE(last_updated, CURRENT_TIMESTAMP)
                FROM Course_old
                WHERE trainer_id IS NOT NULL AND trainer_id != ''
            ";
            var copied = copy.ExecuteNonQuery();

            // 4) Drop old table
            var drop = connection.CreateCommand();
            drop.CommandText = "DROP TABLE IF EXISTS Course_old";
            drop.ExecuteNonQuery();

            System.Diagnostics.Debug.WriteLine($"✅ Migration complete: {copied} courses migrated to composite PK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Migration error: {ex.Message}");

            // Rollback: restore old table if migration failed
            try
            {
                var restore = connection.CreateCommand();
                restore.CommandText = @"
                    DROP TABLE IF EXISTS Course;
                    ALTER TABLE Course_old RENAME TO Course;
                ";
                restore.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("⚠️ Rolled back to old Course table");
            }
            catch { /* ignore rollback errors */ }
        }
    }

    // ─── Shared SQL fragment ──────────────────────────────────
    private const string SelectColumns = @"
        c.class_id, c.class_title, c.class_time, c.class_duration, 
        c.class_rate, c.trainer_id,
        t.trainer_fname || ' ' || t.trainer_lname AS trainer_name,
        c.last_updated
    ";

    private static CourseItem ReadCourseFromReader(SqliteDataReader reader)
    {
        DateTime? lastUpdated = null;
        if (!reader.IsDBNull(7))
        {
            try { lastUpdated = DateTime.Parse(reader.GetString(7)); }
            catch { /* ignore parse errors */ }
        }

        return CourseItem.FromDatabase(
            classId: reader.GetString(0),
            classTitle: reader.GetString(1),
            classTime: reader.GetInt32(2),
            classDuration: reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
            classRate: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            trainerId: reader.IsDBNull(5) ? "" : reader.GetString(5),
            trainerName: reader.IsDBNull(6) ? "" : reader.GetString(6),
            lastUpdated: lastUpdated
        );
    }

    // ═══════════════════════════════════════════════════════════
    // READ
    // ═══════════════════════════════════════════════════════════

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
            ORDER BY c.class_id, c.trainer_id
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
    /// Get course by composite key (class_id + trainer_id)
    /// </summary>
    public async Task<CourseItem?> GetCourseByKeyAsync(string classId, string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE c.class_id = @class_id AND c.trainer_id = @trainer_id
        ";
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadCourseFromReader(reader);
        }

        return null;
    }

    /// <summary>
    /// Get course by class_id only (backward compatible — returns first match)
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
            LIMIT 1
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
    /// Get all courses by class_id (may return multiple rows for different trainers)
    /// </summary>
    public async Task<List<CourseItem>> GetCoursesByClassIdAsync(string classId)
    {
        var courses = new List<CourseItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT {SelectColumns}
            FROM Course c
            LEFT JOIN Trainer t ON c.trainer_id = t.trainer_id
            WHERE c.class_id = @class_id
            ORDER BY c.trainer_id
        ";
        command.Parameters.AddWithValue("@class_id", classId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(ReadCourseFromReader(reader));
        }

        return courses;
    }

    // ═══════════════════════════════════════════════════════════
    // CREATE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Add new course (composite PK: class_id + trainer_id)
    /// </summary>
    public async Task<bool> AddCourseAsync(CourseItem course)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Course (class_id, trainer_id, class_title, class_time, class_duration, class_rate)
            VALUES (@class_id, @trainer_id, @class_title, @class_time, @class_duration, @class_rate)
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@trainer_id", course.TrainerId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Added course: {course.ClassId} + trainer {course.TrainerId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding course: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UPDATE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Update existing course (composite PK: class_id + trainer_id)
    /// Only trainer can be changed — class_id, rate, sessions are fixed
    /// </summary>
    public async Task<bool> UpdateCourseTrainerAsync(string classId, string oldTrainerId, string newTrainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        // Since trainer_id is part of PK, we need DELETE + INSERT
        using var transaction = connection.BeginTransaction();
        try
        {
            // Get existing course data
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT class_title, class_time, class_duration, class_rate, created_date FROM Course WHERE class_id = @class_id AND trainer_id = @old_trainer_id";
            selectCmd.Parameters.AddWithValue("@class_id", classId);
            selectCmd.Parameters.AddWithValue("@old_trainer_id", oldTrainerId);

            string? classTitle = null;
            int classTime = 0, classDuration = 1, classRate = 0;
            string? createdDate = null;

            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    classTitle = reader.GetString(0);
                    classTime = reader.GetInt32(1);
                    classDuration = reader.IsDBNull(2) ? 1 : reader.GetInt32(2);
                    classRate = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    createdDate = reader.IsDBNull(4) ? null : reader.GetString(4);
                }
            }

            if (classTitle == null)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Course not found: {classId} + {oldTrainerId}");
                return false;
            }

            // Delete old row
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Course WHERE class_id = @class_id AND trainer_id = @old_trainer_id";
            deleteCmd.Parameters.AddWithValue("@class_id", classId);
            deleteCmd.Parameters.AddWithValue("@old_trainer_id", oldTrainerId);
            await deleteCmd.ExecuteNonQueryAsync();

            // Insert new row with new trainer_id
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Course (class_id, trainer_id, class_title, class_time, class_duration, class_rate, created_date, last_updated)
                VALUES (@class_id, @new_trainer_id, @class_title, @class_time, @class_duration, @class_rate, @created_date, CURRENT_TIMESTAMP)
            ";
            insertCmd.Parameters.AddWithValue("@class_id", classId);
            insertCmd.Parameters.AddWithValue("@new_trainer_id", newTrainerId);
            insertCmd.Parameters.AddWithValue("@class_title", classTitle);
            insertCmd.Parameters.AddWithValue("@class_time", classTime);
            insertCmd.Parameters.AddWithValue("@class_duration", classDuration);
            insertCmd.Parameters.AddWithValue("@class_rate", classRate);
            insertCmd.Parameters.AddWithValue("@created_date", createdDate ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var result = await insertCmd.ExecuteNonQueryAsync();
            transaction.Commit();

            System.Diagnostics.Debug.WriteLine($"✅ Updated course trainer: {classId} ({oldTrainerId} → {newTrainerId})");
            return result > 0;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            System.Diagnostics.Debug.WriteLine($"❌ Error updating course trainer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update existing course (backward compatible — updates by class_id + trainer_id)
    /// </summary>
    public async Task<bool> UpdateCourseAsync(CourseItem course)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Course
            SET class_title = @class_title,
                class_time = @class_time,
                class_duration = @class_duration,
                class_rate = @class_rate,
                last_updated = CURRENT_TIMESTAMP
            WHERE class_id = @class_id AND trainer_id = @trainer_id
        ";

        command.Parameters.AddWithValue("@class_id", course.ClassId);
        command.Parameters.AddWithValue("@trainer_id", course.TrainerId);
        command.Parameters.AddWithValue("@class_title", course.ClassTitle);
        command.Parameters.AddWithValue("@class_time", course.ClassTime);
        command.Parameters.AddWithValue("@class_duration", course.ClassDuration);
        command.Parameters.AddWithValue("@class_rate", course.ClassRate);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Updated course: {course.ClassId} + {course.TrainerId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating course: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Delete course by composite key (class_id + trainer_id)
    /// </summary>
    public async Task<bool> DeleteCourseAsync(string classId, string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Course WHERE class_id = @class_id AND trainer_id = @trainer_id";
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Deleted course: {classId} + {trainerId}");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting course: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete course by class_id only (backward compatible — deletes all trainers for this class_id)
    /// </summary>
    public async Task<bool> DeleteCourseAsync(string classId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Course WHERE class_id = @class_id";
        command.Parameters.AddWithValue("@class_id", classId);

        try
        {
            var result = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Deleted all courses with class_id: {classId} ({result} rows)");
            return result > 0;
        }
        catch (SqliteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error deleting course: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXISTS / COUNT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Check if course exists by composite key
    /// </summary>
    public async Task<bool> CourseExistsAsync(string classId, string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Course WHERE class_id = @class_id AND trainer_id = @trainer_id";
        command.Parameters.AddWithValue("@class_id", classId);
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /// <summary>
    /// Check if course exists by class_id only (backward compatible)
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

    // ═══════════════════════════════════════════════════════════
    // SEARCH
    // ═══════════════════════════════════════════════════════════

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
            ORDER BY c.class_id, c.trainer_id
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
            ORDER BY c.class_id, c.trainer_id
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
            ORDER BY trainer_name
        ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
                names.Add(reader.GetString(0));
        }

        return names;
    }
}
