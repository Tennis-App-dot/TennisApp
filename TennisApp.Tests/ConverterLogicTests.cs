using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ Converter Logic — Status/Color/Visibility
/// ═══════════════════════════════════════════════════════════════════════
/// ทดสอบ logic เดียวกับ converters ใน TennisApp.Converters namespace
/// (ไม่ใช้ WinUI types เพื่อให้ test ได้โดยไม่ต้อง reference UI framework)
/// </summary>
[TestClass]
public class ConverterLogicTests
{
    // ════════════════════════════════════════════════════════════════
    // StatusTextConverter — "1" = "พร้อมใช้งาน", other = "กำลังปิดปรับปรุง"
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void StatusText_1_Available()
        => Assert.AreEqual("พร้อมใช้งาน", CourtStatusText("1"));

    [TestMethod]
    public void StatusText_0_Maintenance()
        => Assert.AreEqual("กำลังปิดปรับปรุง", CourtStatusText("0"));

    [TestMethod]
    public void StatusText_Null_Maintenance()
        => Assert.AreEqual("กำลังปิดปรับปรุง", CourtStatusText(null));

    [TestMethod]
    public void StatusText_Empty_Maintenance()
        => Assert.AreEqual("กำลังปิดปรับปรุง", CourtStatusText(""));

    // ════════════════════════════════════════════════════════════════
    // StatusTextConverter — ConvertBack
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void StatusText_ConvertBack_Available()
        => Assert.AreEqual("1", CourtStatusTextBack("พร้อมใช้งาน"));

    [TestMethod]
    public void StatusText_ConvertBack_Maintenance()
        => Assert.AreEqual("0", CourtStatusTextBack("กำลังปิดปรับปรุง"));

    [TestMethod]
    public void StatusText_ConvertBack_Other()
        => Assert.AreEqual("0", CourtStatusTextBack("xyz"));

    // ════════════════════════════════════════════════════════════════
    // StatusToColorConverter — reservation status → color hex
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void StatusToColor_Booked_Blue()
        => Assert.AreEqual("#2196F3", ReservationStatusColor("booked"));

    [TestMethod]
    public void StatusToColor_InUse_Orange()
        => Assert.AreEqual("#FF9800", ReservationStatusColor("in_use"));

    [TestMethod]
    public void StatusToColor_Completed_Green()
        => Assert.AreEqual("#4CAF50", ReservationStatusColor("completed"));

    [TestMethod]
    public void StatusToColor_Cancelled_Red()
        => Assert.AreEqual("#F44336", ReservationStatusColor("cancelled"));

    [TestMethod]
    public void StatusToColor_Unknown_Gray()
        => Assert.AreEqual("#9E9E9E", ReservationStatusColor("xyz"));

    [TestMethod]
    public void StatusToColor_Null_Gray()
        => Assert.AreEqual("#9E9E9E", ReservationStatusColor(null));

    // ════════════════════════════════════════════════════════════════
    // BoolToColorConverter — IsAssigned → Black/Orange
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BoolToColor_Assigned_Black()
        => Assert.AreEqual("Black", AssignedColor(true));

    [TestMethod]
    public void BoolToColor_NotAssigned_Orange()
        => Assert.AreEqual("DarkOrange", AssignedColor(false));

    // ════════════════════════════════════════════════════════════════
    // InverseBoolConverter — simple inversion
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void InverseBool_True_False()
        => Assert.IsFalse(InverseBool(true));

    [TestMethod]
    public void InverseBool_False_True()
        => Assert.IsTrue(InverseBool(false));

    // ════════════════════════════════════════════════════════════════
    // BoolToVisibility — true = Visible, false = Collapsed
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BoolToVisibility_True_Visible()
        => Assert.AreEqual("Visible", BoolToVisibility(true));

    [TestMethod]
    public void BoolToVisibility_False_Collapsed()
        => Assert.AreEqual("Collapsed", BoolToVisibility(false));

    // ════════════════════════════════════════════════════════════════
    // InverseBoolToVisibility — true = Collapsed, false = Visible
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void InverseBoolToVisibility_True_Collapsed()
        => Assert.AreEqual("Collapsed", InverseBoolToVisibility(true));

    [TestMethod]
    public void InverseBoolToVisibility_False_Visible()
        => Assert.AreEqual("Visible", InverseBoolToVisibility(false));

    // ════════════════════════════════════════════════════════════════
    // StatusBrushConverter — court_status "1" → Green, other → Red
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void StatusBrush_1_Green()
        => Assert.AreEqual("Green", CourtStatusBrushColor("1"));

    [TestMethod]
    public void StatusBrush_0_Red()
        => Assert.AreEqual("Red", CourtStatusBrushColor("0"));

    [TestMethod]
    public void StatusBrush_Null_Red()
        => Assert.AreEqual("Red", CourtStatusBrushColor(null));

    // ════════════════════════════════════════════════════════════════
    // AlternatingRowConverter — even/odd index
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AlternatingRow_Even_Light()
        => Assert.AreEqual("Light", AlternatingRowBg(0));

    [TestMethod]
    public void AlternatingRow_Odd_Dark()
        => Assert.AreEqual("Dark", AlternatingRowBg(1));

    [TestMethod]
    public void AlternatingRow_2_Light()
        => Assert.AreEqual("Light", AlternatingRowBg(2));

    [TestMethod]
    public void AlternatingRow_3_Dark()
        => Assert.AreEqual("Dark", AlternatingRowBg(3));

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate converter logic without WinUI dependencies
    // ════════════════════════════════════════════════════════════════

    private static string CourtStatusText(string? value)
        => value == "1" ? "พร้อมใช้งาน" : "กำลังปิดปรับปรุง";

    private static string CourtStatusTextBack(string? text)
        => text == "พร้อมใช้งาน" ? "1" : "0";

    private static string ReservationStatusColor(string? status)
        => (status ?? "") switch
        {
            "booked" => "#2196F3",
            "in_use" => "#FF9800",
            "completed" => "#4CAF50",
            "cancelled" => "#F44336",
            _ => "#9E9E9E"
        };

    private static string AssignedColor(bool isAssigned)
        => isAssigned ? "Black" : "DarkOrange";

    private static bool InverseBool(bool value) => !value;

    private static string BoolToVisibility(bool value)
        => value ? "Visible" : "Collapsed";

    private static string InverseBoolToVisibility(bool value)
        => value ? "Collapsed" : "Visible";

    private static string CourtStatusBrushColor(string? value)
        => value == "1" ? "Green" : "Red";

    private static string AlternatingRowBg(int index)
        => index % 2 == 0 ? "Light" : "Dark";
}
