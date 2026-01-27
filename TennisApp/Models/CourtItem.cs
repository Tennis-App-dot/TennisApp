using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace TennisApp.Models;

public class CourtItem : INotifyPropertyChanged
{
    private byte[]? _imageData;
    private BitmapImage? _imageSource;
    private DateTime _lastUpdated = DateTime.Now;
    private DateTime _maintenanceDate = DateTime.Today;

    // court_id: "01".."99" (ตาม database schema)
    public string CourtID { get; set; } = string.Empty;

    // court_status: "1" = พร้อมใช้งาน, "0" = กำลังปิดปรับปรุง (ตาม database schema)
    public string Status { get; set; } = "1";

    // last_updated: วันที่เปลี่ยนแปลงข้อมูลล่าสุด (ตาม database schema)
    public DateTime LastUpdated 
    { 
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUpdatedForDatePicker));
                OnPropertyChanged(nameof(LastMaintenanceDateText));
                OnPropertyChanged(nameof(LastModifiedText));
                
                System.Diagnostics.Debug.WriteLine($"📅 LastUpdated changed to: {_lastUpdated:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }

    // ✅ DateTimeOffset property สำหรับ DatePicker (WinUI requires DateTimeOffset)
    public DateTimeOffset LastUpdatedForDatePicker
    {
        get => new DateTimeOffset(_lastUpdated);
        set
        {
            var newDateTime = value.DateTime;
            if (_lastUpdated.Date != newDateTime.Date) // เปรียบเทียบแค่วันที่
            {
                // เก็บเวลาเดิม แต่เปลี่ยนวันที่
                _lastUpdated = new DateTime(
                    newDateTime.Year, newDateTime.Month, newDateTime.Day,
                    _lastUpdated.Hour, _lastUpdated.Minute, _lastUpdated.Second);
                
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastUpdated));
                OnPropertyChanged(nameof(LastMaintenanceDateText));
                OnPropertyChanged(nameof(LastModifiedText));
            }
        }
    }

    // ✅ maintenance_date: วันที่ปรับปรุงสนามจริง (ผู้ใช้เลือกได้)
    public DateTime MaintenanceDate
    {
        get => _maintenanceDate;
        set
        {
            if (_maintenanceDate != value)
            {
                _maintenanceDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaintenanceDateForPicker));
                OnPropertyChanged(nameof(LastMaintenanceDateText));
                
                System.Diagnostics.Debug.WriteLine($"🔧 MaintenanceDate changed to: {_maintenanceDate:yyyy-MM-dd}");
            }
        }
    }

    // ✅ DateTimeOffset property สำหรับ DatePicker ของวันที่ปรับปรุงสนาม
    public DateTimeOffset MaintenanceDateForPicker
    {
        get 
        {
            var result = new DateTimeOffset(_maintenanceDate);
            return result;
        }
        set
        {
            var newDate = value.Date;
            if (_maintenanceDate.Date != newDate)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 MaintenanceDateForPicker changed: {newDate:yyyy-MM-dd}");
                MaintenanceDate = newDate;
            }
        }
    }

    // court_img: สำหรับ UI ใช้ path แทน BLOB (จะแปลงตอนบันทึก/อ่าน database)
    public string ImagePath { get; set; } = "ms-appx:///Assets/Courts/court1.jpg";

    // court_img: BLOB data สำหรับ database (nullable)
    public byte[]? ImageData
    {
        get => _imageData;
        set
        {
            _imageData = value;
            OnPropertyChanged();

            // อัปเดต ImageSource เมื่อ ImageData เปลี่ยน
            _ = UpdateImageSourceAsync();
        }
    }

    // ImageSource สำหรับแสดงผลใน UI
    public BitmapImage? ImageSource
    {
        get => _imageSource;
        private set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    // ใช้แสดงบน UI แทน CourtName
    public string DisplayName => $"สนาม {CourtID}";

    // สำหรับแสดงในรายการ Court (แสดงวันที่ที่ผู้ใช้เลือก)
    public string LastMaintenanceDateText => $"วันที่ปรับปรุงล่าสุด: {LastUpdated:dd/MM/yyyy}";

    // สำหรับแสดงในหน้า Edit (แสดงวันที่ปัจจุบัน + เวลาจาก LastUpdated)
    public string LastModifiedText
    {
        get
        {
            // ใช้วันที่ปัจจุบัน + เวลาที่บันทึกจริง
            var today = DateTime.Today;
            var timeFromLastUpdated = LastUpdated.TimeOfDay;
            var displayDateTime = today.Add(timeFromLastUpdated);
            
            return $"แก้ไขข้อมูลล่าสุด: {displayDateTime:dd/MM/yyyy HH:mm}";
        }
    }

    /// <summary>
    /// อัปเดต ImageSource จาก ImageData
    /// </summary>
    private async Task UpdateImageSourceAsync()
    {
        try
        {
            if (_imageData != null && _imageData.Length > 0)
            {
                var bitmap = await TennisApp.Helpers.ImageHelper.CreateBitmapFromBytesAsync(_imageData);
                ImageSource = bitmap; // bitmap can be null, which is fine
            }
            else
            {
                // ใช้รูปเดิม
                try
                {
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Cannot load default image: {ex.Message}");
                    ImageSource = null; // ถ้าโหลดรูปเดิมไม่ได้ก็ให้เป็น null
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ UpdateImageSourceAsync Error: {ex.Message}");
            // หากมีปัญหา ใช้รูปเดิม
            try
            {
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
            }
            catch
            {
                ImageSource = null; // ถ้าโหลดรูปเดิมไม่ได้เลยก็ให้เป็น null
            }
        }
    }

    /// <summary>
    /// สำหรับแปลง DateTime เป็นรูปแบบ SQLite
    /// </summary>
    public string LastUpdatedForDatabase => LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// สร้าง CourtItem จากข้อมูล SQLite
    /// </summary>
    public static CourtItem FromDatabase(string courtId, byte[]? imageData, string status, string lastUpdatedString)
    {
        var courtItem = new CourtItem
        {
            CourtID = courtId,
            ImageData = imageData,
            Status = status
        };

        // แปลง string จาก database กลับเป็น DateTime
        if (DateTime.TryParse(lastUpdatedString, out var parsedDate))
        {
            courtItem.LastUpdated = parsedDate;
        }
        else
        {
            courtItem.LastUpdated = DateTime.Now;
        }

        return courtItem;
    }

    // ใช้สร้างสำเนาสำหรับโหมดแก้ไข (Edit)
    public CourtItem Clone() => new CourtItem
    {
        CourtID = this.CourtID,
        Status = this.Status,
        LastUpdated = this.LastUpdated,
        ImagePath = this.ImagePath,
        ImageData = this.ImageData
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
