using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// Data Access Object สำหรับจัดการข้อมูลผู้ฝึกสอน (Trainer)
/// </summary>
public class TrainerDao
{
    private readonly string _connectionString;

    public TrainerDao(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    /// <summary>
    /// สร้างตาราง Trainer
    /// </summary>
    private void InitializeDatabase()
    {
        System.Diagnostics.Debug.WriteLine("🗄️ TrainerDao: เริ่ม InitializeDatabase...");
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Trainer (
                trainer_id        TEXT(9) PRIMARY KEY NOT NULL,
                trainer_fname     TEXT(50) NOT NULL,
                trainer_lname     TEXT(50) NOT NULL,
                trainer_nickname  TEXT(50) NULL,
                trainer_birthdate DATETIME NULL,
                trainer_phone     TEXT(10) NULL,
                Trainer_img       BLOB NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Trainer_Name ON Trainer(trainer_fname, trainer_lname);
            CREATE INDEX IF NOT EXISTS IX_Trainer_Phone ON Trainer(trainer_phone);
        ";
        createCommand.ExecuteNonQuery();
        
        System.Diagnostics.Debug.WriteLine("✅ สร้างตาราง Trainer เสร็จ");

        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM Trainer";
        var count = Convert.ToInt32(checkCommand.ExecuteScalar());

        System.Diagnostics.Debug.WriteLine($"📊 จำนวนผู้ฝึกสอนปัจจุบัน: {count}");
    }

    /// <summary>
    /// ดึงข้อมูลผู้ฝึกสอนทั้งหมด
    /// </summary>
    public async Task<List<TrainerItem>> GetAllTrainersAsync()
    {
        System.Diagnostics.Debug.WriteLine("🔍 GetAllTrainersAsync เริ่มทำงาน...");
        
        var trainers = new List<TrainerItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, 
                   trainer_birthdate, trainer_phone, Trainer_img
            FROM Trainer
            ORDER BY trainer_id ASC
        ";

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var trainerId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainer = TrainerItem.FromDatabase(trainerId, firstName, lastName, nickname, birthDate, phone, imageData);
            
            trainers.Add(trainer);
        }

        System.Diagnostics.Debug.WriteLine($"📊 GetAllTrainersAsync เสร็จ - พบ {trainers.Count} คน");
        return trainers;
    }

    /// <summary>
    /// ดึงข้อมูลผู้ฝึกสอนตาม ID
    /// </summary>
    public async Task<TrainerItem?> GetTrainerByIdAsync(string trainerId)
    {
        System.Diagnostics.Debug.WriteLine($"🔍 GetTrainerByIdAsync: {trainerId}");
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, 
                   trainer_birthdate, trainer_phone, Trainer_img
            FROM Trainer
            WHERE trainer_id = @trainer_id
        ";
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainer = TrainerItem.FromDatabase(trainerId, firstName, lastName, nickname, birthDate, phone, imageData);
            
            System.Diagnostics.Debug.WriteLine($"✅ พบผู้ฝึกสอน: {trainer.FullName}");
            return trainer;
        }

        System.Diagnostics.Debug.WriteLine($"❌ ไม่พบผู้ฝึกสอน ID: {trainerId}");
        return null;
    }

    /// <summary>
    /// เพิ่มผู้ฝึกสอนใหม่
    /// </summary>
    public async Task<bool> AddTrainerAsync(TrainerItem trainer)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ TrainerDao.AddTrainerAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   TrainerID: {trainer.TrainerId}");
        System.Diagnostics.Debug.WriteLine($"   Name: {trainer.FullName}");
        System.Diagnostics.Debug.WriteLine($"   ImageData: {trainer.ImageData?.Length ?? 0} bytes");
        
        using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Trainer (trainer_id, trainer_fname, trainer_lname, trainer_nickname, 
                                     trainer_birthdate, trainer_phone, Trainer_img)
                VALUES (@trainer_id, @trainer_fname, @trainer_lname, @trainer_nickname, 
                        @trainer_birthdate, @trainer_phone, @trainer_img)
            ";

            command.Parameters.AddWithValue("@trainer_id", trainer.TrainerId);
            command.Parameters.AddWithValue("@trainer_fname", trainer.FirstName);
            command.Parameters.AddWithValue("@trainer_lname", trainer.LastName);
            command.Parameters.AddWithValue("@trainer_nickname", trainer.Nickname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_birthdate", trainer.BirthDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_phone", trainer.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_img", trainer.ImageData ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 AddTrainerAsync result: {success}");
            
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
    /// แก้ไขข้อมูลผู้ฝึกสอน
    /// </summary>
    public async Task<bool> UpdateTrainerAsync(TrainerItem trainer)
    {
        System.Diagnostics.Debug.WriteLine($"🗄️ TrainerDao.UpdateTrainerAsync เริ่มทำงาน");
        System.Diagnostics.Debug.WriteLine($"   TrainerID: {trainer.TrainerId}");
        System.Diagnostics.Debug.WriteLine($"   ImageData: {trainer.ImageData?.Length ?? 0} bytes");
        
        using var connection = new SqliteConnection(_connectionString);
        
        try
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Trainer
                SET
                    trainer_fname     = @trainer_fname,
                    trainer_lname     = @trainer_lname,
                    trainer_nickname  = @trainer_nickname,
                    trainer_birthdate = @trainer_birthdate,
                    trainer_phone     = @trainer_phone,
                    Trainer_img       = @trainer_img
                WHERE trainer_id = @trainer_id
            ";

            command.Parameters.AddWithValue("@trainer_id", trainer.TrainerId);
            command.Parameters.AddWithValue("@trainer_fname", trainer.FirstName);
            command.Parameters.AddWithValue("@trainer_lname", trainer.LastName);
            command.Parameters.AddWithValue("@trainer_nickname", trainer.Nickname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_birthdate", trainer.BirthDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_phone", trainer.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@trainer_img", trainer.ImageData ?? (object)DBNull.Value);

            var result = await command.ExecuteNonQueryAsync();
            
            bool success = result > 0;
            System.Diagnostics.Debug.WriteLine($"🎯 UpdateTrainerAsync result: {success}");
            
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
    /// ลบผู้ฝึกสอน
    /// </summary>
    public async Task<bool> DeleteTrainerAsync(string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Trainer WHERE trainer_id = @trainer_id";
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        var result = await command.ExecuteNonQueryAsync();
        return result > 0;
    }

    /// <summary>
    /// ตรวจสอบว่ามี TrainerID นี้อยู่แล้วหรือไม่
    /// </summary>
    public async Task<bool> TrainerExistsAsync(string trainerId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Trainer WHERE trainer_id = @trainer_id";
        command.Parameters.AddWithValue("@trainer_id", trainerId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    /// <summary>
    /// สร้าง TrainerID ถัดไปสำหรับปีปัจจุบัน
    /// Pattern: 2YYYY#### (2=trainer type, YYYY=year, ####=running number)
    /// </summary>
    public async Task<string> GetNextTrainerIdAsync()
    {
        var currentYear = DateTime.Now.Year;
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT MAX(trainer_id) 
            FROM Trainer 
            WHERE trainer_id LIKE @year_pattern
        ";
        command.Parameters.AddWithValue("@year_pattern", $"2{currentYear}%");

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

        var nextId = TrainerItem.GenerateTrainerId(currentYear, nextNumber);
        System.Diagnostics.Debug.WriteLine($"📋 Generated next trainer ID: {nextId}");
        
        return nextId;
    }

    /// <summary>
    /// นับจำนวนผู้ฝึกสอนทั้งหมด
    /// </summary>
    public async Task<int> GetTrainerCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Trainer";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count;
    }

    /// <summary>
    /// ค้นหาผู้ฝึกสอนตามชื่อ
    /// </summary>
    public async Task<List<TrainerItem>> SearchTrainersByNameAsync(string searchTerm)
    {
        var trainers = new List<TrainerItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, 
                   trainer_birthdate, trainer_phone, Trainer_img
            FROM Trainer
            WHERE trainer_fname LIKE @search 
               OR trainer_lname LIKE @search 
               OR trainer_nickname LIKE @search
            ORDER BY trainer_id ASC
        ";
        command.Parameters.AddWithValue("@search", $"%{searchTerm}%");

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var trainerId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainer = TrainerItem.FromDatabase(trainerId, firstName, lastName, nickname, birthDate, phone, imageData);
            trainers.Add(trainer);
        }

        return trainers;
    }

    /// <summary>
    /// ค้นหาผู้ฝึกสอนแบบละเอียด พร้อม Pagination
    /// </summary>
    /// <param name="keyword">คำค้นหา</param>
    /// <param name="searchField">ฟิลด์ที่ต้องการค้นหา: All, ID, FirstName, Phone</param>
    /// <param name="page">หน้าที่ต้องการ (เริ่มจาก 1)</param>
    /// <param name="pageSize">จำนวนรายการต่อหน้า</param>
    /// <returns>SearchResult พร้อมข้อมูลผู้ฝึกสอนและ metadata</returns>
    public async Task<SearchResult<TrainerItem>> SearchAsync(
        string keyword, 
        string searchField = "All", 
        int page = 1, 
        int pageSize = 50)
    {
        System.Diagnostics.Debug.WriteLine($"🔍 SearchAsync: keyword='{keyword}', field='{searchField}', page={page}, pageSize={pageSize}");
        
        var trainers = new List<TrainerItem>();
        int offset = (page - 1) * pageSize;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Build WHERE clause based on search field
        string whereClause = searchField switch
        {
            "ID" => "trainer_id LIKE '%' || @keyword || '%' COLLATE NOCASE",
            "FirstName" => "trainer_fname LIKE '%' || @keyword || '%' COLLATE NOCASE",
            "Phone" => "trainer_phone LIKE '%' || @keyword || '%' COLLATE NOCASE",
            _ => @"trainer_id LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainer_fname LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainer_lname LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainer_nickname LIKE '%' || @keyword || '%' COLLATE NOCASE
                OR trainer_phone LIKE '%' || @keyword || '%' COLLATE NOCASE"
        };

        // Count total results
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM Trainer WHERE {whereClause}";
        countCommand.Parameters.AddWithValue("@keyword", keyword);
        
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        // Get paginated results
        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT trainer_id, trainer_fname, trainer_lname, trainer_nickname, 
                   trainer_birthdate, trainer_phone, Trainer_img
            FROM Trainer
            WHERE {whereClause}
            ORDER BY trainer_id ASC
            LIMIT @pageSize OFFSET @offset
        ";
        command.Parameters.AddWithValue("@keyword", keyword);
        command.Parameters.AddWithValue("@pageSize", pageSize);
        command.Parameters.AddWithValue("@offset", offset);

        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var trainerId = reader.GetString(0);
            var firstName = reader.GetString(1);
            var lastName = reader.GetString(2);
            var nickname = reader.IsDBNull(3) ? null : reader.GetString(3);
            DateTime? birthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4));
            var phone = reader.IsDBNull(5) ? null : reader.GetString(5);
            var imageData = reader.IsDBNull(6) ? null : (byte[])reader.GetValue(6);
            
            var trainer = TrainerItem.FromDatabase(trainerId, firstName, lastName, nickname, birthDate, phone, imageData);
            trainers.Add(trainer);
        }

        var result = new SearchResult<TrainerItem>
        {
            Items = trainers,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        System.Diagnostics.Debug.WriteLine($"📊 SearchAsync เสร็จ - พบ {totalCount} รายการ, หน้า {page}/{result.TotalPages}");
        return result;
    }
}
