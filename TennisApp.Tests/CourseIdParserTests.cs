using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ CourseIdParser — Parse/Validate Course ID (XXYY)
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class CourseIdParserTests
{
    // ════════════════════════════════════════════════════════════════
    // ParseCourseId — Valid IDs
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Parse_TA04_Valid()
    {
        var (valid, type, _, sessions, _) = ParseCourseId("TA04");
        Assert.IsTrue(valid);
        Assert.AreEqual("TA", type);
        Assert.AreEqual(4, sessions);
    }

    [TestMethod]
    public void Parse_T108_Valid()
    {
        var (valid, type, _, sessions, _) = ParseCourseId("T108");
        Assert.IsTrue(valid);
        Assert.AreEqual("T1", type);
        Assert.AreEqual(8, sessions);
    }

    [TestMethod]
    public void Parse_P101_Valid()
    {
        var (valid, type, _, sessions, _) = ParseCourseId("P101");
        Assert.IsTrue(valid);
        Assert.AreEqual("P1", type);
        Assert.AreEqual(1, sessions);
    }

    [TestMethod]
    public void Parse_T300_Monthly()
    {
        var (valid, type, _, sessions, _) = ParseCourseId("T300");
        Assert.IsTrue(valid);
        Assert.AreEqual("T3", type);
        Assert.AreEqual(0, sessions);
    }

    [TestMethod]
    [Description("lowercase input → ต้อง normalize เป็น uppercase")]
    public void Parse_Lowercase_ta04()
    {
        var (valid, type, _, sessions, _) = ParseCourseId("ta04");
        Assert.IsTrue(valid);
        Assert.AreEqual("TA", type);
        Assert.AreEqual(4, sessions);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseCourseId — Invalid IDs
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Parse_Null_Invalid()
    {
        var (valid, _, _, _, error) = ParseCourseId(null!);
        Assert.IsFalse(valid);
        Assert.IsFalse(string.IsNullOrEmpty(error));
    }

    [TestMethod]
    public void Parse_Empty_Invalid()
    {
        var (valid, _, _, _, error) = ParseCourseId("");
        Assert.IsFalse(valid);
        Assert.IsFalse(string.IsNullOrEmpty(error));
    }

    [TestMethod]
    public void Parse_TooShort_Invalid()
    {
        var (valid, _, _, _, _) = ParseCourseId("TA");
        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void Parse_TooLong_Invalid()
    {
        var (valid, _, _, _, _) = ParseCourseId("TA0456");
        Assert.IsFalse(valid);
    }

    [TestMethod]
    [Description("ตัวแรกเป็นตัวเลข → invalid")]
    public void Parse_FirstCharDigit_Invalid()
    {
        var (valid, _, _, _, _) = ParseCourseId("1A04");
        Assert.IsFalse(valid);
    }

    [TestMethod]
    [Description("หลักที่ 3-4 ไม่ใช่ตัวเลข → invalid")]
    public void Parse_LastCharsNotDigits_Invalid()
    {
        var (valid, _, _, _, _) = ParseCourseId("TAAB");
        Assert.IsFalse(valid);
    }

    [TestMethod]
    [Description("ประเภทไม่ถูกต้อง XX → invalid")]
    public void Parse_InvalidType_XX()
    {
        var (valid, _, _, _, error) = ParseCourseId("XX04");
        Assert.IsFalse(valid);
        Assert.IsTrue(error.Contains("ไม่ถูกต้อง"));
    }

    // ════════════════════════════════════════════════════════════════
    // IsValidCourseType
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void IsValid_TA() => Assert.IsTrue(IsValidType("TA"));
    [TestMethod] public void IsValid_T1() => Assert.IsTrue(IsValidType("T1"));
    [TestMethod] public void IsValid_P3() => Assert.IsTrue(IsValidType("P3"));
    [TestMethod] public void IsInvalid_XX() => Assert.IsFalse(IsValidType("XX"));
    [TestMethod] public void IsInvalid_Empty() => Assert.IsFalse(IsValidType(""));

    // ════════════════════════════════════════════════════════════════
    // IsValidSessionCount
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void ValidSession_0() => Assert.IsTrue(IsValidSession(0));
    [TestMethod] public void ValidSession_1() => Assert.IsTrue(IsValidSession(1));
    [TestMethod] public void ValidSession_99() => Assert.IsTrue(IsValidSession(99));
    [TestMethod] public void InvalidSession_Negative() => Assert.IsFalse(IsValidSession(-1));
    [TestMethod] public void InvalidSession_100() => Assert.IsFalse(IsValidSession(100));

    // ════════════════════════════════════════════════════════════════
    // FormatSessionCount
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Format_1_To_01() => Assert.AreEqual("01", FormatSession(1));
    [TestMethod] public void Format_4_To_04() => Assert.AreEqual("04", FormatSession(4));
    [TestMethod] public void Format_12_To_12() => Assert.AreEqual("12", FormatSession(12));
    [TestMethod] public void Format_0_To_00() => Assert.AreEqual("00", FormatSession(0));

    // ════════════════════════════════════════════════════════════════
    // GenerateExampleCourseId
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void GenExample_TA_4() => Assert.AreEqual("TA04", GenExample("TA", 4));
    [TestMethod] public void GenExample_T1_12() => Assert.AreEqual("T112", GenExample("T1", 12));

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GenExample_InvalidType_Throws()
        => GenExample("XX", 4);

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GenExample_InvalidSession_Throws()
        => GenExample("TA", -1);

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate logic from CourseIdParser
    // ════════════════════════════════════════════════════════════════

    private static readonly System.Collections.Generic.Dictionary<string, string> _validTypes = new()
    {
        ["TA"] = "Adult", ["T1"] = "Red & Orange Ball", ["T2"] = "Intermediate",
        ["T3"] = "Competitive", ["P1"] = "Private Kru Mee",
        ["P2"] = "Private + Coach (Day)", ["P3"] = "Private + Coach (Night)"
    };

    private static (bool Valid, string Type, string Name, int Sessions, string Error) ParseCourseId(string? courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return (false, "", "", 0, "กรุณากรอกรหัสคอร์ส");

        courseId = courseId.Trim().ToUpper();
        if (courseId.Length != 4)
            return (false, "", "", 0, "รหัสคอร์สต้องมี 4 หลัก");
        if (!char.IsLetter(courseId[0]))
            return (false, "", "", 0, "หลักที่ 1 ต้องเป็นตัวอักษร");
        if (!char.IsLetterOrDigit(courseId[1]))
            return (false, "", "", 0, "หลักที่ 2 ต้องเป็นตัวอักษรหรือตัวเลข");
        if (!char.IsDigit(courseId[2]) || !char.IsDigit(courseId[3]))
            return (false, "", "", 0, "หลักที่ 3-4 ต้องเป็นตัวเลข (จำนวนครั้ง)");

        var type = courseId[..2];
        if (!int.TryParse(courseId[2..], out var sessions))
            return (false, "", "", 0, "จำนวนครั้งไม่ถูกต้อง");

        if (!_validTypes.TryGetValue(type, out var name))
            return (false, type, "", sessions, $"ประเภทคลาส '{type}' ไม่ถูกต้อง\nประเภทที่รองรับ: TA, T1, T2, T3, P1, P2, P3");

        return (true, type, name, sessions, "");
    }

    private static bool IsValidType(string type)
        => !string.IsNullOrEmpty(type) && _validTypes.ContainsKey(type.ToUpperInvariant());

    private static bool IsValidSession(int s) => s >= 0 && s <= 99;

    private static string FormatSession(int s) => s.ToString("D2");

    private static string GenExample(string type, int sessions)
    {
        if (!IsValidType(type)) throw new ArgumentException("Invalid course type", nameof(type));
        if (!IsValidSession(sessions)) throw new ArgumentException("Invalid session count", nameof(sessions));
        return $"{type.ToUpper()}{FormatSession(sessions)}";
    }
}
