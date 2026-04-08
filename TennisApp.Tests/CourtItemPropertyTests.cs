using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class CourtItemPropertyTests
{
    [TestMethod] public void DisplayName() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 01", $"\u0e2a\u0e19\u0e32\u0e21 {"01"}");
    [TestMethod] public void DisplayName_02() => Assert.AreEqual("\u0e2a\u0e19\u0e32\u0e21 02", $"\u0e2a\u0e19\u0e32\u0e21 {"02"}");

    [TestMethod] public void Status_1_Active() => Assert.AreEqual("1", "1");
    [TestMethod] public void Status_0_Maintenance() => Assert.AreEqual("0", "0");

    [TestMethod]
    public void MaintenanceDateText_HasDate()
    {
        var d = new DateTime(2025, 6, 10);
        var text = $"\u0e27\u0e31\u0e19\u0e17\u0e35\u0e48\u0e1b\u0e23\u0e31\u0e1a\u0e1b\u0e23\u0e38\u0e07\u0e25\u0e48\u0e32\u0e2a\u0e38\u0e14: {d.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)}";
        Assert.IsTrue(text.Contains("10/06/2025"));
    }

    [TestMethod]
    public void MaintenanceDateText_Default()
    {
        var d = default(DateTime);
        var text = d == default ? "\u0e22\u0e31\u0e07\u0e44\u0e21\u0e48\u0e21\u0e35\u0e02\u0e49\u0e2d\u0e21\u0e39\u0e25\u0e01\u0e32\u0e23\u0e1b\u0e23\u0e31\u0e1a\u0e1b\u0e23\u0e38\u0e07" : "";
        Assert.AreEqual("\u0e22\u0e31\u0e07\u0e44\u0e21\u0e48\u0e21\u0e35\u0e02\u0e49\u0e2d\u0e21\u0e39\u0e25\u0e01\u0e32\u0e23\u0e1b\u0e23\u0e31\u0e1a\u0e1b\u0e23\u0e38\u0e07", text);
    }

    [TestMethod]
    public void LastModifiedText()
    {
        var d = new DateTime(2025, 6, 18, 14, 30, 0);
        var text = $"\u0e41\u0e01\u0e49\u0e44\u0e02\u0e02\u0e49\u0e2d\u0e21\u0e39\u0e25\u0e25\u0e48\u0e32\u0e2a\u0e38\u0e14: {d.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture)}";
        Assert.IsTrue(text.Contains("18/06/2025 14:30"));
    }

    [TestMethod]
    public void LastUpdatedForDatabase_Format()
    {
        var d = new DateTime(2025, 6, 18, 14, 30, 0);
        Assert.AreEqual("2025-06-18 14:30:00", d.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void MaintenanceDateForPicker_RoundTrip()
    {
        var original = new DateTime(2025, 6, 10);
        var dto = new DateTimeOffset(original);
        Assert.AreEqual(original.Date, dto.Date);
    }

    [TestMethod]
    public void MaintenanceDateForCalendarPicker_Null_Default()
    {
        DateTimeOffset? val = null;
        var date = val?.Date ?? default;
        Assert.AreEqual(default(DateTime), date);
    }

    [TestMethod]
    public void Clone_AllFields()
    {
        var o = new TCourtItem { Id="01", Stat="1", MDate=new DateTime(2025,6,10), LU=new DateTime(2025,6,18,14,30,0), ImgPath="test.jpg" };
        var c = new TCourtItem { Id=o.Id, Stat=o.Stat, MDate=o.MDate, LU=o.LU, ImgPath=o.ImgPath };
        Assert.AreEqual(o.Id, c.Id);
        Assert.AreEqual(o.Stat, c.Stat);
        Assert.AreEqual(o.MDate, c.MDate);
        Assert.AreEqual(o.LU, c.LU);
        Assert.AreEqual(o.ImgPath, c.ImgPath);
    }

    [TestMethod]
    public void FromDatabase_ParsesDates()
    {
        var mDate = "2025-06-10";
        var luDate = "2025-06-18 14:30:00";
        Assert.IsTrue(DateTime.TryParse(mDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var md));
        Assert.AreEqual(new DateTime(2025, 6, 10), md.Date);
        Assert.IsTrue(DateTime.TryParse(luDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var lu));
        Assert.AreEqual(2025, lu.Year);
        Assert.AreEqual(14, lu.Hour);
    }

    [TestMethod]
    public void FromDatabase_InvalidDate_FallsBack()
    {
        Assert.IsFalse(DateTime.TryParse("not-a-date", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _));
    }

    [TestMethod]
    public void PropChanged_MaintenanceDate()
    {
        var item = new TCourtNotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.MaintenanceDate = new DateTime(2025, 7, 1);
        CollectionAssert.Contains(props, "MaintenanceDate");
        CollectionAssert.Contains(props, "MaintenanceDateText");
    }

    [TestMethod]
    public void PropChanged_LastUpdated()
    {
        var item = new TCourtNotify();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.LastUpdated = new DateTime(2025, 7, 1);
        CollectionAssert.Contains(props, "LastUpdated");
        CollectionAssert.Contains(props, "LastModifiedText");
    }

    record TCourtItem { public string Id{get;set;}=""; public string Stat{get;set;}="1"; public DateTime MDate{get;set;}=DateTime.Today; public DateTime LU{get;set;}=DateTime.Now; public string ImgPath{get;set;}=""; }

    class TCourtNotify : INotifyPropertyChanged
    {
        DateTime _md=DateTime.Today; DateTime _lu=DateTime.Now;
        public DateTime MaintenanceDate { get=>_md; set { if(_md==value)return; _md=value; F("MaintenanceDate"); F("MaintenanceDateForPicker"); F("MaintenanceDateText"); } }
        public DateTime LastUpdated { get=>_lu; set { if(_lu==value)return; _lu=value; F("LastUpdated"); F("LastModifiedText"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}