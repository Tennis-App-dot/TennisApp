using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Helpers;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class PaidBookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();
    private NotificationService? _notify;

    public PaidBookingFormPage()
    {
        this.InitializeComponent();
        this.Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
    }

    // ========================================================================
    // Navigation
    // ========================================================================

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    // ========================================================================
    // Live End Time
    // ========================================================================

    private void TimeOrDuration_Changed(object sender, SelectionChangedEventArgs e)
    {
        var timeTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var durationTag = (FormDurationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        if (!string.IsNullOrEmpty(timeTag) && !string.IsNullOrEmpty(durationTag)
            && TimeSpan.TryParse(timeTag, out var start)
            && double.TryParse(durationTag, System.Globalization.CultureInfo.InvariantCulture, out var dur))
        {
            var end = start.Add(TimeSpan.FromHours(dur));
            EndTimeText.Text = $"{start:hh\\:mm} → {end:hh\\:mm}";
            EndTimePanel.Visibility = Visibility.Visible;
        }
        else
        {
            EndTimePanel.Visibility = Visibility.Collapsed;
        }
    }

    // ========================================================================
    // Submit
    // ========================================================================

    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Validate(out var msg)) { _notify?.ShowWarning(msg); return; }

            var reserveDate = FormDatePicker.Date.DateTime;
            TimeSpan.TryParse((FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var reserveTime);
            double.TryParse((FormDurationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                System.Globalization.CultureInfo.InvariantCulture, out var duration);
            var name = FormNameTextBox.Text.Trim();
            var phone = FormPhoneTextBox.Text.Trim();
            var endTime = reserveTime.Add(TimeSpan.FromHours(duration));

            var confirmMsg = $"📅 {reserveDate:dd/MM/yyyy}\n"
                           + $"⏰ {reserveTime:hh\\:mm} - {endTime:hh\\:mm} ({duration} ชม.)\n"
                           + $"👤 {name}"
                           + (string.IsNullOrEmpty(phone) ? "" : $"\n📞 {phone}");

            bool confirmed = _notify != null
                ? await _notify.ShowConfirmAsync("ยืนยันจองเช่าสนาม", confirmMsg, this.XamlRoot!)
                : await NotificationService.ConfirmAsync("ยืนยันจองเช่าสนาม", confirmMsg, this.XamlRoot!);
            if (!confirmed) return;

            var duplicateMsg = await _vm.GetDuplicateReservationMessageAsync(name, reserveDate, reserveTime, duration);
            if (duplicateMsg != null)
            {
                _notify?.ShowWarning(duplicateMsg);
                return;
            }

            var reservation = new PaidCourtReservationItem
            {
                ReserveId = ReservationIdGenerator.GeneratePaidReservationId(DateTime.Now),
                CourtId = "00",
                RequestDate = DateTime.Now,
                ReserveDate = reserveDate,
                ReserveTime = reserveTime,
                Duration = duration,
                ReserveName = name,
                ReservePhone = phone,
                Status = "booked"
            };

            if (await _vm.AddPaidReservationAsync(reservation))
            {
                _notify?.ShowSuccess("บันทึกการจองเรียบร้อย");
                if (Frame.CanGoBack) Frame.GoBack();
            }
            else
            {
                _notify?.ShowError("ไม่สามารถบันทึกการจองได้");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError(ex.Message);
        }
    }

    // ========================================================================
    // Validation
    // ========================================================================

    private bool Validate(out string message)
    {
        message = string.Empty;
        if (FormTimeComboBox.SelectedIndex < 0) { message = "กรุณาเลือกเวลาที่ต้องการใช้สนาม"; return false; }
        if (FormDurationComboBox.SelectedIndex < 0) { message = "กรุณาเลือกระยะเวลา"; return false; }
        if (string.IsNullOrWhiteSpace(FormNameTextBox.Text)) { message = "กรุณากรอกชื่อผู้จอง"; return false; }

        if (!string.IsNullOrWhiteSpace(FormPhoneTextBox.Text))
        {
            var phone = FormPhoneTextBox.Text.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
            { message = "เบอร์โทรศัพท์ต้องเป็นตัวเลข 10 หลัก"; return false; }
        }
        return true;
    }
}
