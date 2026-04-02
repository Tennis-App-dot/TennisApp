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

    // ═══════════════════════════════════════════════════════════
    // maintenance_date: วันที่ปรับปรุงสนามจริง (ผู้ใช้เลือกจาก DatePicker)
    // ═══════════════════════════════════════════════════════════

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
                OnPropertyChanged(nameof(MaintenanceDateText));
            }
        }
    }

    /// <summary>DateTimeOffset สำหรับ DatePicker (WinUI requires DateTimeOffset)</summary>
    public DateTimeOffset MaintenanceDateForPicker
    {
        get => new DateTimeOffset(_maintenanceDate);
        set
        {
            var newDate = value.Date;
            if (_maintenanceDate.Date != newDate)
            {
                MaintenanceDate = newDate;
            }
        }
    }

    /// <summary>DateTimeOffset? สำหรับ CalendarDatePicker (requires nullable DateTimeOffset)</summary>
    public DateTimeOffset? MaintenanceDateForCalendarPicker
    {
        get => _maintenanceDate == default ? null : new DateTimeOffset(_maintenanceDate);
        set
        {
            var newDate = value?.Date ?? default;
            if (_maintenanceDate.Date != newDate)
            {
                MaintenanceDate = newDate;
            }
        }
    }

    /// <summary>แสดงในหน้า List: "วันที่ปรับปรุงล่าสุด: 10/06/2025"</summary>
    public string MaintenanceDateText => 
        _maintenanceDate == default 
            ? "ยังไม่มีข้อมูลการปรับปรุง" 
            : $"วันที่ปรับปรุงล่าสุด: {_maintenanceDate:dd/MM/yyyy}";

    // ═══════════════════════════════════════════════════════════
    // last_updated: วันที่+เวลากดบันทึกในระบบ (อัตโนมัติ)
    // ═══════════════════════════════════════════════════════════

    public DateTime LastUpdated 
    { 
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastModifiedText));
            }
        }
    }

    /// <summary>แสดงในหน้า Edit: "แก้ไขข้อมูลล่าสุด: 18/06/2025 14:30"</summary>
    public string LastModifiedText => $"แก้ไขข้อมูลล่าสุด: {_lastUpdated:dd/MM/yyyy HH:mm}";

    /// <summary>สำหรับแปลง DateTime เป็นรูปแบบ SQLite</summary>
    public string LastUpdatedForDatabase => LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");

    // ═══════════════════════════════════════════════════════════
    // Image
    // ═══════════════════════════════════════════════════════════

    public string ImagePath { get; set; } = "ms-appx:///Assets/Courts/court1.jpg";

    public byte[]? ImageData
    {
        get => _imageData;
        set
        {
            _imageData = value;
            OnPropertyChanged();
            _ = UpdateImageSourceAsync();
        }
    }

    public BitmapImage? ImageSource
    {
        get => _imageSource;
        private set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Display
    // ═══════════════════════════════════════════════════════════

    public string DisplayName => $"สนาม {CourtID}";

    // ═══════════════════════════════════════════════════════════
    // Factory + Clone
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// สร้าง CourtItem จากข้อมูล SQLite (2 ฟิลด์แยกกัน)
    /// </summary>
    public static CourtItem FromDatabase(string courtId, byte[]? imageData, string status, 
        string? maintenanceDateStr, string lastUpdatedStr)
    {
        var courtItem = new CourtItem
        {
            CourtID = courtId,
            ImageData = imageData,
            Status = status
        };

        // Parse maintenance_date (วันที่ปรับปรุงสนาม)
        if (!string.IsNullOrEmpty(maintenanceDateStr) && DateTime.TryParse(maintenanceDateStr, out var parsedMaintenanceDate))
        {
            courtItem.MaintenanceDate = parsedMaintenanceDate;
        }

        // Parse last_updated (วันที่กดบันทึก)
        if (DateTime.TryParse(lastUpdatedStr, out var parsedLastUpdated))
        {
            courtItem.LastUpdated = parsedLastUpdated;
        }
        else
        {
            courtItem.LastUpdated = DateTime.Now;
        }

        return courtItem;
    }

    public CourtItem Clone() => new CourtItem
    {
        CourtID = this.CourtID,
        Status = this.Status,
        MaintenanceDate = this.MaintenanceDate,
        LastUpdated = this.LastUpdated,
        ImagePath = this.ImagePath,
        ImageData = this.ImageData
    };

    // ═══════════════════════════════════════════════════════════
    // Internal
    // ═══════════════════════════════════════════════════════════

    private async Task UpdateImageSourceAsync()
    {
        try
        {
            if (_imageData != null && _imageData.Length > 0)
            {
                var bitmap = await TennisApp.Helpers.ImageHelper.CreateBitmapFromBytesAsync(_imageData);
                ImageSource = bitmap;
            }
            else
            {
                try
                {
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
                }
                catch
                {
                    ImageSource = null;
                }
            }
        }
        catch
        {
            try
            {
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
            }
            catch
            {
                ImageSource = null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
