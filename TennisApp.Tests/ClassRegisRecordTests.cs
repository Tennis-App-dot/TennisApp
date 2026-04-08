using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class ClassRegisRecordTests
{
    [TestMethod] public void DateFormatted() => Assert.AreEqual("20/06/2025", new DateTime(2025,6,20).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
    [TestMethod] public void DateFormatted_Jan() => Assert.AreEqual("01/01/2025", new DateTime(2025,1,1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

    [TestMethod] public void ClassTimeText_4() => Assert.AreEqual("4 \u0e04\u0e23\u0e31\u0e49\u0e07", CTT(4));
    [TestMethod] public void ClassTimeText_8() => Assert.AreEqual("8 \u0e04\u0e23\u0e31\u0e49\u0e07", CTT(8));
    [TestMethod] public void ClassTimeText_0() => Assert.AreEqual("-", CTT(0));
    [TestMethod] public void ClassTimeText_Neg() => Assert.AreEqual("-", CTT(-1));

    [TestMethod] public void ClassRateText_2200() => Assert.AreEqual("\u0e3f2,200", CRT(2200));
    [TestMethod] public void ClassRateText_600() => Assert.AreEqual("\u0e3f600", CRT(600));
    [TestMethod] public void ClassRateText_0() => Assert.AreEqual("-", CRT(0));

    [TestMethod] public void TrainerDisplay_HasName() => Assert.AreEqual("\u0e04\u0e23\u0e39\u0e40\u0e2d", TD("\u0e04\u0e23\u0e39\u0e40\u0e2d"));
    [TestMethod] public void TrainerDisplay_Empty() => Assert.AreEqual("\u0e44\u0e21\u0e48\u0e23\u0e30\u0e1a\u0e38", TD(""));
    [TestMethod] public void TrainerDisplay_Spaces() => Assert.AreEqual("\u0e44\u0e21\u0e48\u0e23\u0e30\u0e1a\u0e38", TD("   "));
    [TestMethod] public void TrainerDisplay_Null() => Assert.AreEqual("\u0e44\u0e21\u0e48\u0e23\u0e30\u0e1a\u0e38", TD(null));

    [TestMethod]
    public void FromDatabase_AllFields()
    {
        var r = MakeRecord("TR001", "TA04", new DateTime(2025,6,20), "John", "0891234567", "Adult", 4, 2200, "Coach", "T001");
        Assert.AreEqual("TR001", r.TraineeId);
        Assert.AreEqual("TA04", r.ClassId);
        Assert.AreEqual("T001", r.TrainerId);
        Assert.AreEqual(new DateTime(2025,6,20), r.RegisDate);
        Assert.AreEqual("John", r.TraineeName);
        Assert.AreEqual(4, r.ClassTime);
        Assert.AreEqual(2200, r.ClassRate);
    }

    [TestMethod]
    public void FromDatabase_NullsDefault()
    {
        var r = MakeRecord("TR001", "TA04", DateTime.Today, null, null, null, 0, 0, null, null);
        Assert.AreEqual("", r.TraineeName);
        Assert.AreEqual("", r.TrainerId);
        Assert.AreEqual("", r.TrainerName);
    }

    [TestMethod]
    public void RowNumber_SetGet()
    {
        var r = new TRec { RowNumber = 5 };
        Assert.AreEqual(5, r.RowNumber);
    }

    [TestMethod]
    public void PropChanged_TraineeId()
    {
        var item = new TRec();
        string? p = null;
        item.PropertyChanged += (_, e) => p = e.PropertyName;
        item.TraineeId = "TR001";
        Assert.AreEqual("TraineeId", p);
    }

    [TestMethod]
    public void PropChanged_SameValue_NoFire()
    {
        var item = new TRec { ClassId = "TA04" };
        bool fired = false;
        item.PropertyChanged += (_, _) => fired = true;
        item.ClassId = "TA04";
        Assert.IsFalse(fired);
    }

    static string CTT(int t) => t>0?$"{t} \u0e04\u0e23\u0e31\u0e49\u0e07":"-";
    static string CRT(int r) => r>0?$"\u0e3f{r:N0}":"-";
    static string TD(string? n) => string.IsNullOrWhiteSpace(n)?"\u0e44\u0e21\u0e48\u0e23\u0e30\u0e1a\u0e38":n;

    static TRec MakeRecord(string tid, string cid, DateTime date, string? tname, string? tphone, string? cname, int ctime, int crate, string? trname, string? trid)
        => new() { TraineeId=tid, ClassId=cid, RegisDate=date, TraineeName=tname??"", TraineePhone=tphone??"", ClassName=cname??"", ClassTime=ctime, ClassRate=crate, TrainerName=trname??"", TrainerId=trid??"" };

    class TRec : INotifyPropertyChanged
    {
        string _tid="",_cid="",_trid="",_tname="",_tphone="",_cname="",_trname="";
        int _ct,_cr,_rn; DateTime _rd=DateTime.Now;
        public string TraineeId { get=>_tid; set { if(_tid==value)return; _tid=value; F("TraineeId"); } }
        public string ClassId { get=>_cid; set { if(_cid==value)return; _cid=value; F("ClassId"); } }
        public string TrainerId { get=>_trid; set => _trid=value; }
        public DateTime RegisDate { get=>_rd; set => _rd=value; }
        public string TraineeName { get=>_tname; set => _tname=value; }
        public string TraineePhone { get=>_tphone; set => _tphone=value; }
        public string ClassName { get=>_cname; set => _cname=value; }
        public int ClassTime { get=>_ct; set => _ct=value; }
        public int ClassRate { get=>_cr; set => _cr=value; }
        public string TrainerName { get=>_trname; set => _trname=value; }
        public int RowNumber { get=>_rn; set { if(_rn==value)return; _rn=value; F("RowNumber"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}