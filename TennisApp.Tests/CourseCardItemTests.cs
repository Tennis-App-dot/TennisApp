using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class CourseCardItemTests
{
    [TestMethod] public void PriceText_2200() => Assert.AreEqual("\u0e3f2,200", PT(2200));
    [TestMethod] public void PriceText_600() => Assert.AreEqual("\u0e3f600", PT(600));
    [TestMethod] public void PriceText_0() => Assert.AreEqual("-", PT(0));
    [TestMethod] public void PriceText_Neg() => Assert.AreEqual("-", PT(-100));

    [TestMethod] public void RegText_0() => Assert.AreEqual("0 \u0e04\u0e19\u0e25\u0e07", RT(0));
    [TestMethod] public void RegText_5() => Assert.AreEqual("5 \u0e04\u0e19\u0e25\u0e07", RT(5));
    [TestMethod] public void RegText_100() => Assert.AreEqual("100 \u0e04\u0e19\u0e25\u0e07", RT(100));

    [TestMethod] public void HasTrainer_True() => Assert.IsTrue(!string.IsNullOrWhiteSpace("\u0e04\u0e23\u0e39\u0e40\u0e2d"));
    [TestMethod] public void HasTrainer_Empty() => Assert.IsFalse(!string.IsNullOrWhiteSpace(""));
    [TestMethod] public void HasTrainer_Spaces() => Assert.IsFalse(!string.IsNullOrWhiteSpace("   "));

    [TestMethod] public void Session_4() => Assert.AreEqual("4 \u0e04\u0e23\u0e31\u0e49\u0e07", SX(4));
    [TestMethod] public void Session_0() => Assert.AreEqual("\u0e23\u0e32\u0e22\u0e40\u0e14\u0e37\u0e2d\u0e19", SX(0));
    [TestMethod] public void Session_1() => Assert.AreEqual("\u0e04\u0e23\u0e31\u0e49\u0e07\u0e25\u0e30", SX(1));

    [TestMethod] public void CompositeKey() => Assert.AreEqual("TA04|T001", $"{"TA04"}|{"T001"}");

    [TestMethod]
    public void IsSelected_Default_False()
    {
        var c = new TCard();
        Assert.IsFalse(c.IsSelected);
    }

    [TestMethod]
    public void IsSelected_Toggle()
    {
        var c = new TCard();
        c.IsSelected = true;
        Assert.IsTrue(c.IsSelected);
    }

    [TestMethod]
    public void IsSelected_PropChanged()
    {
        var c = new TCard();
        string? p = null;
        c.PropertyChanged += (_, e) => p = e.PropertyName;
        c.IsSelected = true;
        Assert.AreEqual("IsSelected", p);
    }

    [TestMethod]
    public void RegCount_PropChanged()
    {
        var c = new TCard();
        var props = new System.Collections.Generic.List<string>();
        c.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        c.RegCount = 10;
        CollectionAssert.Contains(props, "RegCount");
        CollectionAssert.Contains(props, "RegCountText");
    }

    [TestMethod]
    public void RegCount_SameValue_NoFire()
    {
        var c = new TCard{RegCount=5};
        bool fired = false;
        c.PropertyChanged += (_, _) => fired = true;
        c.RegCount = 5;
        Assert.IsFalse(fired);
    }

    static string PT(int p) => p>0?$"\u0e3f{p:N0}":"-";
    static string RT(int c) => $"{c} \u0e04\u0e19\u0e25\u0e07";
    static string SX(int s) => s switch{0=>"\u0e23\u0e32\u0e22\u0e40\u0e14\u0e37\u0e2d\u0e19",1=>"\u0e04\u0e23\u0e31\u0e49\u0e07\u0e25\u0e30",_=>$"{s} \u0e04\u0e23\u0e31\u0e49\u0e07"};

    class TCard : INotifyPropertyChanged
    {
        bool _sel; int _rc;
        public bool IsSelected { get=>_sel; set { if(_sel==value)return; _sel=value; F("IsSelected"); } }
        public int RegCount { get=>_rc; set { if(_rc==value)return; _rc=value; F("RegCount"); F("RegCountText"); } }
        public string RegCountText => $"{_rc} \u0e04\u0e19\u0e25\u0e07";
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}