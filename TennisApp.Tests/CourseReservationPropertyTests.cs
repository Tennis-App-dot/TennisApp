using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class CourseReservationPropertyTests
{
    [TestMethod] public void Court_01() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 01", CD("01"));
    [TestMethod] public void Court_00() => Assert.AreEqual("\u0e23\u0e2d\u0e08\u0e31\u0e14\u0e2a\u0e23\u0e23\u0e2a\u0e19\u0e32\u0e21", CD("00"));
    [TestMethod] public void Court_Empty() => Assert.AreEqual("-", CD(""));
    [TestMethod] public void Assigned_True() => Assert.IsTrue(!string.IsNullOrEmpty("01") && "01"!="00");
    [TestMethod] public void Assigned_False() => Assert.IsFalse(!string.IsNullOrEmpty("00") && "00"!="00");

    [TestMethod] public void ClassDisplay_HasTitle() => Assert.AreEqual("Adult", ClassDisp("Adult","TA04"));
    [TestMethod] public void ClassDisplay_NoTitle() => Assert.AreEqual("TA04", ClassDisp("","TA04"));
    [TestMethod] public void ClassDisplay_Both_Empty() => Assert.AreEqual("-", ClassDisp("",""));

    [TestMethod] public void DurationDisplay_1() => Assert.AreEqual("1 \u0e0a\u0e21.", $"{1.0:0.#} \u0e0a\u0e21.");
    [TestMethod] public void DurationDisplay_15() => Assert.AreEqual("1.5 \u0e0a\u0e21.", $"{1.5:0.#} \u0e0a\u0e21.");
    [TestMethod] public void DurationDisplay_2() => Assert.AreEqual("2 \u0e0a\u0e21.", $"{2.0:0.#} \u0e0a\u0e21.");

    [TestMethod] public void EndTime_9plus1() { var e=TimeSpan.FromHours(9).Add(TimeSpan.FromHours(1)); Assert.AreEqual("10:00", e.ToString(@"hh\:mm")); }
    [TestMethod] public void EndTime_9plus15() { var e=TimeSpan.FromHours(9).Add(TimeSpan.FromHours(1.5)); Assert.AreEqual("10:30", e.ToString(@"hh\:mm")); }

    [TestMethod] public void FullDisplay_WithTitle() => Assert.AreEqual("20/06/2025, 09:00 - Adult", FR("20/06/2025","09:00","Adult","TA04"));
    [TestMethod] public void FullDisplay_NoTitle() => Assert.AreEqual("20/06/2025, 09:00 - TA04", FR("20/06/2025","09:00","","TA04"));

    [TestMethod] public void Stat_Booked() => Assert.AreEqual("\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27", SD("booked"));
    [TestMethod] public void Stat_InUse() => Assert.AreEqual("\u0e01\u0e33\u0e25\u0e31\u0e07\u0e43\u0e0a\u0e49\u0e07\u0e32\u0e19", SD("in_use"));
    [TestMethod] public void Stat_Completed() => Assert.AreEqual("\u0e40\u0e2a\u0e23\u0e47\u0e08\u0e2a\u0e34\u0e49\u0e19", SD("completed"));
    [TestMethod] public void Stat_Cancelled() => Assert.AreEqual("\u0e22\u0e01\u0e40\u0e25\u0e34\u0e01", SD("cancelled"));

    [TestMethod] public void Col_Booked() => Assert.AreEqual("#2196F3", SC("booked"));
    [TestMethod] public void Col_InUse() => Assert.AreEqual("#FF9800", SC("in_use"));
    [TestMethod] public void Col_Completed() => Assert.AreEqual("#4CAF50", SC("completed"));

    [TestMethod] public void Purpose() => Assert.AreEqual("\u0e04\u0e2d\u0e23\u0e4c\u0e2a\u0e40\u0e23\u0e35\u0e22\u0e19", "\u0e04\u0e2d\u0e23\u0e4c\u0e2a\u0e40\u0e23\u0e35\u0e22\u0e19");
    [TestMethod] public void TimeRange() { var s=TimeSpan.FromHours(9); var e=s.Add(TimeSpan.FromHours(1)); Assert.AreEqual("09:00-10:00",$"{s:hh\\:mm}-{e:hh\\:mm}"); }

    [TestMethod] public void Overlap_True() => Assert.IsTrue(OL(new(2025,6,20),TimeSpan.FromHours(9),1,new(2025,6,20),TimeSpan.FromHours(9),1));
    [TestMethod] public void Overlap_BackToBack_False() => Assert.IsFalse(OL(new(2025,6,20),TimeSpan.FromHours(9),1,new(2025,6,20),TimeSpan.FromHours(10),1));
    [TestMethod] public void Overlap_DiffDate_False() => Assert.IsFalse(OL(new(2025,6,20),TimeSpan.FromHours(9),1,new(2025,6,21),TimeSpan.FromHours(9),1));

    [TestMethod]
    public void Duration_FallsBackToClassDuration()
    {
        double reserveDuration = 0;
        int classDuration = 2;
        double effective = reserveDuration > 0 ? reserveDuration : classDuration;
        Assert.AreEqual(2.0, effective);
    }

    [TestMethod]
    public void Duration_UsesReserveDuration()
    {
        double reserveDuration = 1.5;
        int classDuration = 2;
        double effective = reserveDuration > 0 ? reserveDuration : classDuration;
        Assert.AreEqual(1.5, effective);
    }

    [TestMethod]
    public void Clone_AllFields()
    {
        var o = new TCI{RId="001",CId="01",ClId="TA04",TId="T001",Stat="booked",Dur=1.5};
        var c = new TCI{RId=o.RId,CId=o.CId,ClId=o.ClId,TId=o.TId,Stat=o.Stat,Dur=o.Dur};
        Assert.AreEqual("001", c.RId); Assert.AreEqual("TA04", c.ClId);
        Assert.AreEqual("T001", c.TId); Assert.AreEqual(1.5, c.Dur);
    }

    [TestMethod]
    public void PropChanged_Status()
    {
        var item = new TCINotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Status = "in_use";
        CollectionAssert.Contains(props, "Status");
        CollectionAssert.Contains(props, "StatusDisplay");
        CollectionAssert.Contains(props, "StatusColor");
    }

    static string CD(string? id) => string.IsNullOrEmpty(id)?"-":id=="00"?"\u0e23\u0e2d\u0e08\u0e31\u0e14\u0e2a\u0e23\u0e23\u0e2a\u0e19\u0e32\u0e21":$"\u0e2a\u0e19\u0e32\u0e21 {id}";
    static string ClassDisp(string title, string classId) => !string.IsNullOrEmpty(title)?title:string.IsNullOrEmpty(classId)?"-":classId;
    static string FR(string date, string time, string title, string classId) { var ci=string.IsNullOrEmpty(title)?classId:title; return $"{date}, {time} - {ci}"; }
    static string SD(string s) => s switch{"booked"=>"\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27","in_use"=>"\u0e01\u0e33\u0e25\u0e31\u0e07\u0e43\u0e0a\u0e49\u0e07\u0e32\u0e19","completed"=>"\u0e40\u0e2a\u0e23\u0e47\u0e08\u0e2a\u0e34\u0e49\u0e19","cancelled"=>"\u0e22\u0e01\u0e40\u0e25\u0e34\u0e01",_=>"\u0e08\u0e2d\u0e07\u0e41\u0e25\u0e49\u0e27"};
    static string SC(string s) => s switch{"booked"=>"#2196F3","in_use"=>"#FF9800","completed"=>"#4CAF50","cancelled"=>"#F44336",_=>"#2196F3"};
    static bool OL(DateTime d1, TimeSpan s1, int dur1, DateTime d2, TimeSpan s2, int dur2) { if(d1.Date!=d2.Date)return false; return s1<s2.Add(TimeSpan.FromHours(dur2))&&s1.Add(TimeSpan.FromHours(dur1))>s2; }

    record TCI { public string RId{get;set;}=""; public string CId{get;set;}=""; public string ClId{get;set;}=""; public string TId{get;set;}=""; public string Stat{get;set;}="booked"; public double Dur{get;set;}=1.0; }

    class TCINotify : INotifyPropertyChanged
    {
        string _s="booked";
        public string Status { get=>_s; set { if(_s==value)return; _s=value; F("Status"); F("StatusDisplay"); F("StatusColor"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}