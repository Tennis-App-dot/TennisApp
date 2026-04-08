using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TennisApp.Tests;

[TestClass]
public class CourseTypePackageTests
{
    // CourseTypeItem
    [TestMethod] public void Type_DisplayName_WithThai() => Assert.AreEqual("TA \u2014 Adult (\u0e1c\u0e39\u0e49\u0e43\u0e2b\u0e0d\u0e48)", CTD("TA","Adult","\u0e1c\u0e39\u0e49\u0e43\u0e2b\u0e0d\u0e48"));
    [TestMethod] public void Type_DisplayName_NoThai() => Assert.AreEqual("TA \u2014 Adult", CTD("TA","Adult",""));
    [TestMethod] public void Type_DisplayName_Spaces() => Assert.AreEqual("T1 \u2014 Red", CTD("T1","Red","   "));

    [TestMethod]
    public void Type_PropChanged_TypeCode()
    {
        var item = new TCT();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.TypeCode = "TA";
        CollectionAssert.Contains(props, "TypeCode");
        CollectionAssert.Contains(props, "DisplayName");
    }

    [TestMethod]
    public void Type_PropChanged_TypeName()
    {
        var item = new TCT();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.TypeName = "Adult";
        CollectionAssert.Contains(props, "TypeName");
        CollectionAssert.Contains(props, "DisplayName");
    }

    [TestMethod]
    public void Type_PropChanged_TypeNameThai()
    {
        var item = new TCT();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.TypeNameThai = "\u0e1c\u0e39\u0e49\u0e43\u0e2b\u0e0d\u0e48";
        CollectionAssert.Contains(props, "TypeNameThai");
        CollectionAssert.Contains(props, "DisplayName");
    }

    // CoursePackageItem
    [TestMethod] public void Pkg_ClassId_TA04() => Assert.AreEqual("TA04", PCI("TA",4));
    [TestMethod] public void Pkg_ClassId_T300() => Assert.AreEqual("T300", PCI("T3",0));
    [TestMethod] public void Pkg_ClassId_T108() => Assert.AreEqual("T108", PCI("T1",8));
    [TestMethod] public void Pkg_ClassId_T212() => Assert.AreEqual("T212", PCI("T2",12));

    [TestMethod] public void Pkg_Sessions_Monthly() => Assert.AreEqual("\u0e23\u0e32\u0e22\u0e40\u0e14\u0e37\u0e2d\u0e19", PS(0));
    [TestMethod] public void Pkg_Sessions_PerTime() => Assert.AreEqual("\u0e04\u0e23\u0e31\u0e49\u0e07\u0e25\u0e30", PS(1));
    [TestMethod] public void Pkg_Sessions_4() => Assert.AreEqual("4 \u0e04\u0e23\u0e31\u0e49\u0e07", PS(4));
    [TestMethod] public void Pkg_Sessions_8() => Assert.AreEqual("8 \u0e04\u0e23\u0e31\u0e49\u0e07", PS(8));
    [TestMethod] public void Pkg_Sessions_16() => Assert.AreEqual("16 \u0e04\u0e23\u0e31\u0e49\u0e07", PS(16));

    [TestMethod] public void Pkg_PriceDisplay_2200() => Assert.AreEqual("\u0e3f2,200", $"\u0e3f{2200:N0}");
    [TestMethod] public void Pkg_PriceDisplay_600() => Assert.AreEqual("\u0e3f600", $"\u0e3f{600:N0}");
    [TestMethod] public void Pkg_PriceDisplay_13000() => Assert.AreEqual("\u0e3f13,000", $"\u0e3f{13000:N0}");

    [TestMethod]
    public void Pkg_PropChanged_Sessions()
    {
        var item = new TCP();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Sessions = 8;
        CollectionAssert.Contains(props, "Sessions");
        CollectionAssert.Contains(props, "SessionsDisplay");
        CollectionAssert.Contains(props, "ClassId");
    }

    [TestMethod]
    public void Pkg_PropChanged_Price()
    {
        var item = new TCP();
        var props = new System.Collections.Generic.List<string>();
        item.PropertyChanged += (_, e) => props.Add(e.PropertyName!);
        item.Price = 2200;
        CollectionAssert.Contains(props, "Price");
        CollectionAssert.Contains(props, "PriceDisplay");
    }

    static string CTD(string code, string name, string thai) => string.IsNullOrWhiteSpace(thai)?$"{code} \u2014 {name}":$"{code} \u2014 {name} ({thai})";
    static string PCI(string type, int sessions) => $"{type}{sessions:D2}";
    static string PS(int s) => s switch{0=>"\u0e23\u0e32\u0e22\u0e40\u0e14\u0e37\u0e2d\u0e19",1=>"\u0e04\u0e23\u0e31\u0e49\u0e07\u0e25\u0e30",_=>$"{s} \u0e04\u0e23\u0e31\u0e49\u0e07"};

    class TCT : INotifyPropertyChanged
    {
        string _c="",_n="",_t="";
        public string TypeCode { get=>_c; set { _c=value; F("TypeCode"); F("DisplayName"); } }
        public string TypeName { get=>_n; set { _n=value; F("TypeName"); F("DisplayName"); } }
        public string TypeNameThai { get=>_t; set { _t=value; F("TypeNameThai"); F("DisplayName"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    class TCP : INotifyPropertyChanged
    {
        string _tc=""; int _s,_p;
        public string TypeCode { get=>_tc; set { _tc=value; F("TypeCode"); } }
        public int Sessions { get=>_s; set { _s=value; F("Sessions"); F("SessionsDisplay"); F("ClassId"); } }
        public int Price { get=>_p; set { _p=value; F("Price"); F("PriceDisplay"); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        void F(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}