using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using TennisApp.Models;
using TennisApp.Presentation.Dialogs;
using TennisApp.Presentation.ViewModels;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourtPage : Page
{
    public CourtPageViewModel VM { get; } = new();
    private NotificationService? _notify;
    private string _currentFilter = "all";

    public CourtPage()
    {
        InitializeComponent();
        DataContext = VM;
        this.Loaded += CourtPage_Loaded;
    }

    private async void CourtPage_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);

#if DEBUG
        if (FindName("DebugPanel") is StackPanel debugPanel)
            debugPanel.Visibility = Visibility.Visible;
#else
        if (FindName("DebugPanel") is StackPanel debugPanel)
            debugPanel.Visibility = Visibility.Collapsed;
#endif

        UpdateFilterVisuals();

        try
        {
            await VM.LoadCourtsAsync();
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถโหลดข้อมูลสนามได้: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Filter (Border + Button approach — สีขึ้นชัวร์ 100%)
    // ═══════════════════════════════════════════════════════════

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _currentFilter = btn.Tag?.ToString() ?? "all";
        UpdateFilterVisuals();
        _ = VM.ApplyFilterAsync(_currentFilter);
    }

    private void UpdateFilterVisuals()
    {
        // สี active: ทั้งหมด=ม่วง, พร้อมใช้งาน=เขียว, ปิดปรับปรุง=แดง
        var filters = new[]
        {
            (BorderName: "FilterAllBorder",         ButtonName: "FilterAll",         Tag: "all",         R: (byte)108, G: (byte)43,  B: (byte)217), // ม่วง
            (BorderName: "FilterActiveBorder",      ButtonName: "FilterActive",      Tag: "active",      R: (byte)22,  G: (byte)163, B: (byte)74),  // เขียว
            (BorderName: "FilterMaintenanceBorder", ButtonName: "FilterMaintenance", Tag: "maintenance", R: (byte)220, G: (byte)38,  B: (byte)38),  // แดง
        };

        foreach (var (borderName, buttonName, tag, r, g, b) in filters)
        {
            var border = FindName(borderName) as Border;
            var button = FindName(buttonName) as Button;
            if (border == null || button == null) continue;

            if (_currentFilter == tag)
            {
                border.Background = new SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255, r, g, b));
                button.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
            else
            {
                border.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                button.Foreground = new SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255, 107, 114, 128)); // #6B7280
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CRUD
    // ═══════════════════════════════════════════════════════════

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await VM.GetNextCourtIdAsync();
            if (!result.success)
            {
                _notify?.ShowWarning("มีสนามครบ 99 สนามแล้ว ไม่สามารถเพิ่มได้");
                return;
            }

            var seed = new CourtItem { Status = "1", LastUpdated = DateTime.Today };
            var dlg = new CourtFormDialog(seed) { XamlRoot = this.XamlRoot };

            await dlg.ShowAsync();

            if (dlg.WasSaved)
            {
                var item = dlg.Result;
                var success = await VM.AddCourtAsync(item);

                if (!success)
                {
                    _notify?.ShowError("ไม่สามารถบันทึกข้อมูลสนามได้");
                    return;
                }

                await RefreshCurrentFilterAsync();
                _notify?.ShowSuccess("เพิ่มสนามใหม่เรียบร้อยแล้ว");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถเพิ่มสนามได้: {ex.Message}");
        }
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourtItem target) return;

        try
        {
            var copy = target.Clone();
            var dlg = new CourtFormDialog(copy) { XamlRoot = this.XamlRoot };

            await dlg.ShowAsync();

            if (dlg.WasSaved)
            {
                var edited = dlg.Result;
                var success = await VM.UpdateCourtAsync(edited);

                if (!success)
                {
                    _notify?.ShowError("ไม่สามารถบันทึกการแก้ไขได้");
                    return;
                }

                await RefreshCurrentFilterAsync();
                _notify?.ShowSuccess("แก้ไขข้อมูลสนามเรียบร้อยแล้ว");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถแก้ไขสนามได้: {ex.Message}");
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourtItem target) return;

        try
        {
            bool confirmed = false;
            if (_notify != null)
            {
                confirmed = await _notify.ShowDeleteConfirmAsync(target.DisplayName, this.XamlRoot!);
            }
            else
            {
                confirmed = await NotificationService.ConfirmAsync(
                    "ยืนยันการลบ",
                    $"ต้องการลบสนาม {target.DisplayName} ใช่หรือไม่?",
                    this.XamlRoot!);
            }

            if (confirmed)
            {
                var success = await VM.RemoveCourtAsync(target);

                if (!success)
                {
                    _notify?.ShowError("ไม่สามารถลบสนามได้");
                    return;
                }

                await RefreshCurrentFilterAsync();
                _notify?.ShowSuccess($"ลบ{target.DisplayName}เรียบร้อยแล้ว");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถลบสนามได้: {ex.Message}");
        }
    }

    private async Task RefreshCurrentFilterAsync()
    {
        await VM.ApplyFilterAsync(_currentFilter);
    }

#if DEBUG
    private async void BtnDebugTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var databaseService = ((App)Application.Current).DatabaseService;
            databaseService.EnsureInitialized();
            var courts = await databaseService.Courts.GetAllCourtsAsync();
            await VM.LoadCourtsAsync();
            _notify?.ShowInfo($"Database OK — พบ {courts.Count} สนาม");
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"Test failed: {ex.Message}");
        }
    }

    private async void BtnResetDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool confirmed = false;
            if (_notify != null)
            {
                confirmed = await _notify.ShowConfirmAsync(
                    "ยืนยันการรีเซ็ต",
                    "คุณต้องการลบฐานข้อมูลทั้งหมดแล้วสร้างใหม่เปล่าๆ ใช่หรือไม่?\n\n⚠️ ข้อมูลทั้งหมดจะหายไป",
                    this.XamlRoot!);
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "ยืนยันการรีเซ็ต",
                    Content = "คุณต้องการลบฐานข้อมูลทั้งหมดแล้วสร้างใหม่เปล่าๆ ใช่หรือไม่?\n\n⚠️ ข้อมูลทั้งหมดจะหายไป",
                    PrimaryButtonText = "ใช่",
                    CloseButtonText = "ไม่",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                confirmed = await dialog.ShowAsync() == ContentDialogResult.Primary;
            }

            if (!confirmed) return;

            var databaseService = ((App)Application.Current).DatabaseService;
            await databaseService.ResetDatabaseAsync();
            await VM.LoadCourtsAsync();
            _notify?.ShowSuccess("รีเซ็ตฐานข้อมูลเรียบร้อยแล้ว (เริ่มจากฐานข้อมูลเปล่า)");
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถรีเซ็ตฐานข้อมูลได้: {ex.Message}");
        }
    }

    private async void BtnCheckDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var databaseService = ((App)Application.Current).DatabaseService;
            databaseService.EnsureInitialized();
            var tables = await databaseService.GetAllTableNamesAsync();
            var rowCounts = await databaseService.GetTableRowCountsAsync();

            var message = $"Tables: {tables.Count}\n";
            foreach (var table in tables)
            {
                var count = rowCounts.ContainsKey(table) ? rowCounts[table] : 0;
                message += $"• {table}: {count} rows\n";
            }

            _notify?.ShowInfo(message, "Database Status");
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถตรวจสอบฐานข้อมูลได้: {ex.Message}");
        }
    }

    private async void BtnClearData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool confirmed = false;
            if (_notify != null)
            {
                confirmed = await _notify.ShowConfirmAsync(
                    "ล้างข้อมูลทั้งหมด",
                    "ต้องการลบข้อมูลทั้งหมดในฐานข้อมูลใช่หรือไม่?\n\n⚠️ ข้อมูลทั้งหมดจะหายไป",
                    this.XamlRoot!);
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "ล้างข้อมูลทั้งหมด",
                    Content = "ต้องการลบข้อมูลทั้งหมดในฐานข้อมูลใช่หรือไม่?\n\n⚠️ ข้อมูลทั้งหมดจะหายไป",
                    PrimaryButtonText = "ใช่",
                    CloseButtonText = "ไม่",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                confirmed = await dialog.ShowAsync() == ContentDialogResult.Primary;
            }

            if (!confirmed) return;

            var databaseService = ((App)Application.Current).DatabaseService;
            await databaseService.ClearAllDataAsync();
            await VM.LoadCourtsAsync();
            _notify?.ShowSuccess("ล้างข้อมูลทั้งหมดเรียบร้อยแล้ว");
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"ไม่สามารถล้างข้อมูลได้: {ex.Message}");
        }
    }
#endif
}
