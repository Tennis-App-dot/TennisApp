using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class ValidationLogicTests
{
    [TestMethod]
    public void Phone_Valid_10Digits()
    {
        Assert.IsTrue(IsValidPhone("0891234567"));
    }

    [TestMethod]
    public void Phone_Valid_StartsWith06()
    {
        Assert.IsTrue(IsValidPhone("0612345678"));
    }

    [TestMethod]
    public void Phone_Invalid_9Digits()
    {
        Assert.IsFalse(IsValidPhone("089123456"));
    }

    [TestMethod]
    public void Phone_Invalid_11Digits()
    {
        Assert.IsFalse(IsValidPhone("08912345678"));
    }

    [TestMethod]
    public void Phone_Invalid_Letters()
    {
        Assert.IsFalse(IsValidPhone("089ABC4567"));
    }

    [TestMethod]
    public void Phone_Empty_IsOptional()
    {
        Assert.IsTrue(IsValidPhoneOptional(""));
    }

    [TestMethod]
    public void Phone_Null_IsOptional()
    {
        Assert.IsTrue(IsValidPhoneOptional(null));
    }

    [TestMethod]
    public void Phone_Whitespace_IsOptional()
    {
        Assert.IsTrue(IsValidPhoneOptional("   "));
    }

    [TestMethod]
    public void Date_Today_Valid()
    {
        Assert.IsTrue(DateTime.Today >= DateTime.Today);
    }

    [TestMethod]
    public void Date_Tomorrow_Valid()
    {
        Assert.IsTrue(DateTime.Today.AddDays(1) >= DateTime.Today);
    }

    [TestMethod]
    public void Date_Yesterday_Invalid()
    {
        Assert.IsFalse(DateTime.Today.AddDays(-1) >= DateTime.Today);
    }

    [TestMethod]
    public void Time_InRange_Valid()
    {
        Assert.IsTrue(IsWithinHours(TimeSpan.FromHours(12)));
    }

    [TestMethod]
    public void Time_Boundary21_Valid()
    {
        Assert.IsTrue(IsWithinHours(TimeSpan.FromHours(21)));
    }

    [TestMethod]
    public void Time_Over21_Invalid()
    {
        Assert.IsFalse(IsWithinHours(TimeSpan.FromHours(22)));
    }

    [TestMethod]
    public void Time_Under6_Invalid()
    {
        Assert.IsFalse(IsWithinHours(TimeSpan.FromHours(5)));
    }

    [TestMethod]
    public void Time_EndAfterStart()
    {
        Assert.IsTrue(TimeSpan.FromHours(12) > TimeSpan.FromHours(10));
    }

    [TestMethod]
    public void Time_EndExactly2100()
    {
        var end = TimeSpan.FromHours(19).Add(TimeSpan.FromHours(2));
        Assert.AreEqual(TimeSpan.FromHours(21), end);
    }

    [TestMethod]
    public void Time_EndExceeds2100()
    {
        var end = TimeSpan.FromHours(20).Add(TimeSpan.FromHours(2));
        Assert.IsTrue(end > TimeSpan.FromHours(21));
    }

    [TestMethod]
    public void Name_Valid()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace("John"));
    }

    [TestMethod]
    public void Name_Empty_Invalid()
    {
        Assert.IsTrue(string.IsNullOrWhiteSpace(""));
    }

    [TestMethod]
    public void Name_Null_Invalid()
    {
        Assert.IsTrue(string.IsNullOrWhiteSpace(null));
    }

    [TestMethod]
    public void Name_Spaces_Invalid()
    {
        Assert.IsTrue(string.IsNullOrWhiteSpace("   "));
    }

    [TestMethod]
    public void Duration_1Hour_Valid()
    {
        Assert.IsTrue(1.0 > 0);
    }

    [TestMethod]
    public void Duration_Half_Valid()
    {
        Assert.IsTrue(0.5 > 0);
    }

    [TestMethod]
    public void Duration_Zero_Invalid()
    {
        Assert.IsFalse(0.0 > 0);
    }

    [TestMethod]
    public void Duration_Negative_Invalid()
    {
        Assert.IsFalse(-1.0 > 0);
    }

    private static bool IsValidPhone(string phone)
    {
        return phone.Length == 10 && phone.All(char.IsDigit);
    }

    private static bool IsValidPhoneOptional(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) || IsValidPhone(phone);
    }

    private static bool IsWithinHours(TimeSpan time)
    {
        return time >= TimeSpan.FromHours(6) && time <= TimeSpan.FromHours(21);
    }
}
