using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Presentation.Dialogs;
using TennisApp.Helpers;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class PaidBookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();
    private NotificationService? _notify;
    private DateTime? _selectedDate;
    private PaidCourtReservationItem? _editingReservation;
    private bool _isEditMode;
    private bool _editDataLoaded;

    public PaidBookingFormPage()
    {
        this.InitializeComponent();
        this.Loaded += Page_Loaded;
        this.Unloaded += Page_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _editDataLoaded = false;
        _isEditMode = false;
        _editingReservation = null;
        if (e.Parameter is PaidCourtReservationItem reservation)
        {
            _editingReservation = reservation;
            _isEditMode = true;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        InputScrollHelper.Attach(this);

        if (_isEditMode && _editingReservation != null && !_editDataLoaded)
        {
            _editDataLoaded = true;
            LoadEditData(_editingReservation);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        InputScrollHelper.Detach(this);
    }

    // ========================================================================
    // Edit Mode — Pre-fill form
    // ========================================================================

    private void LoadEditData(PaidCourtReservationItem r)
    {
        // Header
        HeaderText.Text = "แก้ไขการจอง";
        SubmitButtonText.Text = "บันทึกการแก้ไข";

        // Date
        _selectedDate = r.ReserveDate;
        TxtDateDisplay.Text = r.ReserveDate.ToString("dd/MM/yyyy");
        TxtDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);

        // Start Time
        var startTag = r.ReserveTime.ToString(@"hh\:mm");
        foreach (ComboBoxItem item in FormTimeComboBox.Items)
        {
            if (item.Tag?.ToString() == startTag)
            {
                FormTimeComboBox.SelectedItem = item;
                break;
            }
        }

        // End Time (populate first, then select)
        PopulateEndTimeComboBox();
        var endTime = r.ReserveTime.Add(TimeSpan.FromHours(r.Duration));
        var endTag = endTime.ToString(@"hh\:mm");
        foreach (ComboBoxItem item in FormEndTimeComboBox.Items)
        {
            if (item.Tag?.ToString() == endTag)
            {
                FormEndTimeComboBox.SelectedItem = item;
                break;
            }
        }
        UpdateEndTimePreview();

        // Customer info
        FormNameTextBox.Text = r.ReserveName;
        FormPhoneTextBox.Text = r.ReservePhone;
    }

    // ========================================================================
    // Navigation
    // ========================================================================

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    // ========================================================================
    // Date Picker Dialog
    // ========================================================================

    private async void BtnPickDate_Click(object sender, RoutedEventArgs e)
    {
        var result = await DatePickerDialog.ShowAsync(this.XamlRoot!, _selectedDate);
        if (result.HasValue)
        {
            _selectedDate = result.Value;
            TxtDateDisplay.Text = result.Value.ToString("dd/MM/yyyy");
            TxtDateDisplay.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);
        }
    }

    // ========================================================================
    // Live End Time — dynamic end time ComboBox
    // ========================================================================

    private void StartTime_Changed(object sender, SelectionChangedEventArgs e)
    {
        PopulateEndTimeComboBox();
        UpdateEndTimePreview();
    }

    private void EndTime_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateEndTimePreview();
    }

    private void PopulateEndTimeComboBox()
    {
        FormEndTimeComboBox.Items.Clear();
        FormEndTimeComboBox.SelectedIndex = -1;

        var startTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (string.IsNullOrEmpty(startTag) || !TimeSpan.TryParse(startTag, out var start))
            return;

        // สร้างตัวเลือกเวลาสิ้นสุด: ทุก 30 นาที ตั้งแต่ start+0:30 ถึง 21:00
        var maxEnd = TimeSpan.FromHours(21);
        var current = start.Add(TimeSpan.FromMinutes(30));
        while (current <= maxEnd)
        {
            var dur = (current - start).TotalHours;
            var item = new ComboBoxItem
            {
                Content = $"{current:hh\\:mm} ({dur:0.0} ชม.)",
                Tag = current.ToString(@"hh\:mm")
            };
            FormEndTimeComboBox.Items.Add(item);
            current = current.Add(TimeSpan.FromMinutes(30));
        }
    }

    private void UpdateEndTimePreview()
    {
        var startTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var endTag = (FormEndTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        if (!string.IsNullOrEmpty(startTag) && !string.IsNullOrEmpty(endTag)
            && TimeSpan.TryParse(startTag, out var start) && TimeSpan.TryParse(endTag, out var end)
            && end > start)
        {
            var dur = (end - start).TotalHours;
            EndTimePreviewText.Text = $"{start:hh\\:mm} → {end:hh\\:mm} ({dur:0.0} ชม.)";
            EndTimePanel.Visibility = Visibility.Visible;
        }
        else
        {
            EndTimePanel.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// คำนวณ duration จากเวลาเริ่ม-สิ้นสุด
    /// </summary>
    private double CalculateDuration()
    {
        var startTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var endTag = (FormEndTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (!string.IsNullOrEmpty(startTag) && !string.IsNullOrEmpty(endTag)
            && TimeSpan.TryParse(startTag, out var start) && TimeSpan.TryParse(endTag, out var end)
            && end > start)
        {
            return (end - start).TotalHours;
        }
        return 0;
    }

    // ========================================================================
    // Submit
    // ========================================================================

    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Validate(out var msg)) { _notify?.ShowWarning(msg); return; }

            var reserveDate = _selectedDate!.Value;
            TimeSpan.TryParse((FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var reserveTime);
            var duration = CalculateDuration();
            var name = FormNameTextBox.Text.Trim();
            var phone = FormPhoneTextBox.Text.Trim();
            var endTime = reserveTime.Add(TimeSpan.FromHours(duration));

            // ข้อ 3: ตรวจสอบสนามว่าง
            var slotMsg = await CheckSlotAvailabilityAsync(reserveDate, reserveTime, duration);
            if (slotMsg != null)
            {
                if (_notify != null)
                    await _notify.ShowCriticalErrorAsync("🚫 สนามเต็ม", slotMsg, this.XamlRoot!);
                else
                    _notify?.ShowWarning(slotMsg);
                return;
            }

            // ตรวจสอบจองซ้ำก่อนยืนยัน
            var excludeId = _isEditMode ? _editingReservation!.ReserveId : null;
            var duplicateMsg = await _vm.GetDuplicateReservationMessageAsync(name, reserveDate, reserveTime, duration, excludeId);
            if (duplicateMsg != null)
            {
                if (_notify != null)
                    await _notify.ShowCriticalErrorAsync("⚠️ พบการจองซ้ำ", duplicateMsg, this.XamlRoot!);
                else
                    _notify?.ShowWarning(duplicateMsg);
                return;
            }

            var confirmMsg = $"📅 {reserveDate:dd/MM/yyyy}\n"
                           + $"⏰ {reserveTime:hh\\:mm} - {endTime:hh\\:mm} ({duration:0.0} ชม.)\n"
                           + $"👤 {name}"
                           + (string.IsNullOrEmpty(phone) ? "" : $"\n📞 {phone}");

            var confirmTitle = _isEditMode ? "ยืนยันแก้ไขการจอง" : "ยืนยันจองเช่าสนาม";
            bool confirmed = _notify != null
                ? await _notify.ShowConfirmAsync(confirmTitle, confirmMsg, this.XamlRoot!)
                : await NotificationService.ConfirmAsync(confirmTitle, confirmMsg, this.XamlRoot!);
            if (!confirmed) return;

            bool success;
            if (_isEditMode)
            {
                var updated = _editingReservation!.Clone();
                updated.ReserveDate = reserveDate;
                updated.ReserveTime = reserveTime;
                updated.Duration = duration;
                updated.ReserveName = name;
                updated.ReservePhone = phone;
                success = await _vm.UpdatePaidReservationAsync(updated);
            }
            else
            {
                var dbService = ((App)Application.Current).DatabaseService;
                var reserveId = await ReservationIdGenerator.GeneratePaidReservationIdAsync(dbService, DateTime.Now);
                var reservation = new PaidCourtReservationItem
                {
                    ReserveId = reserveId,
                    CourtId = "00",
                    RequestDate = DateTime.Now,
                    ReserveDate = reserveDate,
                    ReserveTime = reserveTime,
                    Duration = duration,
                    ReserveName = name,
                    ReservePhone = phone,
                    Status = "booked"
                };
                success = await _vm.AddPaidReservationAsync(reservation);
            }

            if (success)
            {
                _notify?.ShowSuccess(_isEditMode ? "แก้ไขการจองเรียบร้อย" : "บันทึกการจองเรียบร้อย");
                if (Frame.CanGoBack) Frame.GoBack();
            }
            else
            {
                _notify?.ShowError(_isEditMode ? "ไม่สามารถแก้ไขการจองได้" : "ไม่สามารถบันทึกการจองได้");
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError(ex.Message);
        }
    }

    // ========================================================================
    // ข้อ 3: ตรวจสอบจำนวนสนามว่าง
    // ========================================================================

    private async Task<string?> CheckSlotAvailabilityAsync(DateTime reserveDate, TimeSpan reserveTime, double duration)
    {
        try
        {
            await _vm.LoadAvailableCourtsAsync();
            var totalCourts = _vm.AvailableCourts.Count;
            if (totalCourts == 0) return "ไม่มีสนามพร้อมใช้งานในระบบ";

            // ✅ ถ้าเป็น edit mode ให้ exclude ตัวเองออกจากการนับ unassigned
            var excludeId = _isEditMode ? _editingReservation?.ReserveId : null;
            var availableCourts = await _vm.GetAvailableCourtsForTimeSlotAsync(reserveDate, reserveTime, duration, excludeId);
            if (availableCourts.Count == 0)
            {
                var endTime = reserveTime.Add(TimeSpan.FromHours(duration));
                return $"ไม่มีสนามว่างในช่วงเวลานี้\n\n" +
                       $"📅 {reserveDate:dd/MM/yyyy}\n" +
                       $"⏰ {reserveTime:hh\\:mm} - {endTime:hh\\:mm}\n\n" +
                       $"สนามทั้งหมด {totalCourts} สนาม ถูกจองเต็มแล้ว\nกรุณาเลือกช่วงเวลาอื่น";
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CheckSlotAvailability: {ex.Message}");
            return null;
        }
    }

    // ========================================================================
    // Validation
    // ========================================================================

    private bool Validate(out string message)
    {
        message = string.Empty;
        if (!_selectedDate.HasValue) { message = "กรุณาเลือกวันที่ต้องการใช้สนาม"; return false; }
        if (_selectedDate.Value.Date < DateTime.Today) { message = "ไม่สามารถจองวันที่ผ่านมาแล้วได้"; return false; }
        if (FormTimeComboBox.SelectedIndex < 0) { message = "กรุณาเลือกเวลาที่ต้องการใช้สนาม"; return false; }
        if (FormEndTimeComboBox.SelectedIndex < 0) { message = "กรุณาเลือกเวลาสิ้นสุด"; return false; }
        if (string.IsNullOrWhiteSpace(FormNameTextBox.Text)) { message = "กรุณากรอกชื่อผู้จอง"; return false; }

        // Validate end time > start time
        var startTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var endTag = (FormEndTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (!string.IsNullOrEmpty(startTag) && !string.IsNullOrEmpty(endTag)
            && TimeSpan.TryParse(startTag, out var start) && TimeSpan.TryParse(endTag, out var end))
        {
            if (end <= start) { message = "เวลาสิ้นสุดต้องมากกว่าเวลาเริ่ม"; return false; }
            if (end > TimeSpan.FromHours(21)) { message = "เวลาสิ้นสุดต้องไม่เกิน 21:00"; return false; }
        }

        if (!string.IsNullOrWhiteSpace(FormPhoneTextBox.Text))
        {
            var phone = FormPhoneTextBox.Text.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
            { message = "เบอร์โทรศัพท์ต้องเป็นตัวเลข 10 หลัก"; return false; }
        }
        return true;
    }
}
