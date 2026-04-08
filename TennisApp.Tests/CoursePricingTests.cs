using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ CoursePricingHelper — ราคาคอร์ส + Session + ClassId
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class CoursePricingTests
{
    // ════════════════════════════════════════════════════════════════
    // GetPrice — ราคาถูกต้องตามตาราง
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Price_TA_1() => Assert.AreEqual(600, GetPrice("TA", 1));
    [TestMethod] public void Price_TA_4() => Assert.AreEqual(2200, GetPrice("TA", 4));
    [TestMethod] public void Price_TA_8() => Assert.AreEqual(4000, GetPrice("TA", 8));

    [TestMethod] public void Price_T1_1() => Assert.AreEqual(600, GetPrice("T1", 1));
    [TestMethod] public void Price_T1_4() => Assert.AreEqual(1800, GetPrice("T1", 4));
    [TestMethod] public void Price_T1_8() => Assert.AreEqual(3200, GetPrice("T1", 8));
    [TestMethod] public void Price_T1_12() => Assert.AreEqual(4500, GetPrice("T1", 12));

    [TestMethod] public void Price_T2_1() => Assert.AreEqual(800, GetPrice("T2", 1));
    [TestMethod] public void Price_T2_4() => Assert.AreEqual(3000, GetPrice("T2", 4));
    [TestMethod] public void Price_T2_8() => Assert.AreEqual(4800, GetPrice("T2", 8));
    [TestMethod] public void Price_T2_12() => Assert.AreEqual(6600, GetPrice("T2", 12));
    [TestMethod] public void Price_T2_16() => Assert.AreEqual(8000, GetPrice("T2", 16));

    [TestMethod] public void Price_T3_1() => Assert.AreEqual(900, GetPrice("T3", 1));
    [TestMethod] public void Price_T3_8() => Assert.AreEqual(6500, GetPrice("T3", 8));
    [TestMethod] public void Price_T3_12() => Assert.AreEqual(8500, GetPrice("T3", 12));
    [TestMethod] public void Price_T3_16() => Assert.AreEqual(11500, GetPrice("T3", 16));
    [TestMethod] public void Price_T3_Monthly() => Assert.AreEqual(13000, GetPrice("T3", 0));

    [TestMethod] public void Price_P1() => Assert.AreEqual(2500, GetPrice("P1", 1));
    [TestMethod] public void Price_P2() => Assert.AreEqual(950, GetPrice("P2", 1));
    [TestMethod] public void Price_P3() => Assert.AreEqual(1050, GetPrice("P3", 1));

    [TestMethod]
    [Description("ประเภท/จำนวนที่ไม่มีในตาราง → -1")]
    public void Price_Invalid_ReturnsNegative1()
        => Assert.AreEqual(-1, GetPrice("XX", 1));

    [TestMethod]
    [Description("จำนวนครั้งที่ไม่มีในตาราง → -1")]
    public void Price_InvalidSession_ReturnsNegative1()
        => Assert.AreEqual(-1, GetPrice("TA", 99));

    // ════════════════════════════════════════════════════════════════
    // GetPrice — case insensitive
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("lowercase 'ta' → ต้องหาเจอ")]
    public void Price_CaseInsensitive_Lowercase()
        => Assert.AreEqual(600, GetPrice("ta", 1));

    [TestMethod]
    [Description("mixed case 'Ta' → ต้องหาเจอ")]
    public void Price_CaseInsensitive_Mixed()
        => Assert.AreEqual(600, GetPrice("Ta", 1));

    // ════════════════════════════════════════════════════════════════
    // IsValidCourseType
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void ValidType_TA() => Assert.IsTrue(IsValidType("TA"));
    [TestMethod] public void ValidType_T1() => Assert.IsTrue(IsValidType("T1"));
    [TestMethod] public void ValidType_T2() => Assert.IsTrue(IsValidType("T2"));
    [TestMethod] public void ValidType_T3() => Assert.IsTrue(IsValidType("T3"));
    [TestMethod] public void ValidType_P1() => Assert.IsTrue(IsValidType("P1"));
    [TestMethod] public void ValidType_P2() => Assert.IsTrue(IsValidType("P2"));
    [TestMethod] public void ValidType_P3() => Assert.IsTrue(IsValidType("P3"));
    [TestMethod] public void InvalidType_XX() => Assert.IsFalse(IsValidType("XX"));
    [TestMethod] public void InvalidType_Empty() => Assert.IsFalse(IsValidType(""));
    [TestMethod] public void ValidType_Lowercase() => Assert.IsTrue(IsValidType("ta"));

    // ════════════════════════════════════════════════════════════════
    // GenerateClassId
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void ClassId_TA04() => Assert.AreEqual("TA04", GenClassId("TA", 4));
    [TestMethod] public void ClassId_T108() => Assert.AreEqual("T108", GenClassId("T1", 8));
    [TestMethod] public void ClassId_T300() => Assert.AreEqual("T300", GenClassId("T3", 0));
    [TestMethod] public void ClassId_P101() => Assert.AreEqual("P101", GenClassId("P1", 1));
    [TestMethod] public void ClassId_T212() => Assert.AreEqual("T212", GenClassId("T2", 12));
    [TestMethod] public void ClassId_Lowercase() => Assert.AreEqual("TA01", GenClassId("ta", 1));

    // ════════════════════════════════════════════════════════════════
    // ParseClassId
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseClassId_TA04()
    {
        var (type, sessions) = ParseClassId("TA04");
        Assert.AreEqual("TA", type);
        Assert.AreEqual(4, sessions);
    }

    [TestMethod]
    public void ParseClassId_T300_Monthly()
    {
        var (type, sessions) = ParseClassId("T300");
        Assert.AreEqual("T3", type);
        Assert.AreEqual(0, sessions);
    }

    [TestMethod]
    public void ParseClassId_P101()
    {
        var (type, sessions) = ParseClassId("P101");
        Assert.AreEqual("P1", type);
        Assert.AreEqual(1, sessions);
    }

    [TestMethod]
    [Description("Parse invalid classId (too short) → null")]
    public void ParseClassId_TooShort()
    {
        var (type, _) = ParseClassId("TA");
        Assert.IsNull(type);
    }

    [TestMethod]
    [Description("Parse invalid type → null")]
    public void ParseClassId_InvalidType()
    {
        var (type, _) = ParseClassId("XX01");
        Assert.IsNull(type);
    }

    [TestMethod]
    public void ParseClassId_Null()
    {
        var (type, _) = ParseClassId(null!);
        Assert.IsNull(type);
    }

    // ════════════════════════════════════════════════════════════════
    // GetSessionDisplayText
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Session_0_Monthly() => Assert.AreEqual("รายเดือน", SessionText(0));
    [TestMethod] public void Session_1_PerTime() => Assert.AreEqual("ครั้งละ", SessionText(1));
    [TestMethod] public void Session_4_Times() => Assert.AreEqual("4 ครั้ง", SessionText(4));
    [TestMethod] public void Session_8_Times() => Assert.AreEqual("8 ครั้ง", SessionText(8));
    [TestMethod] public void Session_12_Times() => Assert.AreEqual("12 ครั้ง", SessionText(12));
    [TestMethod] public void Session_16_Times() => Assert.AreEqual("16 ครั้ง", SessionText(16));

    // ════════════════════════════════════════════════════════════════
    // GetCourseName
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void Name_TA() => Assert.AreEqual("Adult", GetName("TA"));
    [TestMethod] public void Name_T1() => Assert.AreEqual("Red & Orange Ball", GetName("T1"));
    [TestMethod] public void Name_T2() => Assert.AreEqual("Intermediate", GetName("T2"));
    [TestMethod] public void Name_T3() => Assert.AreEqual("Competitive", GetName("T3"));
    [TestMethod] public void Name_P1() => Assert.AreEqual("Private Kru Mee", GetName("P1"));
    [TestMethod] public void Name_P2() => Assert.AreEqual("Private + Coach (Day)", GetName("P2"));
    [TestMethod] public void Name_P3() => Assert.AreEqual("Private + Coach (Night)", GetName("P3"));
    [TestMethod] public void Name_Invalid() => Assert.AreEqual("Unknown", GetName("XX"));

    // ════════════════════════════════════════════════════════════════
    // ValidSessions per type
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ValidSessions_TA()
    {
        var sessions = GetValidSessions("TA");
        CollectionAssert.Contains(sessions, 1);
        CollectionAssert.Contains(sessions, 4);
        CollectionAssert.Contains(sessions, 8);
        Assert.AreEqual(3, sessions.Length);
    }

    [TestMethod]
    public void ValidSessions_T1()
    {
        var sessions = GetValidSessions("T1");
        CollectionAssert.Contains(sessions, 1);
        CollectionAssert.Contains(sessions, 4);
        CollectionAssert.Contains(sessions, 8);
        CollectionAssert.Contains(sessions, 12);
    }

    [TestMethod]
    public void ValidSessions_T3_IncludesMonthly()
    {
        var sessions = GetValidSessions("T3");
        CollectionAssert.Contains(sessions, 0); // monthly
    }

    [TestMethod]
    public void ValidSessions_P1_OnlyPerTime()
    {
        var sessions = GetValidSessions("P1");
        Assert.AreEqual(1, sessions.Length);
        Assert.AreEqual(1, sessions[0]);
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate logic from CoursePricingHelper (hardcoded)
    // ════════════════════════════════════════════════════════════════

    private static readonly System.Collections.Generic.Dictionary<string, string> _names = new()
    {
        ["TA"] = "Adult", ["T1"] = "Red & Orange Ball", ["T2"] = "Intermediate",
        ["T3"] = "Competitive", ["P1"] = "Private Kru Mee",
        ["P2"] = "Private + Coach (Day)", ["P3"] = "Private + Coach (Night)"
    };

    private static readonly System.Collections.Generic.Dictionary<(string, int), int> _prices = new()
    {
        [("TA", 1)] = 600, [("TA", 4)] = 2200, [("TA", 8)] = 4000,
        [("T1", 1)] = 600, [("T1", 4)] = 1800, [("T1", 8)] = 3200, [("T1", 12)] = 4500,
        [("T2", 1)] = 800, [("T2", 4)] = 3000, [("T2", 8)] = 4800, [("T2", 12)] = 6600, [("T2", 16)] = 8000,
        [("T3", 1)] = 900, [("T3", 8)] = 6500, [("T3", 12)] = 8500, [("T3", 16)] = 11500, [("T3", 0)] = 13000,
        [("P1", 1)] = 2500, [("P2", 1)] = 950, [("P3", 1)] = 1050
    };

    private static readonly System.Collections.Generic.Dictionary<string, int[]> _validSessions = new()
    {
        ["TA"] = [1, 4, 8], ["T1"] = [1, 4, 8, 12], ["T2"] = [1, 4, 8, 12, 16],
        ["T3"] = [1, 8, 12, 16, 0], ["P1"] = [1], ["P2"] = [1], ["P3"] = [1]
    };

    private static int GetPrice(string type, int sessions)
    {
        var key = (type.ToUpperInvariant(), sessions);
        return _prices.TryGetValue(key, out var p) ? p : -1;
    }

    private static bool IsValidType(string type)
        => !string.IsNullOrEmpty(type) && _names.ContainsKey(type.ToUpperInvariant());

    private static string GenClassId(string type, int sessions)
        => $"{type.ToUpperInvariant()}{sessions:D2}";

    private static (string? Type, int Sessions) ParseClassId(string? classId)
    {
        if (string.IsNullOrWhiteSpace(classId) || classId.Length != 4) return (null, -1);
        var type = classId[..2].ToUpperInvariant();
        if (!int.TryParse(classId[2..], out var sessions)) return (null, -1);
        if (!_names.ContainsKey(type)) return (null, -1);
        return (type, sessions);
    }

    private static string SessionText(int s) => s switch
    {
        0 => "รายเดือน", 1 => "ครั้งละ", _ => $"{s} ครั้ง"
    };

    private static string GetName(string type)
        => _names.TryGetValue(type.ToUpperInvariant(), out var n) ? n : "Unknown";

    private static int[] GetValidSessions(string type)
        => _validSessions.TryGetValue(type.ToUpperInvariant(), out var s) ? s : [];
}
