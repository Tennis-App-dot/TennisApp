using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class BookingPage : Page
{
    public BookingPageViewModel VM { get; } = new();
    private NotificationService? _notify;

    public BookingPage()
    {
        this.InitializeComponent();
        DataContext = VM;
        this.Loaded += BookingPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _notify = NotificationService.GetFromPage(this);
    }

    private async void BookingPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.WhenAll(
                VM.LoadReservationsAsync(),
                VM.LoadAvailableCoursesAsync()
            );

            UpdateSummaryUI();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    // ========================================================================
    // Navigation
    // ========================================================================

    private void BtnAddBooking_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(BookingFormPage));
    }

    // ========================================================================
    // Summary
    // ========================================================================

    private void UpdateSummaryUI()
    {
        VM.UpdateSummary();
        TodayCountText.Text = VM.TodayCount.ToString();
        FutureCountText.Text = VM.FutureCount.ToString();
        CancelledCountText.Text = VM.CancelledCount.ToString();
    }

    // ========================================================================
    // Filter & Search
    // ========================================================================

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var fieldTag = (FilterFieldComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        var searchText = FilterSearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

        AllReservationsListView.ItemsSource = VM.PaidReservations.Where(r =>
            MatchesPaidFilter(r, fieldTag, searchText)).ToList();

        CourseReservationsListView.ItemsSource = VM.CourseReservations.Where(r =>
            MatchesCourseFilter(r, fieldTag, searchText)).ToList();
    }

    private static bool MatchesPaidFilter(PaidCourtReservationItem r, string field, string search)
    {
        if (string.IsNullOrEmpty(search)) return true;

        return field switch
        {
            "Name" => r.ReserveName?.ToLower().Contains(search) ?? false,
            "Id" => r.ReserveId?.ToLower().Contains(search) ?? false,
            _ => (r.ReserveName?.ToLower().Contains(search) ?? false)
              || (r.ReserveId?.ToLower().Contains(search) ?? false)
              || (r.ReservePhone?.ToLower().Contains(search) ?? false),
        };
    }

    private static bool MatchesCourseFilter(CourseCourtReservationItem r, string field, string search)
    {
        if (string.IsNullOrEmpty(search)) return true;

        return field switch
        {
            "Name" => r.ReserveName?.ToLower().Contains(search) ?? false,
            "Id" => r.ReserveId?.ToLower().Contains(search) ?? false,
            _ => (r.ReserveName?.ToLower().Contains(search) ?? false)
              || (r.ReserveId?.ToLower().Contains(search) ?? false)
              || (r.ReservePhone?.ToLower().Contains(search) ?? false)
              || (r.ClassDisplayName?.ToLower().Contains(search) ?? false),
        };
    }

    // ========================================================================
    // Status Actions
    // ========================================================================

    private async void BtnDeletePaid_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PaidCourtReservationItem item) return;

        bool confirmed;
        if (_notify != null)
            confirmed = await _notify.ShowDeleteConfirmAsync(
                $"การจอง {item.ReserveId} ({item.ReserveName})", this.XamlRoot!);
        else
            confirmed = await NotificationService.ConfirmAsync("ยืนยันการลบ",
                $"ต้องการลบการจอง {item.ReserveId} ({item.ReserveName}) ใช่หรือไม่?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.DeletePaidReservationAsync(item.ReserveId))
        {
            UpdateSummaryUI(); ApplyFilter();
            _notify?.ShowSuccess("ลบการจองเรียบร้อยแล้ว");
        }
        else
            _notify?.ShowError("ไม่สามารถลบการจองได้");
    }

    private async void BtnCancelPaid_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PaidCourtReservationItem item) return;

        if (item.Status != "booked") { _notify?.ShowWarning("สถานะปัจจุบันไม่ใช่ 'จองแล้ว'"); return; }

        bool confirmed;
        if (_notify != null)
            confirmed = await _notify.ShowConfirmAsync("ยืนยันยกเลิก", $"ยกเลิกการจอง {item.ReserveId}?", this.XamlRoot!);
        else
            confirmed = await NotificationService.ConfirmAsync("ยืนยันยกเลิก", $"ยกเลิกการจอง {item.ReserveId}?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.UpdatePaidStatusAsync(item.ReserveId, "cancelled"))
        {
            UpdateSummaryUI(); ApplyFilter();
            _notify?.ShowSuccess("ยกเลิกการจองเรียบร้อยแล้ว");
        }
    }

    private async void BtnDeleteCourse_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourseCourtReservationItem item) return;

        bool confirmed;
        if (_notify != null)
            confirmed = await _notify.ShowDeleteConfirmAsync(
                $"การจอง {item.ReserveId} ({item.ReserveName})", this.XamlRoot!);
        else
            confirmed = await NotificationService.ConfirmAsync("ยืนยันการลบ",
                $"ต้องการลบการจอง {item.ReserveId} ({item.ReserveName}) ใช่หรือไม่?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.DeleteCourseReservationAsync(item.ReserveId))
        {
            UpdateSummaryUI(); ApplyFilter();
            _notify?.ShowSuccess("ลบการจองเรียบร้อยแล้ว");
        }
        else
            _notify?.ShowError("ไม่สามารถลบการจองได้");
    }

    private async void BtnCancelCourse_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not CourseCourtReservationItem item) return;

        if (item.Status != "booked") { _notify?.ShowWarning("สถานะปัจจุบันไม่ใช่ 'จองแล้ว'"); return; }

        bool confirmed;
        if (_notify != null)
            confirmed = await _notify.ShowConfirmAsync("ยืนยันยกเลิก", $"ยกเลิกการจอง {item.ReserveId}?", this.XamlRoot!);
        else
            confirmed = await NotificationService.ConfirmAsync("ยืนยันยกเลิก", $"ยกเลิกการจอง {item.ReserveId}?", this.XamlRoot!);

        if (!confirmed) return;

        if (await VM.UpdateCourseStatusAsync(item.ReserveId, "cancelled"))
        {
            UpdateSummaryUI(); ApplyFilter();
            _notify?.ShowSuccess("ยกเลิกการจองเรียบร้อยแล้ว");
        }
    }
}
