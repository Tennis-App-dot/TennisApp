using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ CourseItem / CourseKey — Parse, Validate, Display
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class CourseItemTests
{
    // ════════════════════════════════════════════════════════════════
    // CourseKey — Parse / ToString
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CourseKey_ToString()
    {
        var key = new TestCourseKey("TA04", "220250001");
        Assert.AreEqual("TA04|220250001", key.ToString());
    }

    [TestMethod]
    public void CourseKey_Parse_Valid()
    {
        var key = TestCourseKey.Parse("TA04|220250001");
        Assert.IsNotNull(key);
        Assert.AreEqual("TA04", key.ClassId);
        Assert.AreEqual("220250001", key.TrainerId);
    }

    [TestMethod]
    public void CourseKey_Parse_Null()
    {
        var key = TestCourseKey.Parse(null);
        Assert.IsNull(key);
    }

    [TestMethod]
    public void CourseKey_Parse_Empty()
    {
        var key = TestCourseKey.Parse("");
        Assert.IsNull(key);
    }

    [TestMethod]
    public void CourseKey_Parse_NoPipe()
    {
        var key = TestCourseKey.Parse("TA04220250001");
        Assert.IsNull(key);
    }

    [TestMethod]
    public void CourseKey_Equality()
    {
        var k1 = new TestCourseKey("TA04", "T001");
        var k2 = new TestCourseKey("TA04", "T001");
        Assert.AreEqual(k1, k2);
    }

    [TestMethod]
    public void CourseKey_Inequality()
    {
        var k1 = new TestCourseKey("TA04", "T001");
        var k2 = new TestCourseKey("TA04", "T002");
        Assert.AreNotEqual(k1, k2);
    }

    // ════════════════════════════════════════════════════════════════
    // IsValidClassId
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void IsValid_TA04_True() => Assert.IsTrue(IsValidClassId("TA04"));
    [TestMethod] public void IsValid_T108_True() => Assert.IsTrue(IsValidClassId("T108"));
    [TestMethod] public void IsValid_P101_True() => Assert.IsTrue(IsValidClassId("P101"));
    [TestMethod] public void IsValid_T300_True() => Assert.IsTrue(IsValidClassId("T300"));
    [TestMethod] public void IsValid_Null_False() => Assert.IsFalse(IsValidClassId(null));
    [TestMethod] public void IsValid_Empty_False() => Assert.IsFalse(IsValidClassId(""));
    [TestMethod] public void IsValid_Short_False() => Assert.IsFalse(IsValidClassId("TA"));
    [TestMethod] public void IsValid_Long_False() => Assert.IsFalse(IsValidClassId("TA0412"));
    [TestMethod] public void IsValid_XX_False() => Assert.IsFalse(IsValidClassId("XX04"));
    [TestMethod] public void IsValid_1A04_False() => Assert.IsFalse(IsValidClassId("1A04"));
    [TestMethod] public void IsValid_TAAB_False() => Assert.IsFalse(IsValidClassId("TAAB"));

    // ════════════════════════════════════════════════════════════════
    // GetCourseTypeDescription (from ClassId prefix)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TypeDesc_TA04_Adult()
        => Assert.AreEqual("Adult", GetTypeDesc("TA04"));

    [TestMethod]
    public void TypeDesc_T108_RedOrange()
        => Assert.AreEqual("Red & Orange Ball", GetTypeDesc("T108"));

    [TestMethod]
    public void TypeDesc_Empty_Unknown()
        => Assert.AreEqual("Unknown", GetTypeDesc(""));

    // ════════════════════════════════════════════════════════════════
    // SessionCountText
    // ════════════════════════════════════════════════════════════════

    [TestMethod] public void SessionText_0() => Assert.AreEqual("รายเดือน", SessionText(0));
    [TestMethod] public void SessionText_1() => Assert.AreEqual("ครั้งละ", SessionText(1));
    [TestMethod] public void SessionText_4() => Assert.AreEqual("4 ครั้ง", SessionText(4));
    [TestMethod] public void SessionText_8() => Assert.AreEqual("8 ครั้ง", SessionText(8));

    // ════════════════════════════════════════════════════════════════
    // ClassRateText
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void RateText_2200()
        => Assert.AreEqual("2,200", $"{2200:N0}");

    [TestMethod]
    public void RateText_0_Dash()
        => Assert.AreEqual("-", RateText(0));

    [TestMethod]
    public void RateText_600()
        => Assert.AreEqual("600", RateText(600));

    // ════════════════════════════════════════════════════════════════
    // ComboBoxDisplayText / FullDisplayName
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void FullDisplayName()
    {
        var text = $"{"Adult Class"} ({"4 ครั้ง"})";
        Assert.AreEqual("Adult Class (4 ครั้ง)", text);
    }

    [TestMethod]
    public void CompositeKey_Format()
    {
        var key = $"{"TA04"}|{"220250001"}";
        Assert.AreEqual("TA04|220250001", key);
    }

    [TestMethod]
    public void TrainerDisplayName_Empty()
        => Assert.AreEqual("ไม่ระบุ", TrainerDisplay(""));

    [TestMethod]
    public void TrainerDisplayName_HasName()
        => Assert.AreEqual("ครูมี", TrainerDisplay("ครูมี"));

    // ════════════════════════════════════════════════════════════════
    // CourseCardItem — Display
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Card_RegistrationCountText()
        => Assert.AreEqual("5 คนลง", $"{5} คนลง");

    [TestMethod]
    public void Card_PriceText_HasPrice()
        => Assert.AreEqual("฿2,200", $"฿{2200:N0}");

    [TestMethod]
    public void Card_PriceText_NoPrice()
        => Assert.AreEqual("-", CardPriceText(0));

    // ════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════

    private static readonly System.Collections.Generic.Dictionary<string, string> _types = new()
    {
        ["TA"] = "Adult", ["T1"] = "Red & Orange Ball", ["T2"] = "Intermediate",
        ["T3"] = "Competitive", ["P1"] = "Private Kru Mee",
        ["P2"] = "Private + Coach (Day)", ["P3"] = "Private + Coach (Night)"
    };

    private static bool IsValidClassId(string? classId)
    {
        if (string.IsNullOrWhiteSpace(classId) || classId.Length != 4) return false;
        if (!char.IsLetter(classId[0]) || !char.IsLetterOrDigit(classId[1])) return false;
        if (!char.IsDigit(classId[2]) || !char.IsDigit(classId[3])) return false;
        return _types.ContainsKey(classId[..2].ToUpperInvariant());
    }

    private static string GetTypeDesc(string classId)
    {
        if (string.IsNullOrEmpty(classId) || classId.Length < 2) return "Unknown";
        return _types.TryGetValue(classId[..2].ToUpperInvariant(), out var n) ? n : "Unknown";
    }

    private static string SessionText(int s) => s switch
    {
        0 => "รายเดือน", 1 => "ครั้งละ", _ => $"{s} ครั้ง"
    };

    private static string RateText(int rate) => rate > 0 ? $"{rate:N0}" : "-";

    private static string TrainerDisplay(string name)
        => string.IsNullOrWhiteSpace(name) ? "ไม่ระบุ" : name;

    private static string CardPriceText(int price)
        => price > 0 ? $"฿{price:N0}" : "-";

    // ════════════════════════════════════════════════════════════════
    // Test CourseKey record
    // ════════════════════════════════════════════════════════════════

    private record TestCourseKey(string ClassId, string TrainerId)
    {
        public override string ToString() => $"{ClassId}|{TrainerId}";

        public static TestCourseKey? Parse(string? compositeKey)
        {
            if (string.IsNullOrEmpty(compositeKey)) return null;
            var parts = compositeKey.Split('|');
            return parts.Length == 2 ? new TestCourseKey(parts[0], parts[1]) : null;
        }
    }
}
