using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TennisApp.Models;

/// <summary>
/// ประเภทคอร์ส เช่น TA=Adult, T1=Red &amp; Orange Ball
/// </summary>
public class CourseTypeItem : INotifyPropertyChanged
{
    private string _typeCode = string.Empty;
    private string _typeName = string.Empty;
    private string _typeNameThai = string.Empty;

    /// <summary>รหัสประเภท 2 ตัว เช่น "TA", "T1", "P1"</summary>
    public string TypeCode
    {
        get => _typeCode;
        set { _typeCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    /// <summary>ชื่อภาษาอังกฤษ เช่น "Adult"</summary>
    public string TypeName
    {
        get => _typeName;
        set { _typeName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    /// <summary>ชื่อภาษาไทย เช่น "ผู้ใหญ่"</summary>
    public string TypeNameThai
    {
        get => _typeNameThai;
        set { _typeNameThai = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    /// <summary>แสดงผลรวม เช่น "TA — Adult (ผู้ใหญ่)"</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(TypeNameThai)
        ? $"{TypeCode} — {TypeName}"
        : $"{TypeCode} — {TypeName} ({TypeNameThai})";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// แพ็กเกจ (จำนวนครั้ง + ราคา) ของประเภทคอร์สหนึ่งๆ
/// </summary>
public class CoursePackageItem : INotifyPropertyChanged
{
    private string _typeCode = string.Empty;
    private int _sessions;
    private int _price;

    /// <summary>รหัสประเภท FK → CourseType</summary>
    public string TypeCode
    {
        get => _typeCode;
        set { _typeCode = value; OnPropertyChanged(); }
    }

    /// <summary>จำนวนครั้ง: 0=รายเดือน, 1=ครั้งละ, 4/8/12/16=แพ็กเกจ</summary>
    public int Sessions
    {
        get => _sessions;
        set { _sessions = value; OnPropertyChanged(); OnPropertyChanged(nameof(SessionsDisplay)); OnPropertyChanged(nameof(ClassId)); }
    }

    /// <summary>ราคา (บาท)</summary>
    public int Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceDisplay)); }
    }

    /// <summary>รหัสคอร์ส เช่น "TA04"</summary>
    public string ClassId => $"{TypeCode}{Sessions:D2}";

    /// <summary>แสดงจำนวนครั้ง</summary>
    public string SessionsDisplay => Sessions switch
    {
        0 => "รายเดือน",
        1 => "ครั้งละ",
        _ => $"{Sessions} ครั้ง"
    };

    /// <summary>แสดงราคา</summary>
    public string PriceDisplay => $"฿{Price:N0}";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
