using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Services;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

public sealed partial class PaidBookingPage : Page
{
    public BookingPageViewModel VM { get; } = new();
    private NotificationService? _notify;
    private DateTime _currentDate = DateTime.Today;
    private string _currentStatusFilter = "all";

    public PaidBookingPage()
    {
        this.InitializeComponent();
        DataContext = VM;
        this.Loaded += PaidBookingPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _notify = NotificationService.GetFromPage(this);
    }

    // ========================================================================
    // Load
    // ========================================================================

    private async void PaidBookingPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await VM.LoadReservationsAsync();
            UpdateDateDisplay();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    // ========================================================================
    // Date Navigator
    // ========================================================================

    private void BtnPrevDate_Click(object sender, RoutedEventArgs e)
    {
        _currentDate = _currentDate.AddDays(-1);
        UpdateDateDisplay();
        ApplyFilter();
    }

    private void BtnNextDate_Click(object sender, RoutedEventArgs e)
    {
        _currentDate = _currentDate.AddDays(1);
        UpdateDateDisplay();
        ApplyFilter();
    }

    private void UpdateDateDisplay()
    {
        var thai = new CultureInfo("th-TH");
        var isToday = _currentDate.Date == DateTime.Today;
        var prefix = isToday ? "วันนี้ — " : "";
        DateDisplayText.Text = $"{prefix}{_currentDate:dd/MM/yyyy}";
        DayOfWeekText.Text = _currentDate.ToString("dddd", thai);
    }

    // ========================================================================
    // Status Filter
    // ========================================================================

    private void StatusFilter_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is string tag)
        {
            _currentStatusFilter = tag;
            UpdateFilterChipVisuals();
            ApplyFilter();
        }
    }

    private void UpdateFilterChipVisuals()
    {
        // Active chip = filled, inactive = light bg
        SetChipStyle(FilterAll, "all", "#4A148C", "White");
        SetChipStyle(FilterBooked, "booked", "#1565C0", "#E3F2FD");
        SetChipStyle(FilterInUse, "in_use", "#E65100", "#FFF3E0");
        SetChipStyle(FilterCompleted, "completed", "#2E7D32", "#E8F5E9");
        SetChipStyle(FilterCancelled, "cancelled", "#C62828", "#FFEBEE");
    }

    private void SetChipStyle(Border chip, string tag, string activeColor, string inactiveBg)
    {
        if (_currentStatusFilter == tag)
        {
            chip.Background = new SolidColorBrush(ParseColor(activeColor));
            foreach (var child in (chip.Child as StackPanel)!.Children)
            {
                if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                if (child is Ellipse ell) ell.Fill = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
        }
        else
        {
            chip.Background = new SolidColorBrush(ParseColor(inactiveBg));
            var fgColor = ParseColor(activeColor);
            foreach (var child in (chip.Child as StackPanel)!.Children)
            {
                if (child is TextBlock tb) tb.Foreground = new SolidColorBrush(fgColor);
                if (child is Ellipse ell) ell.Fill = new SolidColorBrush(fgColor);
            }
        }
    }

    private static Windows.UI.Color ParseColor(string hex) => UIHelper.ParseColor(hex);

    // ========================================================================
    // Search
    // ========================================================================

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();

    private void FilterSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    // ========================================================================
    // Filter Logic
    // ========================================================================

    private void ApplyFilter()
    {
        var searchText = FilterSearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

        var filtered = VM.PaidReservations.Where(r =>
        {
            // Date filter
            if (r.ReserveDate.Date != _currentDate.Date) return false;

            // Status filter
            if (_currentStatusFilter != "all" && r.Status != _currentStatusFilter) return false;

            // Search text
            if (!string.IsNullOrEmpty(searchText))
            {
                return (r.ReserveName?.ToLower().Contains(searchText) ?? false)
                    || (r.ReserveId?.ToLower().Contains(searchText) ?? false)
                    || (r.ReservePhone?.ToLower().Contains(searchText) ?? false);
            }

            return true;
        }).OrderBy(r => r.ReserveTime).ToList();

        PaidReservationsListView.ItemsSource = filtered;

        // Update counts (for current date)
        var dateItems = VM.PaidReservations.Where(r => r.ReserveDate.Date == _currentDate.Date).ToList();
        AllCountText.Text = dateItems.Count.ToString();
        BookedCountText.Text = dateItems.Count(r => r.Status == "booked").ToString();
        InUseCountText.Text = dateItems.Count(r => r.Status == "in_use").ToString();
        CompletedCountText.Text = dateItems.Count(r => r.Status == "completed").ToString();
        CancelledCountText.Text = dateItems.Count(r => r.Status == "cancelled").ToString();

        // Empty state
        EmptyStatePanel.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PaidReservationsListView.Visibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        var isToday = _currentDate.Date == DateTime.Today;
        EmptyStateText.Text = isToday
            ? "ไม่มีรายการจองในวันนี้"
            : $"ไม่มีรายการจองวันที่ {_currentDate:dd/MM/yyyy}";
    }

    // ========================================================================
    // Navigation
    // ========================================================================

    private void BtnAddBooking_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PaidBookingFormPage));
    }

    // ========================================================================
    // Pull-to-Refresh
    // ========================================================================

    private async void PaidRefreshContainer_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            await VM.LoadReservationsAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"รีเฟรชล้มเหลว: {ex.Message}");
        }
        finally
        {
            deferral.Complete();
        }
    }

    // ========================================================================
    // Edit
    // ========================================================================

    private void BtnEditPaid_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PaidCourtReservationItem item) return;
        if (item.Status != "booked")
        {
            _notify?.ShowWarning("สามารถแก้ไขได้เฉพาะสถานะ 'จองแล้ว' เท่านั้น");
            return;
        }
        Frame.Navigate(typeof(PaidBookingFormPage), item);
    }

    // ========================================================================
    // Delete
    // ========================================================================

    private async void BtnDeletePaid_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PaidCourtReservationItem item) return;

        // ✅ ป้องกันลบจองที่กำลังใช้งานหรือเสร็จสิ้นแล้ว
        if (item.Status == "in_use")
        {
            _notify?.ShowWarning("ไม่สามารถลบได้ เนื่องจากกำลังใช้งานอยู่\nกรุณาสิ้นสุดการใช้งานก่อน");
            return;
        }
        if (item.Status == "completed")
        {
            _notify?.ShowWarning("ไม่สามารถลบได้ เนื่องจากใช้งานเสร็จสิ้นแล้ว");
            return;
        }

        var confirmed = _notify != null
            ? await _notify.ShowDeleteConfirmAsync($"การจอง {item.ReserveId} ({item.ReserveName})", this.XamlRoot!)
            : await NotificationService.ConfirmAsync("ยืนยันการลบ",
                $"ต้องการลบการจอง {item.ReserveId} ({item.ReserveName}) ใช่หรือไม่?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.DeletePaidReservationAsync(item.ReserveId))
        {
            ApplyFilter();
            _notify?.ShowSuccess("ลบการจองเรียบร้อยแล้ว");
        }
        else
            _notify?.ShowError("ไม่สามารถลบการจองได้");
    }

    // ========================================================================
    // Cancel
    // ========================================================================

    private async void BtnCancelPaid_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PaidCourtReservationItem item) return;
        if (item.Status != "booked") { _notify?.ShowWarning("เฉพาะสถานะ 'จองแล้ว' เท่านั้นที่ยกเลิกได้"); return; }

        var confirmed = _notify != null
            ? await _notify.ShowConfirmAsync("ยืนยันยกเลิก",
                $"ยกเลิกการจอง {item.ReserveId}?\n\nผู้จอง: {item.ReserveName}\nเวลา: {item.TimeRangeDisplay}", this.XamlRoot!)
            : await NotificationService.ConfirmAsync("ยืนยันยกเลิก", $"ยกเลิกการจอง {item.ReserveId}?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.UpdatePaidStatusAsync(item.ReserveId, "cancelled"))
        {
            ApplyFilter();
            _notify?.ShowSuccess("ยกเลิกการจองเรียบร้อยแล้ว");
        }
    }
}
