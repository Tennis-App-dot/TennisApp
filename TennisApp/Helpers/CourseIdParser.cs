using System;

namespace TennisApp.Helpers;

/// <summary>
/// Helper class for parsing and validating course IDs
/// Course ID format: XXYY where XX = course type (2 letters), YY = session count (2 digits)
/// Examples: TA01, T104, P201
/// </summary>
public static class CourseIdParser
{
    /// <summary>
    /// Parse course ID and extract course information
    /// </summary>
    public static (bool IsValid, string CourseType, string CourseName, int SessionCount, string ErrorMessage) ParseCourseId(string courseId)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(courseId))
        {
            return (false, "", "", 0, "กรุณากรอกรหัสคอร์ส");
        }

        courseId = courseId.Trim().ToUpper();

        // Check length (must be exactly 4 characters)
        if (courseId.Length != 4)
        {
            return (false, "", "", 0, "รหัสคอร์สต้องมี 4 หลัก");
        }

        // Check first character is letter
        if (!char.IsLetter(courseId[0]))
        {
            return (false, "", "", 0, "หลักที่ 1 ต้องเป็นตัวอักษร");
        }

        // Check second character is letter or digit (for TA, T1, P2, etc.)
        if (!char.IsLetterOrDigit(courseId[1]))
        {
            return (false, "", "", 0, "หลักที่ 2 ต้องเป็นตัวอักษรหรือตัวเลข");
        }

        // Check last 2 characters are digits
        if (!char.IsDigit(courseId[2]) || !char.IsDigit(courseId[3]))
        {
            return (false, "", "", 0, "หลักที่ 3-4 ต้องเป็นตัวเลข (จำนวนครั้ง)");
        }

        // Extract course type and session count
        string courseType = courseId.Substring(0, 2);
        string sessionStr = courseId.Substring(2, 2);
        
        if (!int.TryParse(sessionStr, out int sessionCount))
        {
            return (false, "", "", 0, "จำนวนครั้งไม่ถูกต้อง");
        }

        // Validate session count (must be between 1 and 99)
        if (sessionCount < 1 || sessionCount > 99)
        {
            return (false, courseType, "", sessionCount, $"จำนวนครั้งต้องอยู่ระหว่าง 01-99 (ได้รับ: {sessionCount:D2})");
        }

        // Get course name from type
        string courseName = GetCourseName(courseType);
        
        if (string.IsNullOrEmpty(courseName))
        {
            return (false, courseType, "", sessionCount, $"ประเภทคลาส '{courseType}' ไม่ถูกต้อง\nประเภทที่รองรับ: TA, T1, T2, T3, P1, P2, P3");
        }

        return (true, courseType, courseName, sessionCount, "");
    }

    /// <summary>
    /// Get Thai course name from course type code
    /// </summary>
    public static string GetCourseName(string courseType)
    {
        return courseType.ToUpper() switch
        {
            "TA" => "Adult Class",
            "T1" => "Kids Class",
            "T2" => "Intermediate Class",
            "T3" => "Competitive Class",
            "P1" => "Private & Master Coach",
            "P2" => "Private & Standard Coach (Day time)",
            "P3" => "Private & Standard Coach (Night time)",
            _ => ""
        };
    }

    /// <summary>
    /// Get full description of course type
    /// </summary>
    public static string GetCourseTypeDescription(string courseType)
    {
        return courseType.ToUpper() switch
        {
            "TA" => "คลาสผู้ใหญ่ (Adult Class)",
            "T1" => "คลาสเด็ก (Kids Class)",
            "T2" => "คลาสระดับกลาง (Intermediate Class)",
            "T3" => "คลาสแข่งขัน (Competitive Class)",
            "P1" => "ส่วนตัวกับโค้ชระดับมาสเตอร์ (Private & Master Coach)",
            "P2" => "ส่วนตัวกับโค้ชมาตรฐาน กลางวัน (Private & Standard Coach - Day)",
            "P3" => "ส่วนตัวกับโค้ชมาตรฐาน กลางคืน (Private & Standard Coach - Night)",
            _ => "ไม่ทราบประเภท"
        };
    }

    /// <summary>
    /// Validate if course type is valid
    /// </summary>
    public static bool IsValidCourseType(string courseType)
    {
        var validTypes = new[] { "TA", "T1", "T2", "T3", "P1", "P2", "P3" };
        return Array.Exists(validTypes, t => t.Equals(courseType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validate if session count is valid (1-99)
    /// </summary>
    public static bool IsValidSessionCount(int sessionCount)
    {
        return sessionCount >= 1 && sessionCount <= 99;
    }

    /// <summary>
    /// Get all valid course types
    /// </summary>
    public static string[] GetValidCourseTypes()
    {
        return new[] { "TA", "T1", "T2", "T3", "P1", "P2", "P3" };
    }

    /// <summary>
    /// Get all valid session counts (common options for dropdown)
    /// </summary>
    public static int[] GetValidSessionCounts()
    {
        return new[] { 1, 4, 8, 12, 16, 20, 24 };
    }

    /// <summary>
    /// Format session count to 2 digits (e.g., 1 -> "01", 12 -> "12")
    /// </summary>
    public static string FormatSessionCount(int sessionCount)
    {
        return sessionCount.ToString("D2");
    }

    /// <summary>
    /// Generate example course ID
    /// </summary>
    public static string GenerateExampleCourseId(string courseType, int sessionCount)
    {
        if (!IsValidCourseType(courseType))
        {
            throw new ArgumentException("Invalid course type", nameof(courseType));
        }

        if (!IsValidSessionCount(sessionCount))
        {
            throw new ArgumentException("Invalid session count", nameof(sessionCount));
        }

        return $"{courseType.ToUpper()}{FormatSessionCount(sessionCount)}";
    }
}
