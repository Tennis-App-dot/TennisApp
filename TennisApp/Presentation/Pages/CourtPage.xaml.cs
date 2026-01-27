using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Text;
using System;
using System.Linq;
using System.Threading.Tasks;
using TennisApp.Models;
using TennisApp.Presentation.Dialogs;
using TennisApp.Presentation.ViewModels;
using Microsoft.UI.Xaml;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourtPage : Page
{
    public CourtPageViewModel VM { get; } = new();

    public CourtPage()
    {
        System.Diagnostics.Debug.WriteLine("CourtPage constructor - เริ่มสร้าง CourtPage");
        
        InitializeComponent();
        DataContext = VM;
        
        System.Diagnostics.Debug.WriteLine("CourtPage InitializeComponent เสร็จ");
        System.Diagnostics.Debug.WriteLine($"DataContext set to VM: {VM != null}");
        
        // 🆕 โหลดข้อมูลจาก Database หลัง UI พร้อม
        this.Loaded += CourtPage_Loaded;
        
        System.Diagnostics.Debug.WriteLine("CourtPage constructor เสร็จสมบูรณ์");
    }

    /// <summary>
    /// โหลดข้อมูลจาก Database เมื่อ Page พร้อม
    /// </summary>
    private async void CourtPage_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("CourtPage Loaded - เริ่มโหลดข้อมูลจาก Database");
        
#if DEBUG
        // แสดง Debug panel เฉพาะใน Debug build
        if (FindName("DebugPanel") is StackPanel debugPanel)
        {
            debugPanel.Visibility = Visibility.Visible;
        }
#else
        // ซ่อน Debug panel ใน Release build
        if (FindName("DebugPanel") is StackPanel debugPanel)
        {
            debugPanel.Visibility = Visibility.Collapsed;
        }
#endif
        
        try
        {
            // โหลดข้อมูลจาก Database จริง
            await VM.LoadCourtsAsync();
            
            System.Diagnostics.Debug.WriteLine($"โหลดเสร็จ - แสดง {VM.Courts.Count} สนาม");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"โหลดข้อมูลล้มเหลว: {ex.Message}");
            
            // แสดงข้อผิดพลาดให้ผู้ใช้
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถโหลดข้อมูลสนามได้: {ex.Message}");
        }
    }

    // จัดการเหตุการณ์ Radio Button เปลี่ยนแปลง
    private void FilterRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radio) return;

        string filterType = radio.Name switch
        {
            "RadioAll" => "all",
            "RadioActive" => "active", 
            "RadioMaintenance" => "maintenance",
            _ => "all"
        };

        // ใช้ async version
        _ = VM.ApplyFilterAsync(filterType);
    }

    // เพิ่มสนามใหม่
    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("BtnAdd_Click เริ่มทำงาน");
            
            var result = await VM.GetNextCourtIdAsync();
            if (!result.success)
            {
                await ShowMessage("ครบจำนวนสูงสุด", "มีสนามครบ 99 สนามแล้ว ไม่สามารถเพิ่มได้");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Next Court ID: {result.nextId}");

            // สร้าง CourtItem ใหม่โดยไม่กำหนด CourtID เพื่อให้ระบบรู้ว่าเป็นโหมดเพิ่ม
            var seed = new CourtItem { Status = "1", LastUpdated = DateTime.Today };
            var dlg = new CourtFormDialog(seed) { XamlRoot = this.XamlRoot };

            System.Diagnostics.Debug.WriteLine("เปิด CourtFormDialog");

            await dlg.ShowAsync();
            
            // ✅ ใช้ WasSaved แทน ContentDialogResult.Primary
            if (dlg.WasSaved)
            {
                var item = dlg.Result;
                
                System.Diagnostics.Debug.WriteLine($"User กด Save - Status: {item.Status}, LastUpdated: {item.LastUpdated}");
                
                // บันทึกลง database
                var success = await VM.AddCourtAsync(item);
                
                System.Diagnostics.Debug.WriteLine($"AddCourtAsync result: {success}");
                
                if (!success)
                {
                    await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถบันทึกข้อมูลสนามได้");
                    return;
                }
                
                // รีเฟรชตัวกรองเพื่อให้แสดงผลถูกต้อง
                await RefreshCurrentFilterAsync();
                
                System.Diagnostics.Debug.WriteLine("RefreshCurrentFilterAsync เสร็จ");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User กด Cancel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BtnAdd_Click Error: {ex.Message}");
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถเพิ่มสนามได้: {ex.Message}");
        }
    }

    // แก้ไขสนาม
    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourtItem target) return;
        
        try
        {
            var copy = target.Clone();
            var dlg = new CourtFormDialog(copy) { XamlRoot = this.XamlRoot };

            await dlg.ShowAsync();
            
            // ✅ ใช้ WasSaved แทน ContentDialogResult.Primary
            if (dlg.WasSaved)
            {
                var edited = dlg.Result;
                
                // อัปเดตลง database
                var success = await VM.UpdateCourtAsync(edited);
                
                if (!success)
                {
                    await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถบันทึกการแก้ไขได้");
                    return;
                }
                
                // รีเฟรชตัวกรองเพื่อให้แสดงผลถูกต้อง
                await RefreshCurrentFilterAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถแก้ไขสนามได้: {ex.Message}");
        }
    }

    // ลบสนาม
    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourtItem target) return;

        try
        {
            bool confirmed = await ShowConfirm(
                "Confirm Delete!!!!",
                $"ต้องการลบสนาม {target.DisplayName} ใช่หรือไม่?"
            );

            if (confirmed)
            {
                var success = await VM.RemoveCourtAsync(target);
                
                if (!success)
                {
                    await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถลบสนามได้");
                    return;
                }
                
                // รีเฟรชตัวกรองเพื่อให้แสดงผลถูกต้อง
                await RefreshCurrentFilterAsync();
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถลบสนามได้: {ex.Message}");
        }
    }

    // รีเฟรชตัวกรองปัจจุบัน (Async version)
    private async Task RefreshCurrentFilterAsync()
    {
        string currentFilter = "all";
        
        try
        {
            var radioActive = FindName("RadioActive") as RadioButton;
            var radioMaintenance = FindName("RadioMaintenance") as RadioButton;
            
            if (radioActive?.IsChecked == true) 
                currentFilter = "active";
            else if (radioMaintenance?.IsChecked == true) 
                currentFilter = "maintenance";
            else
                currentFilter = "all";
        }
        catch
        {
            currentFilter = "all";
        }
        
        await VM.ApplyFilterAsync(currentFilter);
    }

    /// <summary>
    /// Gets Thai font with fallback to system font if font file is missing
    /// </summary>
    private Microsoft.UI.Xaml.Media.FontFamily GetThaiFont()
    {
        try
        {
            // Try to use the custom Thai font
            return new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai");
        }
        catch
        {
            // Fallback to system font that supports Thai
            return new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI");
        }
    }

    /// <summary>
    /// Shows a message dialog with Thai font support
    /// </summary>
    private async Task ShowMessage(string title, string content)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot
        };

        // Apply Thai font from application resources
        if (Application.Current?.Resources != null)
        {
            if (Application.Current.Resources.TryGetValue("ThaiFontFamily", out var fontResource))
            {
                dlg.FontFamily = fontResource as Microsoft.UI.Xaml.Media.FontFamily;
            }
        }

        await dlg.ShowAsync();
    }

    /// <summary>
    /// Shows a confirmation dialog with Thai font support
    /// </summary>
    private async Task<bool> ShowConfirm(string title, string content)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        // Apply Thai font from application resources
        if (Application.Current?.Resources != null)
        {
            if (Application.Current.Resources.TryGetValue("ThaiFontFamily", out var fontResource))
            {
                dlg.FontFamily = fontResource as Microsoft.UI.Xaml.Media.FontFamily;
            }
        }

        var result = await dlg.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

#if DEBUG
    // Debug function for testing Database
    private async void BtnDebugTest_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Starting Database Debug test...");
        
        try
        {
            var databaseService = new TennisApp.Services.DatabaseService();
            var courts = await databaseService.Courts.GetAllCourtsAsync();
            
            System.Diagnostics.Debug.WriteLine($"Successfully loaded {courts.Count} courts");
            
            // Refresh UI
            await VM.LoadCourtsAsync();
            
            await ShowMessage("Success", $"Database working normally\nFound {courts.Count} courts");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test failed: {ex.Message}");
            await ShowMessage("Error", $"Test failed: {ex.Message}");
        }
    }
    
    // Reset Database for Debug
    private async void BtnResetDatabase_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Reset Database starting...");
        
        try
        {
            var databaseService = new TennisApp.Services.DatabaseService();
            var dbPath = databaseService.GetDatabasePath();
            var connectionString = $"Data Source={dbPath}";
            
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Court";
            await command.ExecuteNonQueryAsync();
            
            // Refresh UI
            await VM.LoadCourtsAsync();
            
            System.Diagnostics.Debug.WriteLine("Reset Database completed");
            await ShowMessage("Success", "Database reset completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reset Database failed: {ex.Message}");
            await ShowMessage("Error", $"Reset Database failed: {ex.Message}");
        }
    }
    
    // ตรวจสอบสถานะ Database
    private async void BtnCheckDatabase_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Checking Database status...");
        
        try
        {
            var databaseService = new TennisApp.Services.DatabaseService();
            var dbPath = databaseService.GetDatabasePath();
            var isReady = databaseService.IsDatabaseReady();
            var courts = await databaseService.Courts.GetAllCourtsAsync();
            
            var message = $"Database Path: {dbPath}\n" +
                         $"Database Ready: {isReady}\n" +
                         $"Court Count: {courts.Count}\n\n";
            
            foreach (var court in courts)
            {
                message += $"Court {court.CourtID}: {court.Status} ({court.LastUpdated:dd/MM/yyyy})\n";
            }
            
            System.Diagnostics.Debug.WriteLine($"Database Info:\n{message}");
            await ShowMessage("Database Status", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Check Database failed: {ex.Message}");
            await ShowMessage("Error", $"Check Database failed: {ex.Message}");
        }
    }
#endif
}
