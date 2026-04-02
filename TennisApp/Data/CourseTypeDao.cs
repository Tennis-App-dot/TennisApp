using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TennisApp.Models;

namespace TennisApp.Data;

/// <summary>
/// DAO สำหรับจัดการประเภทคอร์ส + แพ็กเกจราคา
/// เจ้าของสนามจัดการผ่าน UI ได้เอง ไม่ต้องแก้โค้ด
/// </summary>
public class CourseTypeDao
{
    private readonly string _connectionString;

    public CourseTypeDao(string connectionString)
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
            CREATE TABLE IF NOT EXISTS CourseType (
                type_code       TEXT(2) PRIMARY KEY NOT NULL,
                type_name       TEXT(50) NOT NULL,
                type_name_thai  TEXT(50) NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS CoursePackage (
                type_code   TEXT(2) NOT NULL,
                sessions    INTEGER NOT NULL,
                price       INTEGER NOT NULL,
                PRIMARY KEY (type_code, sessions),
                FOREIGN KEY (type_code) REFERENCES CourseType(type_code) ON DELETE CASCADE
            );
        ";
        cmd.ExecuteNonQuery();

        SeedDefaultsIfEmpty(connection);

        System.Diagnostics.Debug.WriteLine("✅ CourseType + CoursePackage tables initialized");
    }

    /// <summary>
    /// Seed ข้อมูลเริ่มต้นจาก hardcoded pricing (ถ้าตารางว่าง)
    /// </summary>
    private static void SeedDefaultsIfEmpty(SqliteConnection connection)
    {
        var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM CourseType";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        if (count > 0) return;

        System.Diagnostics.Debug.WriteLine("🌱 Seeding default course types + packages...");

        var types = new (string Code, string Name, string Thai)[]
        {
            ("TA", "Adult", "ผู้ใหญ่"),
            ("T1", "Red & Orange Ball", "เด็กเล็ก"),
            ("T2", "Intermediate", "ระดับกลาง"),
            ("T3", "Competitive", "แข่งขัน"),
            ("P1", "Private Kru Mee", ""),
            ("P2", "Private + Coach (Day)", "กลางวัน"),
            ("P3", "Private + Coach (Night)", "กลางคืน")
        };

        foreach (var (code, name, thai) in types)
        {
            var ins = connection.CreateCommand();
            ins.CommandText = "INSERT OR IGNORE INTO CourseType (type_code, type_name, type_name_thai) VALUES (@c, @n, @t)";
            ins.Parameters.AddWithValue("@c", code);
            ins.Parameters.AddWithValue("@n", name);
            ins.Parameters.AddWithValue("@t", thai);
            ins.ExecuteNonQuery();
        }

        var packages = new (string Code, int Sessions, int Price)[]
        {
            ("TA", 1, 600), ("TA", 4, 2200), ("TA", 8, 4000),
            ("T1", 1, 600), ("T1", 4, 1800), ("T1", 8, 3200), ("T1", 12, 4500),
            ("T2", 1, 800), ("T2", 4, 3000), ("T2", 8, 4800), ("T2", 12, 6600), ("T2", 16, 8000),
            ("T3", 1, 900), ("T3", 8, 6500), ("T3", 12, 8500), ("T3", 16, 11500), ("T3", 0, 13000),
            ("P1", 1, 2500),
            ("P2", 1, 950),
            ("P3", 1, 1050)
        };

        foreach (var (code, sessions, price) in packages)
        {
            var ins = connection.CreateCommand();
            ins.CommandText = "INSERT OR IGNORE INTO CoursePackage (type_code, sessions, price) VALUES (@c, @s, @p)";
            ins.Parameters.AddWithValue("@c", code);
            ins.Parameters.AddWithValue("@s", sessions);
            ins.Parameters.AddWithValue("@p", price);
            ins.ExecuteNonQuery();
        }

        System.Diagnostics.Debug.WriteLine("✅ Seeded 7 course types + 20 packages");
    }

    // ========================================================================
    // CourseType CRUD
    // ========================================================================

    public async Task<List<CourseTypeItem>> GetAllTypesAsync()
    {
        var list = new List<CourseTypeItem>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type_code, type_name, type_name_thai FROM CourseType ORDER BY type_code";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CourseTypeItem
            {
                TypeCode = reader.GetString(0),
                TypeName = reader.GetString(1),
                TypeNameThai = reader.IsDBNull(2) ? "" : reader.GetString(2)
            });
        }
        return list;
    }

    public async Task<CourseTypeItem?> GetTypeByCodeAsync(string typeCode)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type_code, type_name, type_name_thai FROM CourseType WHERE type_code = @c";
        cmd.Parameters.AddWithValue("@c", typeCode);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new CourseTypeItem
            {
                TypeCode = reader.GetString(0),
                TypeName = reader.GetString(1),
                TypeNameThai = reader.IsDBNull(2) ? "" : reader.GetString(2)
            };
        }
        return null;
    }

    public async Task<bool> AddTypeAsync(CourseTypeItem item)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO CourseType (type_code, type_name, type_name_thai) VALUES (@c, @n, @t)";
            cmd.Parameters.AddWithValue("@c", item.TypeCode.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@n", item.TypeName);
            cmd.Parameters.AddWithValue("@t", item.TypeNameThai);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AddType: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateTypeAsync(CourseTypeItem item)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE CourseType SET type_name = @n, type_name_thai = @t WHERE type_code = @c";
            cmd.Parameters.AddWithValue("@c", item.TypeCode);
            cmd.Parameters.AddWithValue("@n", item.TypeName);
            cmd.Parameters.AddWithValue("@t", item.TypeNameThai);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ UpdateType: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteTypeAsync(string typeCode)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON";
            await pragma.ExecuteNonQueryAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CourseType WHERE type_code = @c";
            cmd.Parameters.AddWithValue("@c", typeCode);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ DeleteType: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TypeExistsAsync(string typeCode)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM CourseType WHERE type_code = @c";
        cmd.Parameters.AddWithValue("@c", typeCode.ToUpperInvariant());

        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    // ========================================================================
    // CoursePackage CRUD
    // ========================================================================

    public async Task<List<CoursePackageItem>> GetPackagesByTypeAsync(string typeCode)
    {
        var list = new List<CoursePackageItem>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type_code, sessions, price FROM CoursePackage WHERE type_code = @c ORDER BY sessions";
        cmd.Parameters.AddWithValue("@c", typeCode);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CoursePackageItem
            {
                TypeCode = reader.GetString(0),
                Sessions = reader.GetInt32(1),
                Price = reader.GetInt32(2)
            });
        }
        return list;
    }

    public async Task<List<CoursePackageItem>> GetAllPackagesAsync()
    {
        var list = new List<CoursePackageItem>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type_code, sessions, price FROM CoursePackage ORDER BY type_code, sessions";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CoursePackageItem
            {
                TypeCode = reader.GetString(0),
                Sessions = reader.GetInt32(1),
                Price = reader.GetInt32(2)
            });
        }
        return list;
    }

    public async Task<int> GetPriceAsync(string typeCode, int sessions)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT price FROM CoursePackage WHERE type_code = @c AND sessions = @s";
        cmd.Parameters.AddWithValue("@c", typeCode.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@s", sessions);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : -1;
    }

    public async Task<bool> AddPackageAsync(CoursePackageItem item)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO CoursePackage (type_code, sessions, price) VALUES (@c, @s, @p)";
            cmd.Parameters.AddWithValue("@c", item.TypeCode.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@s", item.Sessions);
            cmd.Parameters.AddWithValue("@p", item.Price);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AddPackage: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdatePackagePriceAsync(string typeCode, int sessions, int newPrice)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE CoursePackage SET price = @p WHERE type_code = @c AND sessions = @s";
            cmd.Parameters.AddWithValue("@c", typeCode);
            cmd.Parameters.AddWithValue("@s", sessions);
            cmd.Parameters.AddWithValue("@p", newPrice);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ UpdatePackagePrice: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePackageAsync(string typeCode, int sessions)
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CoursePackage WHERE type_code = @c AND sessions = @s";
            cmd.Parameters.AddWithValue("@c", typeCode);
            cmd.Parameters.AddWithValue("@s", sessions);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ DeletePackage: {ex.Message}");
            return false;
        }
    }
}
