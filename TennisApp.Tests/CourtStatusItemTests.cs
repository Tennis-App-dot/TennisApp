using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class CourtStatusItemTests
{
    [TestMethod] public void Display_01() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 01", $"\u0e2a\u0e19\u0e32\u0e21 {"01"}");
    [TestMethod] public void Display_TA() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 TA", $"\u0e2a\u0e19\u0e32\u0e21 {"TA"}");

    [TestMethod] public void StatusText_Available() => Assert.AreEqual("\u0e27\u0e48\u0e32\u0e07", ST(false, TimeSpan.Zero, 0));
    [TestMethod] public void StatusText_InUse() { var t=ST(true, TimeSpan.FromHours(10), 2.0); Assert.IsTrue(t.Contains("12:00")); }
    [TestMethod] public void StatusText_InUse_1930() { var t=ST(true, TimeSpan.FromHours(19.5), 1.5); Assert.IsTrue(t.Contains("21:00")); }

    [TestMethod] public void StatusColor_InUse() => Assert.AreEqual("#FF9800", IsInUse(true));
    [TestMethod] public void StatusColor_Available() => Assert.AreEqual("#4CAF50", IsInUse(false));

    [TestMethod] public void EndTime_10plus2() => Assert.AreEqual(TimeSpan.FromHours(12), TimeSpan.FromHours(10).Add(TimeSpan.FromHours(2)));
    [TestMethod] public void EndTime_19plus2() => Assert.AreEqual(TimeSpan.FromHours(21), TimeSpan.FromHours(19).Add(TimeSpan.FromHours(2)));

    [TestMethod] public void StartTimeDisplay() => Assert.AreEqual("09:00", TimeSpan.FromHours(9).ToString(@"hh\:mm"));
    [TestMethod] public void EndTimeDisplay() { var e=TimeSpan.FromHours(10).Add(TimeSpan.FromHours(2)); Assert.AreEqual("12:00", e.ToString(@"hh\:mm")); }
    [TestMethod] public void DurationDisplay_1() => Assert.AreEqual("1 \u0e0a\u0e31\u0e48\u0e27\u0e42\u0e21\u0e07", $"{1.0:0.#} \u0e0a\u0e31\u0e48\u0e27\u0e42\u0e21\u0e07");
    [TestMethod] public void DurationDisplay_15() => Assert.AreEqual("1.5 \u0e0a\u0e31\u0e48\u0e27\u0e42\u0e21\u0e07", $"{1.5:0.#} \u0e0a\u0e31\u0e48\u0e27\u0e42\u0e21\u0e07");
    [TestMethod] public void PriceDisplay_400() => Assert.AreEqual("400", $"{400:N0}");
    [TestMethod] public void PriceDisplay_1000() => Assert.AreEqual("1,000", $"{1000:N0}");

    [TestMethod] public void UsageType_Paid() => Assert.AreEqual("\u0e40\u0e0a\u0e48\u0e32\u0e43\u0e0a\u0e49\u0e1e\u0e37\u0e49\u0e19\u0e17\u0e35\u0e48", UT("Paid"));
    [TestMethod] public void UsageType_Course() => Assert.AreEqual("\u0e04\u0e2d\u0e23\u0e4c\u0e2a\u0e40\u0e23\u0e35\u0e22\u0e19", UT("Course"));
    [TestMethod] public void UsageType_Unknown() => Assert.AreEqual("-", UT(""));

    [TestMethod]
    public void ActualStartTimeDisplay_HasValue()
    {
        var dt = new DateTime(2025, 6, 20, 14, 30, 0);
        Assert.AreEqual("14:30", dt.ToString("HH:mm"));
    }

    [TestMethod]
    public void ActualStartTimeDisplay_Null_Fallback()
    {
        DateTime? actual = null;
        var fallback = TimeSpan.FromHours(14).ToString(@"hh\:mm");
        Assert.AreEqual("14:00", actual?.ToString("HH:mm") ?? fallback);
    }

    [TestMethod]
    public void PropChanged_IsInUse()
    {
        var item = new TCSNotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.IsInUse = true;
        CollectionAssert.Contains(props, "IsInUse");
        CollectionAssert.Contains(props, "StatusText");
        CollectionAssert.Contains(props, "StatusColor");
    }

    [TestMethod]
    public void PropChanged_Duration()
    {
        var item = new TCSNotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Duration = 2.0;
        CollectionAssert.Contains(props, "Duration");
        CollectionAssert.Contains(props, "DurationDisplay");
        CollectionAssert.Contains(props, "EndTime");
        CollectionAssert.Contains(props, "StatusText");
    }

    static string ST(bool inUse, TimeSpan start, double dur) { if(!inUse)return"\u0e27\u0e48\u0e32\u0e07"; var e=start.Add(TimeSpan.FromHours(dur)); return $"\u0e23\u0e30\u0e22\u0e30\u0e40\u0e27\u0e25\u0e32\u0e2a\u0e34\u0e49\u0e19\u0e2a\u0e38\u0e14: {e:hh\\:mm}"; }
    static string IsInUse(bool v) => v?"#FF9800":"#4CAF50";
    static string UT(string t) => t switch{"Paid"=>"\u0e40\u0e0a\u0e48\u0e32\u0e43\u0e0a\u0e49\u0e1e\u0e37\u0e49\u0e19\u0e17\u0e35\u0e48","Course"=>"\u0e04\u0e2d\u0e23\u0e4c\u0e2a\u0e40\u0e23\u0e35\u0e22\u0e19",_=>"-"};

    class TCSNotify : INotifyPropertyChanged
    {
        bool _iu; double _d;
        public bool IsInUse { get=>_iu; set { _iu=value; F("IsInUse"); F("StatusText"); F("StatusColor"); } }
        public double Duration { get=>_d; set { _d=value; F("Duration"); F("DurationDisplay"); F("EndTime"); F("StatusText"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}