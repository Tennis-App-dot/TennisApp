using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TennisApp.Models;

namespace TennisApp.Helpers;

/// <summary>
/// Price lookup สำหรับคอร์ส — ดึงจาก DB (CourseType + CoursePackage) เป็นหลัก
/// fallback เป็น hardcoded dictionary ถ้า DB ยังไม่พร้อม
/// </summary>
public static class CoursePricingHelper
{
    // ═══════════════════════════════════════════════════════════════
    // Hardcoded Defaults (fallback — ใช้ตอน DB ยังไม่พร้อม)
    // ═══════════════════════════════════════════════════════════════

    public static readonly Dictionary<string, string> CourseTypeNames = new()
    {
        ["TA"] = "Adult",
        ["T1"] = "Red & Orange Ball",
        ["T2"] = "Intermediate",
        ["T3"] = "Competitive",
        ["P1"] = "Private Kru Mee",
        ["P2"] = "Private + Coach (Day)",
        ["P3"] = "Private + Coach (Night)"
    };

    public static readonly Dictionary<string, string> CourseTypeDisplayNames = new()
    {
        ["TA"] = "TA — Adult (ผู้ใหญ่)",
        ["T1"] = "T1 — Red & Orange Ball (เด็กเล็ก)",
        ["T2"] = "T2 — Intermediate (ระดับกลาง)",
        ["T3"] = "T3 — Competitive (แข่งขัน)",
        ["P1"] = "P1 — Private Kru Mee",
        ["P2"] = "P2 — Private + Coach (กลางวัน)",
        ["P3"] = "P3 — Private + Coach (กลางคืน)"
    };

    public static readonly Dictionary<string, int[]> ValidSessions = new()
    {
        ["TA"] = [1, 4, 8],
        ["T1"] = [1, 4, 8, 12],
        ["T2"] = [1, 4, 8, 12, 16],
        ["T3"] = [1, 8, 12, 16, 0],
        ["P1"] = [1],
        ["P2"] = [1],
        ["P3"] = [1]
    };

    private static readonly Dictionary<(string CourseType, int Sessions), int> PriceTable = new()
    {
        [("TA", 1)]  = 600,   [("TA", 4)]  = 2_200, [("TA", 8)]  = 4_000,
        [("T1", 1)]  = 600,   [("T1", 4)]  = 1_800, [("T1", 8)]  = 3_200, [("T1", 12)] = 4_500,
        [("T2", 1)]  = 800,   [("T2", 4)]  = 3_000, [("T2", 8)]  = 4_800, [("T2", 12)] = 6_600, [("T2", 16)] = 8_000,
        [("T3", 1)]  = 900,   [("T3", 8)]  = 6_500, [("T3", 12)] = 8_500, [("T3", 16)] = 11_500, [("T3", 0)]  = 13_000,
        [("P1", 1)]  = 2_500,
        [("P2", 1)]  = 950,
        [("P3", 1)]  = 1_050
    };

    // ═══════════════════════════════════════════════════════════════
    // In-memory cache (loaded from DB at startup)
    // ═══════════════════════════════════════════════════════════════

    private static Dictionary<string, CourseTypeItem>? _cachedTypes;
    private static Dictionary<(string, int), int>? _cachedPrices;
    private static Dictionary<string, int[]>? _cachedSessions;

    /// <summary>
    /// โหลดข้อมูลจาก DB เข้า cache (เรียกครั้งเดียวตอน startup)
    /// </summary>
    public static async Task LoadFromDatabaseAsync(Services.DatabaseService dbService)
    {
        try
        {
            var types = await dbService.CourseTypes.GetAllTypesAsync();
            var packages = await dbService.CourseTypes.GetAllPackagesAsync();

            if (types.Count == 0) return; // ยังไม่มีข้อมูลใน DB → ใช้ hardcoded

            _cachedTypes = types.ToDictionary(t => t.TypeCode.ToUpperInvariant());

            _cachedPrices = packages.ToDictionary(
                p => (p.TypeCode.ToUpperInvariant(), p.Sessions),
                p => p.Price);

            _cachedSessions = packages
                .GroupBy(p => p.TypeCode.ToUpperInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Sessions).OrderBy(s => s).ToArray());

            // อัปเดต hardcoded dictionaries ด้วย (เพื่อให้ sync methods ใช้ได้)
            foreach (var t in types)
            {
                var code = t.TypeCode.ToUpperInvariant();
                CourseTypeNames[code] = t.TypeName;
                CourseTypeDisplayNames[code] = t.DisplayName;
            }

            foreach (var p in packages)
            {
                PriceTable[(p.TypeCode.ToUpperInvariant(), p.Sessions)] = p.Price;
            }

            // อัปเดต ValidSessions
            foreach (var kvp in _cachedSessions)
            {
                ValidSessions[kvp.Key] = kvp.Value;
            }

            System.Diagnostics.Debug.WriteLine($"✅ CoursePricingHelper: Loaded {types.Count} types + {packages.Count} packages from DB");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ CoursePricingHelper.LoadFromDatabaseAsync: {ex.Message} — using hardcoded defaults");
        }
    }

    /// <summary>
    /// ล้าง cache (เรียกหลังจากเพิ่ม/แก้/ลบประเภทคอร์สหรือแพ็กเกจ)
    /// </summary>
    public static void InvalidateCache()
    {
        _cachedTypes = null;
        _cachedPrices = null;
        _cachedSessions = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Public Methods (ใช้ cache ถ้ามี, fallback hardcoded)
    // ═══════════════════════════════════════════════════════════════

    public static int GetPrice(string courseType, int sessions)
    {
        var key = (courseType.ToUpperInvariant(), sessions);
        if (_cachedPrices != null && _cachedPrices.TryGetValue(key, out var cachedPrice))
            return cachedPrice;
        return PriceTable.TryGetValue(key, out var price) ? price : -1;
    }

    public static int GetPriceByClassId(string classId)
    {
        var (courseType, sessions) = ParseClassId(classId);
        if (courseType == null) return -1;
        return GetPrice(courseType, sessions);
    }

    public static int[] GetValidSessions(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        if (_cachedSessions != null && _cachedSessions.TryGetValue(key, out var cachedSessions))
            return cachedSessions;
        return ValidSessions.TryGetValue(key, out var sessions) ? sessions : [];
    }

    public static string[] GetAllCourseTypes()
    {
        if (_cachedTypes != null)
            return [.. _cachedTypes.Keys];
        return [.. CourseTypeNames.Keys];
    }

    public static string GetCourseName(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        if (_cachedTypes != null && _cachedTypes.TryGetValue(key, out var item))
            return item.TypeName;
        return CourseTypeNames.TryGetValue(key, out var name) ? name : "Unknown";
    }

    public static string GetCourseDisplayName(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        if (_cachedTypes != null && _cachedTypes.TryGetValue(key, out var item))
            return item.DisplayName;
        return CourseTypeDisplayNames.TryGetValue(key, out var name) ? name : courseType;
    }

    public static bool IsValidCourseType(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        if (_cachedTypes != null)
            return _cachedTypes.ContainsKey(key);
        return CourseTypeNames.ContainsKey(key);
    }

    public static bool IsValidSession(string courseType, int sessions)
    {
        var validSessions = GetValidSessions(courseType);
        return validSessions.Contains(sessions);
    }

    public static string GenerateClassId(string courseType, int sessions)
    {
        return $"{courseType.ToUpperInvariant()}{sessions:D2}";
    }

    public static (string? CourseType, int Sessions) ParseClassId(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId) || classId.Length != 4)
            return (null, -1);

        var courseType = classId[..2].ToUpperInvariant();
        var sessionStr = classId[2..];

        if (!int.TryParse(sessionStr, out var sessions))
            return (null, -1);

        if (!IsValidCourseType(courseType))
            return (null, -1);

        return (courseType, sessions);
    }

    public static string GetSessionDisplayText(int sessions)
    {
        return sessions switch
        {
            0 => "รายเดือน",
            1 => "ครั้งละ",
            _ => $"{sessions} ครั้ง"
        };
    }

    public static List<(int Sessions, string DisplayText, int Price)> GetPackages(string courseType)
    {
        var validSessions = GetValidSessions(courseType);
        var packages = new List<(int, string, int)>();

        foreach (var sessions in validSessions)
        {
            var price = GetPrice(courseType, sessions);
            if (price > 0)
            {
                packages.Add((sessions, GetSessionDisplayText(sessions), price));
            }
        }

        return packages;
    }

    public static List<(string CourseType, string CourseName, int Sessions, string SessionText, int Price)> GetAllPackages()
    {
        var allPackages = new List<(string, string, int, string, int)>();

        foreach (var courseType in GetAllCourseTypes())
        {
            var courseName = GetCourseName(courseType);
            var packages = GetPackages(courseType);

            foreach (var (sessions, displayText, price) in packages)
            {
                allPackages.Add((courseType, courseName, sessions, displayText, price));
            }
        }

        return allPackages;
    }
}
