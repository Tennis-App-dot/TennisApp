using System;
using System.Collections.Generic;
using System.Linq;

namespace TennisApp.Helpers;

/// <summary>
/// Static price lookup ตามตาราง Talent Tennis Academy Fee &amp; Tickets
/// ราคาตายตัว — ไม่ต้องเก็บใน DB
/// </summary>
public static class CoursePricingHelper
{
    // ═══════════════════════════════════════════════════════════════
    // Course Type Definitions
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

    // ═══════════════════════════════════════════════════════════════
    // Session Options per Course Type
    // Key = course type, Value = list of valid session counts
    // 1 = per time (ครั้งละ), 0 = monthly (รายเดือน)
    // ═══════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════
    // Price Table (course_type + sessions → price)
    // ═══════════════════════════════════════════════════════════════

    private static readonly Dictionary<(string CourseType, int Sessions), int> PriceTable = new()
    {
        // TA — Adult
        [("TA", 1)]  = 600,
        [("TA", 4)]  = 2_200,
        [("TA", 8)]  = 4_000,

        // T1 — Red & Orange Ball
        [("T1", 1)]  = 600,
        [("T1", 4)]  = 1_800,
        [("T1", 8)]  = 3_200,
        [("T1", 12)] = 4_500,

        // T2 — Intermediate
        [("T2", 1)]  = 800,
        [("T2", 4)]  = 3_000,
        [("T2", 8)]  = 4_800,
        [("T2", 12)] = 6_600,
        [("T2", 16)] = 8_000,

        // T3 — Competitive
        [("T3", 1)]  = 900,
        [("T3", 8)]  = 6_500,
        [("T3", 12)] = 8_500,
        [("T3", 16)] = 11_500,
        [("T3", 0)]  = 13_000,   // 0 = รายเดือน

        // P1 — Private Kru Mee
        [("P1", 1)]  = 2_500,

        // P2 — Private + Coach (Day 06:00-17:00)
        [("P2", 1)]  = 950,

        // P3 — Private + Coach (Night 18:00-21:00)
        [("P3", 1)]  = 1_050
    };

    // ═══════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// ดึงราคาจากประเภทคอร์ส + จำนวนครั้ง
    /// </summary>
    /// <returns>ราคา หรือ -1 ถ้าไม่พบ</returns>
    public static int GetPrice(string courseType, int sessions)
    {
        var key = (courseType.ToUpperInvariant(), sessions);
        return PriceTable.TryGetValue(key, out var price) ? price : -1;
    }

    /// <summary>
    /// ดึงราคาจาก class_id เช่น "TA04" → ฿2,200
    /// </summary>
    /// <returns>ราคา หรือ -1 ถ้าไม่พบ</returns>
    public static int GetPriceByClassId(string classId)
    {
        var (courseType, sessions) = ParseClassId(classId);
        if (courseType == null) return -1;
        return GetPrice(courseType, sessions);
    }

    /// <summary>
    /// ดึงจำนวนครั้งที่เปิดให้ซื้อ สำหรับประเภทนั้น
    /// </summary>
    public static int[] GetValidSessions(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        return ValidSessions.TryGetValue(key, out var sessions) ? sessions : [];
    }

    /// <summary>
    /// ดึงประเภทคอร์สทั้งหมด
    /// </summary>
    public static string[] GetAllCourseTypes()
    {
        return [.. CourseTypeNames.Keys];
    }

    /// <summary>
    /// ดึงชื่อคอร์สจากประเภท เช่น "TA" → "Adult"
    /// </summary>
    public static string GetCourseName(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        return CourseTypeNames.TryGetValue(key, out var name) ? name : "Unknown";
    }

    /// <summary>
    /// ดึงชื่อแสดงผลจากประเภท เช่น "TA" → "TA — Adult (ผู้ใหญ่)"
    /// </summary>
    public static string GetCourseDisplayName(string courseType)
    {
        var key = courseType.ToUpperInvariant();
        return CourseTypeDisplayNames.TryGetValue(key, out var name) ? name : courseType;
    }

    /// <summary>
    /// ตรวจสอบว่าประเภทคอร์สถูกต้องหรือไม่
    /// </summary>
    public static bool IsValidCourseType(string courseType)
    {
        return CourseTypeNames.ContainsKey(courseType.ToUpperInvariant());
    }

    /// <summary>
    /// ตรวจสอบว่าจำนวนครั้งถูกต้องสำหรับประเภทนั้นหรือไม่
    /// </summary>
    public static bool IsValidSession(string courseType, int sessions)
    {
        var validSessions = GetValidSessions(courseType);
        return validSessions.Contains(sessions);
    }

    /// <summary>
    /// สร้าง class_id จากประเภท + จำนวนครั้ง
    /// เช่น ("TA", 4) → "TA04", ("T3", 0) → "T300"
    /// </summary>
    public static string GenerateClassId(string courseType, int sessions)
    {
        return $"{courseType.ToUpperInvariant()}{sessions:D2}";
    }

    /// <summary>
    /// Parse class_id → (courseType, sessions)
    /// เช่น "TA04" → ("TA", 4), "T300" → ("T3", 0)
    /// </summary>
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

    /// <summary>
    /// แสดงข้อความจำนวนครั้ง เช่น 4 → "4 ครั้ง", 1 → "ครั้งละ", 0 → "รายเดือน"
    /// </summary>
    public static string GetSessionDisplayText(int sessions)
    {
        return sessions switch
        {
            0 => "รายเดือน",
            1 => "ครั้งละ",
            _ => $"{sessions} ครั้ง"
        };
    }

    /// <summary>
    /// ดึงข้อมูลแพ็กเกจทั้งหมดของประเภทนั้น (สำหรับแสดงใน UI)
    /// </summary>
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

    /// <summary>
    /// ดึงแพ็กเกจทั้งหมดทุกประเภท (20 แพ็กเกจ)
    /// </summary>
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
