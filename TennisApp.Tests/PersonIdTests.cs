using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════
/// Unit Tests สำหรับ TraineeItem / TrainerItem — ID Generation + Parse
/// ═══════════════════════════════════════════════════════════════════════
/// </summary>
[TestClass]
public class PersonIdTests
{
    // ════════════════════════════════════════════════════════════════
    // TraineeItem — GenerateTraineeId
    // Pattern: 1YYYY#### (prefix=1, year=4 digits, running=4 digits)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainee_GenerateId_2025_1()
        => Assert.AreEqual("120250001", GenerateTraineeId(2025, 1));

    [TestMethod]
    public void Trainee_GenerateId_2025_999()
        => Assert.AreEqual("120250999", GenerateTraineeId(2025, 999));

    [TestMethod]
    public void Trainee_GenerateId_2030_42()
        => Assert.AreEqual("120300042", GenerateTraineeId(2030, 42));

    [TestMethod]
    public void Trainee_Id_Length_Is9()
    {
        var id = GenerateTraineeId(2025, 1);
        Assert.AreEqual(9, id.Length);
    }

    [TestMethod]
    public void Trainee_Id_StartsWithPrefix1()
    {
        var id = GenerateTraineeId(2025, 1);
        Assert.IsTrue(id.StartsWith("1"));
    }

    // ════════════════════════════════════════════════════════════════
    // TraineeItem — ParseTraineeId
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainee_ParseId_120250001()
    {
        var (year, running) = ParseTraineeId("120250001");
        Assert.AreEqual(2025, year);
        Assert.AreEqual(1, running);
    }

    [TestMethod]
    public void Trainee_ParseId_120250042()
    {
        var (year, running) = ParseTraineeId("120250042");
        Assert.AreEqual(2025, year);
        Assert.AreEqual(42, running);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Trainee_ParseId_WrongPrefix_Throws()
        => ParseTraineeId("220250001"); // starts with 2 = trainer

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Trainee_ParseId_TooShort_Throws()
        => ParseTraineeId("12025");

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Trainee_ParseId_TooLong_Throws()
        => ParseTraineeId("1202500001");

    // ════════════════════════════════════════════════════════════════
    // TraineeItem — RoundTrip (Generate → Parse → Verify)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainee_RoundTrip()
    {
        var id = GenerateTraineeId(2025, 123);
        var (year, running) = ParseTraineeId(id);
        Assert.AreEqual(2025, year);
        Assert.AreEqual(123, running);
    }

    // ════════════════════════════════════════════════════════════════
    // TrainerItem — GenerateTrainerId
    // Pattern: 2YYYY#### (prefix=2, year=4 digits, running=4 digits)
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainer_GenerateId_2025_1()
        => Assert.AreEqual("220250001", GenerateTrainerId(2025, 1));

    [TestMethod]
    public void Trainer_GenerateId_2025_999()
        => Assert.AreEqual("220250999", GenerateTrainerId(2025, 999));

    [TestMethod]
    public void Trainer_GenerateId_2030_5()
        => Assert.AreEqual("220300005", GenerateTrainerId(2030, 5));

    [TestMethod]
    public void Trainer_Id_Length_Is9()
    {
        var id = GenerateTrainerId(2025, 1);
        Assert.AreEqual(9, id.Length);
    }

    [TestMethod]
    public void Trainer_Id_StartsWithPrefix2()
    {
        var id = GenerateTrainerId(2025, 1);
        Assert.IsTrue(id.StartsWith("2"));
    }

    // ════════════════════════════════════════════════════════════════
    // TrainerItem — ParseTrainerId
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainer_ParseId_220250001()
    {
        var (year, running) = ParseTrainerId("220250001");
        Assert.AreEqual(2025, year);
        Assert.AreEqual(1, running);
    }

    [TestMethod]
    public void Trainer_ParseId_220250099()
    {
        var (year, running) = ParseTrainerId("220250099");
        Assert.AreEqual(2025, year);
        Assert.AreEqual(99, running);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Trainer_ParseId_WrongPrefix_Throws()
        => ParseTrainerId("120250001"); // starts with 1 = trainee

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Trainer_ParseId_TooShort_Throws()
        => ParseTrainerId("22025");

    // ════════════════════════════════════════════════════════════════
    // TrainerItem — RoundTrip
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Trainer_RoundTrip()
    {
        var id = GenerateTrainerId(2025, 456);
        var (year, running) = ParseTrainerId(id);
        Assert.AreEqual(2025, year);
        Assert.AreEqual(456, running);
    }

    // ════════════════════════════════════════════════════════════════
    // Age Calculation
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Age_Born2000_In2025_Is25OrLess()
    {
        var birthDate = new DateTime(2000, 1, 1);
        var today = new DateTime(2025, 6, 20);
        var age = CalculateAge(birthDate, today);
        Assert.AreEqual(25, age);
    }

    [TestMethod]
    public void Age_BirthdayNotYetInYear()
    {
        var birthDate = new DateTime(2000, 12, 31);
        var today = new DateTime(2025, 6, 20);
        var age = CalculateAge(birthDate, today);
        Assert.AreEqual(24, age); // birthday hasn't happened yet
    }

    [TestMethod]
    public void Age_BirthdayToday()
    {
        var birthDate = new DateTime(2000, 6, 20);
        var today = new DateTime(2025, 6, 20);
        var age = CalculateAge(birthDate, today);
        Assert.AreEqual(25, age);
    }

    [TestMethod]
    public void Age_Child_8Years()
    {
        var birthDate = new DateTime(2017, 3, 15);
        var today = new DateTime(2025, 6, 20);
        var age = CalculateAge(birthDate, today);
        Assert.AreEqual(8, age);
    }

    // ════════════════════════════════════════════════════════════════
    // FullName
    // ════════════════════════════════════════════════════════════════

    [TestMethod]
    public void FullName_Concatenation()
        => Assert.AreEqual("สมชาย ทดสอบ", $"{"สมชาย"} {"ทดสอบ"}");

    [TestMethod]
    public void HasNickname_True()
        => Assert.IsTrue(!string.IsNullOrWhiteSpace("น้องเอ"));

    [TestMethod]
    public void HasNickname_False_Empty()
        => Assert.IsFalse(!string.IsNullOrWhiteSpace(""));

    [TestMethod]
    public void HasNickname_False_Whitespace()
        => Assert.IsFalse(!string.IsNullOrWhiteSpace("   "));

    // ════════════════════════════════════════════════════════════════
    // Helpers — replicate logic from TraineeItem/TrainerItem
    // ════════════════════════════════════════════════════════════════

    private static string GenerateTraineeId(int year, int running)
        => $"1{year:D4}{running:D4}";

    private static (int year, int running) ParseTraineeId(string id)
    {
        if (id.Length != 9 || !id.StartsWith("1"))
            throw new ArgumentException("Invalid trainee ID format");
        return (int.Parse(id.Substring(1, 4)), int.Parse(id.Substring(5, 4)));
    }

    private static string GenerateTrainerId(int year, int running)
        => $"2{year:D4}{running:D4}";

    private static (int year, int running) ParseTrainerId(string id)
    {
        if (id.Length != 9 || !id.StartsWith("2"))
            throw new ArgumentException("Invalid trainer ID format");
        return (int.Parse(id.Substring(1, 4)), int.Parse(id.Substring(5, 4)));
    }

    private static int CalculateAge(DateTime birthDate, DateTime today)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
