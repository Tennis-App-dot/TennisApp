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

        // Validate session count (must be between 0 and 99; 0 = monthly)
        if (sessionCount < 0 || sessionCount > 99)
        {
            return (false, courseType, "", sessionCount, $"จำนวนครั้งต้องอยู่ระหว่าง 00-99 (ได้รับ: {sessionCount:D2})");
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
        // ใช้ CoursePricingHelper (ดึงจาก DB cache) เป็นหลัก
        var name = CoursePricingHelper.GetCourseName(courseType);
        return name != "Unknown" ? name : "";
    }

    /// <summary>
    /// Get full description of course type
    /// </summary>
    public static string GetCourseTypeDescription(string courseType)
    {
        var displayName = CoursePricingHelper.GetCourseDisplayName(courseType);
        return displayName != courseType ? displayName : "ไม่ทราบประเภท";
    }

    /// <summary>
    /// Validate if course type is valid
    /// </summary>
    public static bool IsValidCourseType(string courseType)
    {
        return CoursePricingHelper.IsValidCourseType(courseType);
    }

    /// <summary>
    /// Validate if session count is valid (0-99; 0 = monthly)
    /// </summary>
    public static bool IsValidSessionCount(int sessionCount)
    {
        return sessionCount >= 0 && sessionCount <= 99;
    }

    /// <summary>
    /// Get all valid course types
    /// </summary>
    public static string[] GetValidCourseTypes()
    {
        return CoursePricingHelper.GetAllCourseTypes();
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
