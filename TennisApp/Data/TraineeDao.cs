using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// Data Access Object สำหรับจัดการข้อมูลผู้เรียน (Trainee)
/// </summary>
public class TraineeDao
{
    private readonly string _connectionString;

    public TraineeDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    /// <summary>
    /// สร้างตาราง Trainee
    /// </summary>
    private void InitializeDatabase()
    {
        System.Diagnostics.Debug.WriteLine("🗄️ TraineeDao: เริ่ม InitializeDatabase...");
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // ✅ Enable Foreign Key enforcement
        var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON";
        pragmaCommand.ExecuteNonQuery();

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Trainee (
                trainee_id        TEXT(9) PRIMARY KEY NOT NULL,
                trainee_fname     TEXT(50) NOT NULL,
                trainee_lname     TEXT(50) NOT NULL,
                trainee_nickname  TEXT(50) NULL,
                trainee_birthdate DATETIME NULL,
                trainee_phone     TEXT(10) NULL,
                trainee_img       BLOB NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Trainee_Name ON Trainee(trainee_fname, trainee_lname);
            CREATE INDEX IF NOT EXISTS IX_Trainee_Phone ON Trainee(trainee_phone);
        ";
        createCommand.ExecuteNonQuery();
        
        System.Diagnostics.Debug.WriteLine("✅ สร้างตาราง Trainee เสร็จ");

        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Trainee";
        var count = Convert.ToInt32(checkCommand.ExecuteScalar());

        System.Diagnostics.Debug.WriteLine($"📊 จำนวนผู้เรียนปัจจุบัน: {count}");
    }

    /// <summary>
    /// ดึงข้อมูลผู้เรียนทั้งหมด
    /// </summary>
    public async Task<List<TraineeItem>> GetAllTraineesAsync()
    {
        System.Diagnostics.Debug.WriteLine("🔍 GetAllTraineesAsync เริ่มทำงาน...");
        
        var trainees = new List<TraineeItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, 
                   trainee_birthdate, trainee_phone, trainee_img
            FROM Trainee
            ORDER BY trainee_id ASC
        ";

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var traineeId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainee = TraineeItem.FromDatabase(traineeId, firstName, lastName, nickname, birthDate, phone, imageData);
            
            trainees.Add(trainee);
        }

        System.Diagnostics.Debug.WriteLine($"📊 GetAllTraineesAsync เสร็จ - พบ {trainees.Count} คน");
        return trainees;
    }

    /// <summary>
    /// ดึงข้อมูลผู้เรียนตาม ID
    /// </summary>
    public async Task<TraineeItem?> GetTraineeByIdAsync(string traineeId)
    {
        System.Diagnostics.Debug.WriteLine($"🔍 GetTraineeByIdAsync: {traineeId}");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, 
                   trainee_birthdate, trainee_phone, trainee_img
            FROM Trainee
            WHERE trainee_id = @trainee_id
        ";
        command.Parameters.AddWithValue("@trainee_id", traineeId);

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainee = TraineeItem.FromDatabase(traineeId, firstName, lastName, nickname, birthDate, phone, imageData);
            
            System.Diagnostics.Debug.WriteLine($"✅ พบผู้เรียน: {trainee.FullName}");
            return trainee;
        }

        System.Diagnostics.Debug.WriteLine($"❌ ไม่พบผู้เรียน ID: {traineeId}");
        return null;
    }

    /// <summary>
    /// เพิ่มผู้เรียนใหม่
    /// </summary>
    public async Task<bool> AddTraineeAsync(TraineeItem trainee)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ TraineeDao.AddTraineeAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   TraineeID: {trainee.TraineeId}");
        System.Diagnostics.Debug.WriteLine($"   Name: {trainee.FullName}");
        System.Diagnostics.Debug.WriteLine($"   ImageData: {trainee.ImageData?.Length ?? 0} bytes");
        
        using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Trainee (trainee_id, trainee_fname, trainee_lname, trainee_nickname, 
                                     trainee_birthdate, trainee_phone, trainee_img)
                VALUES (@trainee_id, @trainee_fname, @trainee_lname, @trainee_nickname, 
                        @trainee_birthdate, @trainee_phone, @trainee_img)
            ";

            command.Parameters.AddWithValue("@trainee_id", trainee.TraineeId);
            command.Parameters.AddWithValue("@trainee_fname", trainee.FirstName);
            command.Parameters.AddWithValue("@trainee_lname", trainee.LastName);
            command.Parameters.AddWithValue("@trainee_nickname", trainee.Nickname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_birthdate", trainee.BirthDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_phone", trainee.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_img", trainee.ImageData ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 AddTraineeAsync result: {success}");
            
            return success;
        }
        catch (SqliteException sqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SQLite Error: {sqlEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// แก้ไขข้อมูลผู้เรียน
    /// </summary>
    public async Task<bool> UpdateTraineeAsync(TraineeItem trainee)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ TraineeDao.UpdateTraineeAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   TraineeID: {trainee.TraineeId}");
        System.Diagnostics.Debug.WriteLine($"   ImageData: {trainee.ImageData?.Length ?? 0} bytes");
        
        using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Trainee
                SET
                    trainee_fname     = @trainee_fname,
                    trainee_lname     = @trainee_lname,
                    trainee_nickname  = @trainee_nickname,
                    trainee_birthdate = @trainee_birthdate,
                    trainee_phone     = @trainee_phone,
                    trainee_img       = @trainee_img
                WHERE trainee_id = @trainee_id
            ";

            command.Parameters.AddWithValue("@trainee_id", trainee.TraineeId);
            command.Parameters.AddWithValue("@trainee_fname", trainee.FirstName);
            command.Parameters.AddWithValue("@trainee_lname", trainee.LastName);
            command.Parameters.AddWithValue("@trainee_nickname", trainee.Nickname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_birthdate", trainee.BirthDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_phone", trainee.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainee_img", trainee.ImageData ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 UpdateTraineeAsync result: {success}");
            
            return success;
        }
        catch (SqliteException sqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SQLite Error: {sqlEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ลบผู้เรียน
    /// </summary>
    public async Task<bool> DeleteTraineeAsync(string traineeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // ✅ Enable Foreign Key enforcement for CASCADE delete
        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
        await pragmaCmd.ExecuteNonQueryAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Trainee WHERE trainee_id = @trainee_id";
        command.Parameters.AddWithValue("@trainee_id", traineeId);

        var result = await command.ExecuteNonQueryAsync();
        return result > 0;
    }

    /// <summary>
    /// ตรวจสอบว่ามี TraineeID นี้อยู่แล้วหรือไม่
    /// </summary>
    public async Task<bool> TraineeExistsAsync(string traineeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Trainee WHERE trainee_id = @trainee_id";
        command.Parameters.AddWithValue("@trainee_id", traineeId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /// <summary>
    /// สร้าง TraineeID ถัดไปสำหรับปีปัจจุบัน
    /// Pattern: 1YYYY#### (1=trainee type, YYYY=year, ####=running number)
    /// </summary>
    public async Task<string> GetNextTraineeIdAsync()
    {
        var currentYear = DateTime.Now.Year;
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT MAX(trainee_id) 
            FROM Trainee 
            WHERE trainee_id LIKE @year_pattern
        ";
        command.Parameters.AddWithValue("@year_pattern", $"1{currentYear}%");

        var maxId = await command.ExecuteScalarAsync();
        
        int nextNumber = 1;
        
        if (maxId != null && maxId != DBNull.Value)
        {
            var maxIdStr = maxId.ToString();
            if (maxIdStr!.Length == 9)
            {
                var lastNumber = int.Parse(maxIdStr.Substring(5, 4));
                nextNumber = lastNumber + 1;
            }
        }

        var nextId = TraineeItem.GenerateTraineeId(currentYear, nextNumber);
        System.Diagnostics.Debug.WriteLine($"📋 Generated next trainee ID: {nextId}");
        
        return nextId;
    }

    /// <summary>
    /// นับจำนวนผู้เรียนทั้งหมด
    /// </summary>
    public async Task<int> GetTraineeCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Trainee";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count;
    }

    /// <summary>
    /// ค้นหาผู้เรียนตามชื่อ
    /// </summary>
    public async Task<List<TraineeItem>> SearchTraineesByNameAsync(string searchTerm)
    {
        var trainees = new List<TraineeItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, 
                   trainee_birthdate, trainee_phone, trainee_img
            FROM Trainee
            WHERE trainee_fname LIKE @search 
               OR trainee_lname LIKE @search 
               OR trainee_nickname LIKE @search
            ORDER BY trainee_id ASC
        ";
        command.Parameters.AddWithValue("@search", $"%{searchTerm}%");

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var traineeId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainee = TraineeItem.FromDatabase(traineeId, firstName, lastName, nickname, birthDate, phone, imageData);
            trainees.Add(trainee);
        }

        return trainees;
    }

    /// <summary>
    /// ค้นหาผู้เรียนแบบละเอียด พร้อม Pagination
    /// </summary>
    /// <param name="keyword">คำค้นหา</param>
    /// <param name="searchField">ฟิลด์ที่ต้องการค้นหา: All, ID, FirstName, Phone</param>
    /// <param name="page">หน้าที่ต้องการ (เริ่มจาก 1)</param>
    /// <param name="pageSize">จำนวนรายการต่อหน้า</param>
    /// <returns>SearchResult พร้อมข้อมูลผู้เรียนและ metadata</returns>
    public async Task<SearchResult<TraineeItem>> SearchAsync(
        string keyword, 
        string searchField = "All", 
        int page = 1, 
        int pageSize = 50)
    {
        System.Diagnostics.Debug.WriteLine($"🔍 SearchAsync: keyword='{keyword}', field='{searchField}', page={page}, pageSize={pageSize}");
        
        var trainees = new List<TraineeItem>();
        int offset = (page - 1) * pageSize;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Build WHERE clause based on search field
        string whereClause = searchField switch
        {
            "ID" => "trainee_id LIKE '%' || @keyword || '%' COLLATE NOCASE",
            "FirstName" => "trainee_fname LIKE '%' || @keyword || '%' COLLATE NOCASE",
            "Phone" => "trainee_phone LIKE '%' || @keyword || '%' COLLATE NOCASE",
            _ => @"trainee_id LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainee_fname LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainee_lname LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainee_phone LIKE '%' || @keyword || '%' COLLATE NOCASE"
        };

        // Get total count first
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = $@"
            SELECT COUNT(*)
            FROM Trainee
            WHERE {whereClause}
        ";
        countCommand.Parameters.AddWithValue("@keyword", keyword ?? "");
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        // Get paginated results
        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT trainee_id, trainee_fname, trainee_lname, trainee_nickname, 
                   trainee_birthdate, trainee_phone, trainee_img
            FROM Trainee
            WHERE {whereClause}
            ORDER BY trainee_id ASC
            LIMIT @limit OFFSET @offset
        ";
        command.Parameters.AddWithValue("@keyword", keyword ?? "");
        command.Parameters.AddWithValue("@limit", pageSize);
        command.Parameters.AddWithValue("@offset", offset);

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var traineeId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainee = TraineeItem.FromDatabase(traineeId, firstName, lastName, nickname, birthDate, phone, imageData);
            trainees.Add(trainee);
        }

        System.Diagnostics.Debug.WriteLine($"✅ SearchAsync: พบ {trainees.Count}/{totalCount} รายการ");

        return new SearchResult<TraineeItem>
        {
            Items = trainees,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}

/// <summary>
/// ผลลัพธ์การค้นหาพร้อม Pagination
/// </summary>
public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
