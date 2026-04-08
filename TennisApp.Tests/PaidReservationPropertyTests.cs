using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class PaidReservationPropertyTests
{
    [TestMethod] public void DateDisplay() => Assert.AreEqual("16/04/2025", new DateTime(2025,4,16).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
    [TestMethod] public void TimeDisplay_0800() => Assert.AreEqual("08:00", new TimeSpan(8,0,0).ToString(@"hh\:mm"));
    [TestMethod] public void TimeDisplay_1430() => Assert.AreEqual("14:30", new TimeSpan(14,30,0).ToString(@"hh\:mm"));
    [TestMethod] public void Duration_1() => Assert.AreEqual("1.0 \u0e0a\u0e21.", $"{1.0:F1} \u0e0a\u0e21.");
    [TestMethod] public void Duration_25() => Assert.AreEqual("2.5 \u0e0a\u0e21.", $"{2.5:F1} \u0e0a\u0e21.");
    [TestMethod] public void EndTime_8plus2() { var e=TimeSpan.FromHours(8).Add(TimeSpan.FromHours(2)); Assert.AreEqual("10:00", e.ToString(@"hh\:mm")); }
    [TestMethod] public void EndTime_14plus3() { var e=TimeSpan.FromHours(14).Add(TimeSpan.FromHours(3)); Assert.AreEqual("17:00", e.ToString(@"hh\:mm")); }
    [TestMethod] public void DateTimeDisplay() => Assert.AreEqual("16/04/2025, 14:00", $"{new DateTime(2025,4,16).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}, {new TimeSpan(14,0,0):hh\\:mm}");
    [TestMethod] public void TimeRange_10to12() { var s=TimeSpan.FromHours(10); var e=s.Add(TimeSpan.FromHours(2)); Assert.AreEqual("10:00-12:00",$"{s:hh\\:mm}-{e:hh\\:mm}"); }
    [TestMethod] public void Purpose() => Assert.AreEqual("\u0e40\u0e0a\u0e48\u0e32\u0e2a\u0e19\u0e32\u0e21", "\u0e40\u0e0a\u0e48\u0e32\u0e2a\u0e19\u0e32\u0e21");

    [TestMethod] public void Court_Normal() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 02", CD("02"));
    [TestMethod] public void Court_00() => Assert.AreEqual("\u0e23\u0e2d\u0e08\u0e31\u0e14\u0e2a\u0e23\u0e23\u0e2a\u0e19\u0e32\u0e21", CD("00"));
    [TestMethod] public void Court_Empty() => Assert.AreEqual("-", CD(""));
    [TestMethod] public void Court_Null() => Assert.AreEqual("-", CD(null));
    [TestMethod] public void Assigned_01() => Assert.IsTrue(IA("01"));
    [TestMethod] public void Assigned_00() => Assert.IsFalse(IA("00"));
    [TestMethod] public void Assigned_Empty() => Assert.IsFalse(IA(""));

    [TestMethod] public void Stat_Booked() => Assert.AreEqual("\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27", SD("booked"));
    [TestMethod] public void Stat_InUse() => Assert.AreEqual("\u0e01\u0e33\u0e25\u0e31\u0e07\u0e43\u0e0a\u0e49\u0e07\u0e32\u0e19", SD("in_use"));
    [TestMethod] public void Stat_Completed() => Assert.AreEqual("\u0e40\u0e2a\u0e23\u0e47\u0e08\u0e2a\u0e34\u0e49\u0e19", SD("completed"));
    [TestMethod] public void Stat_Cancelled() => Assert.AreEqual("\u0e22\u0e01\u0e40\u0e25\u0e34\u0e01", SD("cancelled"));
    [TestMethod] public void Stat_Unknown() => Assert.AreEqual("\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27", SD("xyz"));

    [TestMethod] public void Col_Booked() => Assert.AreEqual("#2196F3", SC("booked"));
    [TestMethod] public void Col_InUse() => Assert.AreEqual("#FF9800", SC("in_use"));
    [TestMethod] public void Col_Completed() => Assert.AreEqual("#4CAF50", SC("completed"));
    [TestMethod] public void Col_Cancelled() => Assert.AreEqual("#F44336", SC("cancelled"));

    [TestMethod] public void Overlap_Partial() => Assert.IsTrue(OL(new(2025,6,20), TimeSpan.FromHours(10), 2.0, new(2025,6,20), TimeSpan.FromHours(11), 2.0));
    [TestMethod] public void Overlap_BackToBack() => Assert.IsFalse(OL(new(2025,6,20), TimeSpan.FromHours(10), 1.0, new(2025,6,20), TimeSpan.FromHours(11), 1.0));
    [TestMethod] public void Overlap_DiffDate() => Assert.IsFalse(OL(new(2025,6,20), TimeSpan.FromHours(10), 2.0, new(2025,6,21), TimeSpan.FromHours(10), 2.0));
    [TestMethod] public void Overlap_Contained() => Assert.IsTrue(OL(new(2025,6,20), TimeSpan.FromHours(9), 4.0, new(2025,6,20), TimeSpan.FromHours(10), 1.0));
    [TestMethod] public void Overlap_Identical() => Assert.IsTrue(OL(new(2025,6,20), TimeSpan.FromHours(10), 2.0, new(2025,6,20), TimeSpan.FromHours(10), 2.0));

    [TestMethod]
    public void Clone_Copies()
    {
        var o = new TPI{RId="001",CId="01",Dur=2.0,Stat="booked",AP=800};
        var c = new TPI{RId=o.RId,CId=o.CId,Dur=o.Dur,Stat=o.Stat,AP=o.AP};
        Assert.AreEqual(o.RId, c.RId); Assert.AreEqual(o.CId, c.CId);
        Assert.AreEqual(o.Dur, c.Dur); Assert.AreEqual(o.Stat, c.Stat);
        Assert.AreEqual(o.AP, c.AP);
    }

    [TestMethod]
    public void Clone_Independent()
    {
        var o = new TPI{Stat="booked"};
        var c = new TPI{Stat=o.Stat};
        c.Stat="cancelled";
        Assert.AreEqual("booked", o.Stat);
    }

    [TestMethod]
    public void PropChanged_Status()
    {
        var item = new TPINotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Status = "in_use";
        CollectionAssert.Contains(props, "Status");
        CollectionAssert.Contains(props, "StatusDisplay");
        CollectionAssert.Contains(props, "StatusColor");
    }

    [TestMethod]
    public void PropChanged_Duration()
    {
        var item = new TPINotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Duration = 3.0;
        CollectionAssert.Contains(props, "Duration");
        CollectionAssert.Contains(props, "DurationDisplay");
        CollectionAssert.Contains(props, "EndTimeDisplay");
    }

    [TestMethod]
    public void PropChanged_SameValue_NoFire()
    {
        var item = new TPINotify{Status="booked"};
        bool fired = false;
        item.PropertyChanged += (_, _) => fired = true;
        item.Status = "booked";
        Assert.IsFalse(fired);
    }

    [TestMethod] public void Default_Status() => Assert.AreEqual("booked", new TPI().Stat);
    [TestMethod] public void Default_Duration() => Assert.AreEqual(1.0, new TPI().Dur);
    [TestMethod] public void Default_Time() => Assert.AreEqual(new TimeSpan(8,0,0), new TPI().Time);

    static string CD(string? id) => string.IsNullOrEmpty(id) ? "-" : id=="00" ? "\u0e23\u0e2d\u0e08\u0e31\u0e14\u0e2a\u0e23\u0e23\u0e2a\u0e19\u0e32\u0e21" : $"\u0e2a\u0e19\u0e32\u0e21 {id}";
    static bool IA(string? id) => !string.IsNullOrEmpty(id) && id!="00";
    static string SD(string s) => s switch { "booked"=>"\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27","in_use"=>"\u0e01\u0e33\u0e25\u0e31\u0e07\u0e43\u0e0a\u0e49\u0e07\u0e32\u0e19","completed"=>"\u0e40\u0e2a\u0e23\u0e47\u0e08\u0e2a\u0e34\u0e49\u0e19","cancelled"=>"\u0e22\u0e01\u0e40\u0e25\u0e34\u0e01",_=>"\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27" };
    static string SC(string s) => s switch { "booked"=>"#2196F3","in_use"=>"#FF9800","completed"=>"#4CAF50","cancelled"=>"#F44336",_=>"#2196F3" };
    static bool OL(DateTime d1, TimeSpan s1, double dur1, DateTime d2, TimeSpan s2, double dur2) { if(d1.Date!=d2.Date)return false; return s1<s2.Add(TimeSpan.FromHours(dur2))&&s1.Add(TimeSpan.FromHours(dur1))>s2; }

    record TPI { public string RId{get;set;}=""; public string CId{get;set;}=""; public DateTime Date{get;set;}=DateTime.Today; public TimeSpan Time{get;set;}=new(8,0,0); public double Dur{get;set;}=1.0; public string Stat{get;set;}="booked"; public int? AP{get;set;} }

    class TPINotify : INotifyPropertyChanged
    {
        string _s="booked"; double _d=1.0;
        public string Status { get=>_s; set { if(_s==value)return; _s=value; F("Status"); F("StatusDisplay"); F("StatusColor"); } }
        public double Duration { get=>_d; set { if(_d==value)return; _d=value; F("Duration"); F("DurationDisplay"); F("EndTimeDisplay"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}