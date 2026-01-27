using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.ViewModels;

public partial class CourtPageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly ObservableCollection<CourtItem> _allCourts = new();
    
    public ObservableCollection<CourtItem> Courts { get; } = new();

    public CourtPageViewModel()
    {
        System.Diagnostics.Debug.WriteLine("CourtPageViewModel constructor เริ่มทำงาน");
        
        try
        {
            _databaseService = new DatabaseService();
            System.Diagnostics.Debug.WriteLine("DatabaseService สร้างสำเร็จ");
            System.Diagnostics.Debug.WriteLine("CourtPageViewModel สร้างแล้ว - พร้อมเชื่อมต่อ Database");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CourtPageViewModel constructor error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    public async Task LoadCourtsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadCourtsAsync เริ่มโหลดข้อมูล...");
            
            var courtsFromDb = await _databaseService.Courts.GetAllCourtsAsync().ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"พบข้อมูลจาก Database: {courtsFromDb.Count} รายการ");
            
            _allCourts.Clear();
            Courts.Clear();
            
            foreach (var court in courtsFromDb)
            {
                System.Diagnostics.Debug.WriteLine($"   - สนาม {court.CourtID}: {court.Status} ({court.LastUpdated:dd/MM/yyyy HH:mm})");
                _allCourts.Add(court);
                Courts.Add(court);
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ โหลดข้อมูลเสร็จ - UI แสดง {Courts.Count} รายการ");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courts: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    public async Task ApplyFilterAsync(string filterType)
    {
        try
        {
            Courts.Clear();

            var filteredItems = filterType switch
            {
                "active" => await _databaseService.Courts.GetCourtsByStatusAsync("1").ConfigureAwait(false),
                "maintenance" => await _databaseService.Courts.GetCourtsByStatusAsync("0").ConfigureAwait(false),
                _ => await _databaseService.Courts.GetAllCourtsAsync().ConfigureAwait(false)
            };

            // อัปเดต _allCourts ด้วยถ้าเป็น "all"
            if (filterType == "all")
            {
                _allCourts.Clear();
                foreach (var item in filteredItems)
                {
                    _allCourts.Add(item);
                }
            }

            foreach (var item in filteredItems)
            {
                Courts.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error applying filter: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task<bool> AddCourtAsync(CourtItem newCourt)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🏗️ เริ่มเพิ่มสนามใหม่...");
            
            // ใช้ NextAvailableCourtId จาก database
            var nextId = await _databaseService.Courts.GetNextAvailableCourtIdAsync().ConfigureAwait(false);
            newCourt.CourtID = nextId;
            
            System.Diagnostics.Debug.WriteLine($"📝 CourtID: {nextId}");
            System.Diagnostics.Debug.WriteLine($"📝 Status: {newCourt.Status}");
            System.Diagnostics.Debug.WriteLine($"📝 LastUpdated: {newCourt.LastUpdated}");
            System.Diagnostics.Debug.WriteLine($"📝 ImageData length: {newCourt.ImageData?.Length ?? 0}");
            
            var success = await _databaseService.Courts.AddCourtAsync(newCourt).ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"💾 Database บันทึก: {(success ? "✅ สำเร็จ" : "❌ ล้มเหลว")}");
            
            if (success)
            {
                _allCourts.Add(newCourt);
                Courts.Add(newCourt);
                
                System.Diagnostics.Debug.WriteLine($"📊 จำนวนสนามใน UI: {Courts.Count}");
                return true;
            }
            
            return false;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error adding court: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    [RelayCommand]
    public async Task<bool> UpdateCourtAsync(CourtItem court)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔄 เริ่มแก้ไขสนาม {court.CourtID}...");
            System.Diagnostics.Debug.WriteLine($"📝 Status: {court.Status}");
            System.Diagnostics.Debug.WriteLine($"📝 LastUpdated: {court.LastUpdated}");
            System.Diagnostics.Debug.WriteLine($"📝 ImageData: {court.ImageData?.Length ?? 0} bytes");
            
            var success = await _databaseService.Courts.UpdateCourtAsync(court).ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"💾 Database อัปเดต: {(success ? "✅ สำเร็จ" : "❌ ล้มเหลว")}");
            
            if (success)
            {
                // อัปเดตใน memory collections
                var existingInAll = _allCourts.FirstOrDefault(c => c.CourtID == court.CourtID);
                var existingInDisplay = Courts.FirstOrDefault(c => c.CourtID == court.CourtID);

                if (existingInAll != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 อัปเดต _allCourts สำหรับสนาม {court.CourtID}");
                    existingInAll.Status = court.Status;
                    existingInAll.LastUpdated = court.LastUpdated;
                    existingInAll.ImagePath = court.ImagePath;
                    existingInAll.ImageData = court.ImageData;
                }

                if (existingInDisplay != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 อัปเดต Courts collection สำหรับสนาม {court.CourtID}");
                    existingInDisplay.Status = court.Status;
                    existingInDisplay.LastUpdated = court.LastUpdated;
                    existingInDisplay.ImagePath = court.ImagePath;
                    existingInDisplay.ImageData = court.ImageData;
                }

                System.Diagnostics.Debug.WriteLine($"📊 จำนวนสนามใน UI หลังอัปเดต: {Courts.Count}");
                return true;
            }
            
            return false;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error updating court: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    [RelayCommand]
    public async Task<bool> RemoveCourtAsync(CourtItem court)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ เริ่มลบสนาม {court.CourtID}...");
            
            var success = await _databaseService.Courts.DeleteCourtAsync(court.CourtID).ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"💾 Database ลบ: {(success ? "✅ สำเร็จ" : "❌ ล้มเหลว")}");
            
            if (success)
            {
                // ลบจาก memory collections
                var removedFromAll = _allCourts.Remove(court);
                var removedFromDisplay = Courts.Remove(court);
                
                System.Diagnostics.Debug.WriteLine($"ลบจาก _allCourts: {removedFromAll}");
                System.Diagnostics.Debug.WriteLine($"ลบจาก Courts: {removedFromDisplay}");
                System.Diagnostics.Debug.WriteLine($"จำนวนสนามเหลือ: {Courts.Count}");
                
                return true;
            }
            
            return false;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error removing court: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    [RelayCommand]
    public async Task<(bool success, string nextId)> GetNextCourtIdAsync()
    {
        try
        {
            var nextId = await _databaseService.Courts.GetNextAvailableCourtIdAsync().ConfigureAwait(false);
            return (true, nextId);
        }
        catch
        {
            return (false, string.Empty);
        }
    }
}
