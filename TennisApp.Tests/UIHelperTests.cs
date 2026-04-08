using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ UIHelper — ParseColor + Dialog configuration logic
/// ═══════════════════════════════════════════════════════════════════════
/// ทดสอบ logic เดียวกับ UIHelper (ไม่ใช้ WinUI types เพื่อให้ test ได้
/// โดยไม่ต้อง reference UI framework)
/// </summary>
[TestClass]
public class UIHelperTests
{
    // ════════════════════════════════════════════════════════════════
    // ParseColor — Valid Hex (with #)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_WithHash_4A148C()
    {
        var (a, r, g, b) = ParseColor("#4A148C");
        Assert.AreEqual(255, a);
        Assert.AreEqual(0x4A, r);
        Assert.AreEqual(0x14, g);
        Assert.AreEqual(0x8C, b);
    }

    [TestMethod]
    public void ParseColor_WithHash_FF0000_Red()
    {
        var (_, r, g, b) = ParseColor("#FF0000");
        Assert.AreEqual(255, r);
        Assert.AreEqual(0, g);
        Assert.AreEqual(0, b);
    }

    [TestMethod]
    public void ParseColor_WithHash_00FF00_Green()
    {
        var (_, r, g, b) = ParseColor("#00FF00");
        Assert.AreEqual(0, r);
        Assert.AreEqual(255, g);
        Assert.AreEqual(0, b);
    }

    [TestMethod]
    public void ParseColor_WithHash_0000FF_Blue()
    {
        var (_, r, g, b) = ParseColor("#0000FF");
        Assert.AreEqual(0, r);
        Assert.AreEqual(0, g);
        Assert.AreEqual(255, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Valid Hex (without #)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_WithoutHash()
    {
        var (_, r, g, b) = ParseColor("2196F3");
        Assert.AreEqual(0x21, r);
        Assert.AreEqual(0x96, g);
        Assert.AreEqual(0xF3, b);
    }

    [TestMethod]
    public void ParseColor_Black()
    {
        var (_, r, g, b) = ParseColor("000000");
        Assert.AreEqual(0, r);
        Assert.AreEqual(0, g);
        Assert.AreEqual(0, b);
    }

    [TestMethod]
    public void ParseColor_White()
    {
        var (_, r, g, b) = ParseColor("FFFFFF");
        Assert.AreEqual(255, r);
        Assert.AreEqual(255, g);
        Assert.AreEqual(255, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Invalid → fallback gray (158, 158, 158)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_TooShort_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("FFF");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_Empty_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_TooLong_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("FF00FF00");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_SingleChar_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("F");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_FiveChars_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("ABCDE");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_SevenChars_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("ABCDEF0");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_HashOnly_FallbackGray()
    {
        var (_, r, g, b) = ParseColor("#");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    [TestMethod]
    public void ParseColor_MultipleHashes_FallbackGray()
    {
        // TrimStart('#') removes all leading '#', leaving 5 chars → fallback
        var (_, r, g, b) = ParseColor("##ABCDE");
        Assert.AreEqual(158, r);
        Assert.AreEqual(158, g);
        Assert.AreEqual(158, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Alpha is always 255
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_AlphaIsAlways255_ForValidHex()
    {
        var (a, _, _, _) = ParseColor("#ABCDEF");
        Assert.AreEqual(255, a);
    }

    [TestMethod]
    public void ParseColor_AlphaIsAlways255_ForFallback()
    {
        var (a, _, _, _) = ParseColor("XYZ");
        Assert.AreEqual(255, a);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Lowercase / mixed case hex
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_LowercaseHex()
    {
        var (_, r, g, b) = ParseColor("#4a148c");
        Assert.AreEqual(0x4A, r);
        Assert.AreEqual(0x14, g);
        Assert.AreEqual(0x8C, b);
    }

    [TestMethod]
    public void ParseColor_MixedCaseHex()
    {
        var (_, r, g, b) = ParseColor("#aAbBcC");
        Assert.AreEqual(0xAA, r);
        Assert.AreEqual(0xBB, g);
        Assert.AreEqual(0xCC, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Boundary / specific decimal values
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_010101()
    {
        var (_, r, g, b) = ParseColor("#010101");
        Assert.AreEqual(1, r);
        Assert.AreEqual(1, g);
        Assert.AreEqual(1, b);
    }

    [TestMethod]
    public void ParseColor_7F7F7F()
    {
        var (_, r, g, b) = ParseColor("#7F7F7F");
        Assert.AreEqual(127, r);
        Assert.AreEqual(127, g);
        Assert.AreEqual(127, b);
    }

    [TestMethod]
    public void ParseColor_808080_MidGray()
    {
        var (_, r, g, b) = ParseColor("#808080");
        Assert.AreEqual(128, r);
        Assert.AreEqual(128, g);
        Assert.AreEqual(128, b);
    }

    [TestMethod]
    public void ParseColor_FEFEFE()
    {
        var (_, r, g, b) = ParseColor("#FEFEFE");
        Assert.AreEqual(254, r);
        Assert.AreEqual(254, g);
        Assert.AreEqual(254, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — App-specific status colors
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_StatusBooked_2196F3()
    {
        var (_, r, g, b) = ParseColor("#2196F3");
        Assert.AreEqual(33, r);
        Assert.AreEqual(150, g);
        Assert.AreEqual(243, b);
    }

    [TestMethod]
    public void ParseColor_StatusInUse_FF9800()
    {
        var (_, r, g, b) = ParseColor("#FF9800");
        Assert.AreEqual(255, r);
        Assert.AreEqual(152, g);
        Assert.AreEqual(0, b);
    }

    [TestMethod]
    public void ParseColor_StatusCompleted_4CAF50()
    {
        var (_, r, g, b) = ParseColor("#4CAF50");
        Assert.AreEqual(76, r);
        Assert.AreEqual(175, g);
        Assert.AreEqual(80, b);
    }

    [TestMethod]
    public void ParseColor_StatusCancelled_F44336()
    {
        var (_, r, g, b) = ParseColor("#F44336");
        Assert.AreEqual(244, r);
        Assert.AreEqual(67, g);
        Assert.AreEqual(54, b);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Consistency: with/without # produce identical result
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ParseColor_WithAndWithoutHash_SameResult()
    {
        var withHash = ParseColor("#AB12CD");
        var withoutHash = ParseColor("AB12CD");
        Assert.AreEqual(withHash, withoutHash);
    }

    // ════════════════════════════════════════════════════════════════
    // ParseColor — Invalid hex chars in 6-char string throws
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void ParseColor_InvalidHexChars_ThrowsFormatException()
        => ParseColor("ZZZZZZ");

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void ParseColor_PartiallyInvalidHex_ThrowsFormatException()
        => ParseColor("GG0000");

    // ════════════════════════════════════════════════════════════════
    // ShowLoadingAsync — Dialog configuration logic
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ShowLoading_DefaultMessage()
    {
        var config = BuildLoadingDialogConfig();
        Assert.AreEqual("กำลังประมวลผล", config.Title);
        Assert.AreEqual("กำลังโหลด...", config.Message);
        Assert.IsTrue(config.ProgressRingActive);
        Assert.AreEqual(40.0, config.ProgressRingSize);
    }

    [TestMethod]
    public void ShowLoading_CustomMessage()
    {
        var config = BuildLoadingDialogConfig("กำลังบันทึก...");
        Assert.AreEqual("กำลังประมวลผล", config.Title);
        Assert.AreEqual("กำลังบันทึก...", config.Message);
    }

    [TestMethod]
    public void ShowLoading_EmptyMessage()
    {
        var config = BuildLoadingDialogConfig("");
        Assert.AreEqual("กำลังประมวลผล", config.Title);
        Assert.AreEqual("", config.Message);
    }

    [TestMethod]
    public void ShowLoading_TitleIsAlwaysFixed()
    {
        var config1 = BuildLoadingDialogConfig("msg1");
        var config2 = BuildLoadingDialogConfig("msg2");
        Assert.AreEqual("กำลังประมวลผล", config1.Title);
        Assert.AreEqual("กำลังประมวลผล", config2.Title);
    }

    [TestMethod]
    public void ShowLoading_LongThaiMessage()
    {
        var config = BuildLoadingDialogConfig("กำลังโหลดข้อมูลจากฐานข้อมูล กรุณารอสักครู่...");
        Assert.AreEqual("กำลังโหลดข้อมูลจากฐานข้อมูล กรุณารอสักครู่...", config.Message);
    }

    [TestMethod]
    public void ShowLoading_ProgressRingIsActive()
    {
        var config = BuildLoadingDialogConfig();
        Assert.IsTrue(config.ProgressRingActive);
    }

    [TestMethod]
    public void ShowLoading_ProgressRingSize_40x40()
    {
        var config = BuildLoadingDialogConfig();
        Assert.AreEqual(40.0, config.ProgressRingSize);
    }

    [TestMethod]
    public void ShowLoading_TextMarginTop_Is16()
    {
        var config = BuildLoadingDialogConfig();
        Assert.AreEqual(16.0, config.TextMarginTop);
    }

    // ════════════════════════════════════════════════════════════════
    // ShowErrorAsync — Dialog configuration logic
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ShowError_SetsAllProperties()
    {
        var config = BuildMessageDialogConfig("เกิดข้อผิดพลาด", "ไม่สามารถบันทึกได้");
        Assert.AreEqual("เกิดข้อผิดพลาด", config.Title);
        Assert.AreEqual("ไม่สามารถบันทึกได้", config.Content);
        Assert.AreEqual("ตกลง", config.CloseButtonText);
    }

    [TestMethod]
    public void ShowError_EmptyTitle()
    {
        var config = BuildMessageDialogConfig("", "some message");
        Assert.AreEqual("", config.Title);
        Assert.AreEqual("some message", config.Content);
    }

    [TestMethod]
    public void ShowError_EmptyMessage()
    {
        var config = BuildMessageDialogConfig("title", "");
        Assert.AreEqual("title", config.Title);
        Assert.AreEqual("", config.Content);
    }

    [TestMethod]
    public void ShowError_CloseButtonTextIsAlwaysOk()
    {
        var config1 = BuildMessageDialogConfig("a", "b");
        var config2 = BuildMessageDialogConfig("x", "y");
        Assert.AreEqual("ตกลง", config1.CloseButtonText);
        Assert.AreEqual("ตกลง", config2.CloseButtonText);
    }

    [TestMethod]
    public void ShowError_NoPrimaryButton()
    {
        var config = BuildMessageDialogConfig("t", "m");
        Assert.IsNull(config.PrimaryButtonText);
    }

    [TestMethod]
    public void ShowError_NoDefaultButton()
    {
        var config = BuildMessageDialogConfig("t", "m");
        Assert.IsNull(config.DefaultButton);
    }

    [TestMethod]
    public void ShowError_ThaiTitle()
    {
        var config = BuildMessageDialogConfig("ข้อผิดพลาด", "เชื่อมต่อฐานข้อมูลไม่สำเร็จ");
        Assert.AreEqual("ข้อผิดพลาด", config.Title);
        Assert.AreEqual("เชื่อมต่อฐานข้อมูลไม่สำเร็จ", config.Content);
    }

    [TestMethod]
    public void ShowError_SpecialCharactersInMessage()
    {
        var config = BuildMessageDialogConfig("Error!", "Path: C:\\data\\file.db — not found <404>");
        Assert.AreEqual("Error!", config.Title);
        Assert.AreEqual("Path: C:\\data\\file.db — not found <404>", config.Content);
    }

    // ════════════════════════════════════════════════════════════════
    // ShowSuccessAsync — Dialog configuration logic
    // ════════════════════════════════════════════════════════════════
    // (same structure as ShowError — both use title, content, "ตกลง")

    [TestMethod]
    public void ShowSuccess_SetsAllProperties()
    {
        var config = BuildMessageDialogConfig("สำเร็จ", "บันทึกเรียบร้อย");
        Assert.AreEqual("สำเร็จ", config.Title);
        Assert.AreEqual("บันทึกเรียบร้อย", config.Content);
        Assert.AreEqual("ตกลง", config.CloseButtonText);
    }

    [TestMethod]
    public void ShowSuccess_EmptyTitle()
    {
        var config = BuildMessageDialogConfig("", "msg");
        Assert.AreEqual("", config.Title);
    }

    [TestMethod]
    public void ShowSuccess_EmptyMessage()
    {
        var config = BuildMessageDialogConfig("title", "");
        Assert.AreEqual("", config.Content);
    }

    [TestMethod]
    public void ShowSuccess_CloseButtonTextIsAlwaysOk()
    {
        var config = BuildMessageDialogConfig("t", "m");
        Assert.AreEqual("ตกลง", config.CloseButtonText);
    }

    [TestMethod]
    public void ShowSuccess_NoPrimaryButton()
    {
        var config = BuildMessageDialogConfig("t", "m");
        Assert.IsNull(config.PrimaryButtonText);
    }

    [TestMethod]
    public void ShowSuccess_IdenticalToErrorStructure()
    {
        var config = BuildMessageDialogConfig("title", "msg");
        Assert.AreEqual("title", config.Title);
        Assert.AreEqual("msg", config.Content);
        Assert.AreEqual("ตกลง", config.CloseButtonText);
        Assert.IsNull(config.PrimaryButtonText);
        Assert.IsNull(config.DefaultButton);
    }

    // ════════════════════════════════════════════════════════════════
    // ShowConfirmationAsync — Dialog configuration logic
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ShowConfirmation_SetsAllProperties()
    {
        var config = BuildConfirmationDialogConfig("ยืนยัน", "ต้องการลบ?");
        Assert.AreEqual("ยืนยัน", config.Title);
        Assert.AreEqual("ต้องการลบ?", config.Content);
        Assert.AreEqual("ใช่", config.PrimaryButtonText);
        Assert.AreEqual("ไม่", config.CloseButtonText);
        Assert.AreEqual("Close", config.DefaultButton);
    }

    [TestMethod]
    public void ShowConfirmation_DefaultButtonIsClose()
    {
        var config = BuildConfirmationDialogConfig("t", "m");
        Assert.AreEqual("Close", config.DefaultButton);
    }

    [TestMethod]
    public void ShowConfirmation_PrimaryButtonIsYes()
    {
        var config = BuildConfirmationDialogConfig("t", "m");
        Assert.AreEqual("ใช่", config.PrimaryButtonText);
    }

    [TestMethod]
    public void ShowConfirmation_CloseButtonIsNo()
    {
        var config = BuildConfirmationDialogConfig("t", "m");
        Assert.AreEqual("ไม่", config.CloseButtonText);
    }

    [TestMethod]
    public void ShowConfirmation_EmptyTitle()
    {
        var config = BuildConfirmationDialogConfig("", "msg");
        Assert.AreEqual("", config.Title);
    }

    [TestMethod]
    public void ShowConfirmation_EmptyMessage()
    {
        var config = BuildConfirmationDialogConfig("title", "");
        Assert.AreEqual("", config.Content);
    }

    [TestMethod]
    public void ShowConfirmation_ThaiContent()
    {
        var config = BuildConfirmationDialogConfig("ยืนยันการลบ", "คุณต้องการลบสนามนี้หรือไม่?");
        Assert.AreEqual("ยืนยันการลบ", config.Title);
        Assert.AreEqual("คุณต้องการลบสนามนี้หรือไม่?", config.Content);
    }

    [TestMethod]
    public void ShowConfirmation_DiffersFromMessageDialog_HasPrimaryButton()
    {
        var message = BuildMessageDialogConfig("title", "msg");
        var confirm = BuildConfirmationDialogConfig("title", "msg");
        Assert.IsNull(message.PrimaryButtonText);
        Assert.IsNotNull(confirm.PrimaryButtonText);
    }

    [TestMethod]
    public void ShowConfirmation_DiffersFromMessageDialog_DifferentCloseText()
    {
        var message = BuildMessageDialogConfig("title", "msg");
        var confirm = BuildConfirmationDialogConfig("title", "msg");
        Assert.AreEqual("ตกลง", message.CloseButtonText);
        Assert.AreEqual("ไม่", confirm.CloseButtonText);
    }

    // ════════════════════════════════════════════════════════════════
    // ShowConfirmationAsync — Result mapping logic
    // (replicates: result == ContentDialogResult.Primary → true)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ConfirmationResult_Primary_ReturnsTrue()
        => Assert.IsTrue(MapConfirmationResult("Primary"));

    [TestMethod]
    public void ConfirmationResult_None_ReturnsFalse()
        => Assert.IsFalse(MapConfirmationResult("None"));

    [TestMethod]
    public void ConfirmationResult_Secondary_ReturnsFalse()
        => Assert.IsFalse(MapConfirmationResult("Secondary"));

    [TestMethod]
    public void ConfirmationResult_EmptyString_ReturnsFalse()
        => Assert.IsFalse(MapConfirmationResult(""));

    [TestMethod]
    public void ConfirmationResult_CaseSensitive_PrimaryLower_ReturnsFalse()
        => Assert.IsFalse(MapConfirmationResult("primary"));

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate UIHelper.ParseColor logic
    // ════════════════════════════════════════════════════════════════

    private static (byte A, byte R, byte G, byte B) ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            return (255,
                byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        return (255, 158, 158, 158); // fallback gray
    }

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate UIHelper.ShowLoadingAsync config
    // ════════════════════════════════════════════════════════════════

    private record LoadingDialogConfig(
        string Title,
        string Message,
        bool ProgressRingActive,
        double ProgressRingSize,
        double TextMarginTop);

    private static LoadingDialogConfig BuildLoadingDialogConfig(string message = "กำลังโหลด...")
        => new(
            Title: "กำลังประมวลผล",
            Message: message,
            ProgressRingActive: true,
            ProgressRingSize: 40,
            TextMarginTop: 16);

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate UIHelper.ShowErrorAsync / ShowSuccessAsync config
    // (both methods build identical dialog structure)
    // ════════════════════════════════════════════════════════════════

    private record DialogConfig(
        string Title,
        string Content,
        string CloseButtonText,
        string? PrimaryButtonText = null,
        string? DefaultButton = null);

    private static DialogConfig BuildMessageDialogConfig(string title, string message)
        => new(title, message, CloseButtonText: "ตกลง");

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate UIHelper.ShowConfirmationAsync config + result
    // ════════════════════════════════════════════════════════════════

    private static DialogConfig BuildConfirmationDialogConfig(string title, string message)
        => new(title, message,
            PrimaryButtonText: "ใช่",
            CloseButtonText: "ไม่",
            DefaultButton: "Close");

    /// <summary>
    /// Replicates: return result == ContentDialogResult.Primary
    /// </summary>
    private static bool MapConfirmationResult(string result)
        => result == "Primary";
}
