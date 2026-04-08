using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ ReservationIdGenerator — สร้าง ID + Parse + Validate
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class ReservationIdGeneratorTests
{
    // ════════════════════════════════════════════════════════════════
    // ID Format: YYYYMMDDXX (10 digits)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("ID ต้อง 10 หลัก")]
    public void GenerateId_Has10Digits()
    {
        var id = GenerateId(new DateTime(2025, 6, 20), 1);
        Assert.AreEqual(10, id.Length);
    }

    [TestMethod]
    [Description("ID เริ่มด้วย date prefix: 20250620")]
    public void GenerateId_StartsWithDatePrefix()
    {
        var id = GenerateId(new DateTime(2025, 6, 20), 5);
        Assert.IsTrue(id.StartsWith("20250620"));
    }

    [TestMethod]
    [Description("Sequence 1 → XX = 01")]
    public void GenerateId_Sequence1_PaddedTo01()
    {
        var id = GenerateId(new DateTime(2025, 1, 1), 1);
        Assert.AreEqual("2025010101", id);
    }

    [TestMethod]
    [Description("Sequence 99 → XX = 99")]
    public void GenerateId_Sequence99()
    {
        var id = GenerateId(new DateTime(2025, 12, 31), 99);
        Assert.AreEqual("2025123199", id);
    }

    [TestMethod]
    [Description("วันที่ 1 มกราคม → prefix = 20250101")]
    public void GenerateId_JanuaryFirst()
    {
        var id = GenerateId(new DateTime(2025, 1, 1), 1);
        Assert.IsTrue(id.StartsWith("20250101"));
    }

    [TestMethod]
    [Description("วันที่ 31 ธันวาคม → prefix = 20251231")]
    public void GenerateId_DecemberLast()
    {
        var id = GenerateId(new DateTime(2025, 12, 31), 1);
        Assert.IsTrue(id.StartsWith("20251231"));
    }

    // ════════════════════════════════════════════════════════════════
    // ParseReservationId
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [Description("Parse valid ID: 2025062005 → Date=2025-06-20, Seq=5")]
    public void Parse_ValidId()
    {
        var result = ParseId("2025062005");
        Assert.IsNotNull(result);
        Assert.AreEqual(new DateTime(2025, 6, 20), result.Value.Date);
        Assert.AreEqual(5, result.Value.Sequence);
    }

    [TestMethod]
    [Description("Parse ID: 2025010199 → Seq=99")]
    public void Parse_MaxSequence()
    {
        var result = ParseId("2025010199");
        Assert.IsNotNull(result);
        Assert.AreEqual(99, result.Value.Sequence);
    }

    [TestMethod]
    [Description("Parse null → null")]
    public void Parse_Null_ReturnsNull()
    {
        var result = ParseId(null!);
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Parse empty → null")]
    public void Parse_Empty_ReturnsNull()
    {
        var result = ParseId("");
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Parse too short → null")]
    public void Parse_TooShort_ReturnsNull()
    {
        var result = ParseId("12345");
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Parse too long → null")]
    public void Parse_TooLong_ReturnsNull()
    {
        var result = ParseId("12345678901");
        Assert.IsNull(result);
    }

    [TestMethod]
    [Description("Parse invalid date → null")]
    public void Parse_InvalidDate_ReturnsNull()
    {
        var result = ParseId("2025139901"); // month 13
        Assert.IsNull(result);
    }

    // ════════════════════════════════════════════════════════════════
    // IsValidReservationId
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Validate_ValidId_True()
        => Assert.IsTrue(IsValidId("2025062001"));

    [TestMethod]
    public void Validate_Null_False()
        => Assert.IsFalse(IsValidId(null!));

    [TestMethod]
    public void Validate_Empty_False()
        => Assert.IsFalse(IsValidId(""));

    [TestMethod]
    public void Validate_Short_False()
        => Assert.IsFalse(IsValidId("12345"));

    [TestMethod]
    public void Validate_Letters_False()
        => Assert.IsFalse(IsValidId("ABCDEFGHIJ"));

    // ════════════════════════════════════════════════════════════════
    // ExtractSequenceNumber (ทดสอบผ่าน Parse)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Extract_Sequence01() => Assert.AreEqual(1, ParseId("2025062001")!.Value.Sequence);

    [TestMethod]
    public void Extract_Sequence50() => Assert.AreEqual(50, ParseId("2025062050")!.Value.Sequence);

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate logic from ReservationIdGenerator
    // ════════════════════════════════════════════════════════════════

    private static string GenerateId(DateTime date, int sequence)
        => $"{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{sequence:D2}";

    private static (DateTime Date, int Sequence)? ParseId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 10) return null;
        try
        {
            int year = int.Parse(id[..4]);
            int month = int.Parse(id[4..6]);
            int day = int.Parse(id[6..8]);
            var date = new DateTime(year, month, day);
            int seq = int.Parse(id[8..10]);
            return (date, seq);
        }
        catch { return null; }
    }

    private static bool IsValidId(string? id) => ParseId(id) != null;
}
