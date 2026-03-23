using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Helpers;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class BookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();
    private NotificationService? _notify;
    private string _defaultType = "Paid";

    public BookingFormPage()
    {
        this.InitializeComponent();
        this.Loaded += BookingFormPage_Loaded;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string type && (type == "Paid" || type == "Course"))
        {
            _defaultType = type;
        }
    }

    private async void BookingFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        try
        {
            await _vm.LoadAvailableCoursesAsync();
            PopulateCourses();

            // Auto-select type based on navigation parameter
            foreach (ComboBoxItem item in FormTypeComboBox.Items)
            {
                if (item.Tag?.ToString() == _defaultType)
                {
                    FormTypeComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    private void PopulateCourses()
    {
        FormCourseComboBox.Items.Clear();
        FormCourseComboBox.Items.Add(new ComboBoxItem { Content = "เลือกคอร์ส", IsSelected = true });
        foreach (var course in _vm.AvailableCourses)
            FormCourseComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"{course.ClassTitle} ({course.TrainerDisplayName})",
                Tag = course.CompositeKey
            });
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void FormTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FormTypeComboBox.SelectedItem is ComboBoxItem item)
        {
            var isCourse = item.Tag?.ToString() == "Course";
            FormCourseComboBox.IsEnabled = isCourse;
            FormCourseComboBox.Opacity = isCourse ? 1.0 : 0.4;
            if (!isCourse) FormCourseComboBox.SelectedIndex = 0;
        }
    }

    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ValidateForm(out var message))
            {
                _notify?.ShowWarning(message);
                return;
            }

            var reserveDate = FormDatePicker.Date.DateTime;
            var timeTag = (FormTimeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var durationTag = (FormDurationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var typeTag = (FormTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var name = FormNameTextBox.Text.Trim();
            var phone = FormPhoneTextBox.Text.Trim();

            TimeSpan.TryParse(timeTag, out var reserveTime);
            double.TryParse(durationTag, out var duration);

            var endTime = reserveTime.Add(TimeSpan.FromHours(duration));
            var confirmMsg = $"วันที่: {reserveDate:dd/MM/yyyy}\n"
                           + $"เวลา: {reserveTime:hh\\:mm} - {endTime:hh\\:mm}\n"
                           + $"ระยะเวลา: {duration} ชม.\n"
                           + $"จุดประสงค์: {(typeTag == "Paid" ? "เช่าสนาม" : "คอร์สเรียน")}\n"
                           + $"ผู้จอง: {name}";

            if (!string.IsNullOrEmpty(phone))
                confirmMsg += $"\nเบอร์โทร: {phone}";

            bool confirmed;
            if (_notify != null)
                confirmed = await _notify.ShowConfirmAsync("ยืนยันการจอง", confirmMsg, this.XamlRoot!);
            else
                confirmed = await NotificationService.ConfirmAsync("ยืนยันการจอง", confirmMsg, this.XamlRoot!);

            if (!confirmed) return;

            // ตรวจสอบการจองซ้ำ (ชื่อเดียวกัน + วันเดียวกัน + เวลาซ้อนทับ) — cross Paid+Course
            var duplicateMsg = await _vm.GetDuplicateReservationMessageAsync(name, reserveDate, reserveTime, duration);
            if (duplicateMsg != null)
            {
                _notify?.ShowWarning(duplicateMsg);
                return;
            }

            bool success;
            if (typeTag == "Paid")
            {
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
                success = await _vm.AddPaidReservationAsync(reservation);
            }
            else
            {
                var compositeKey = (FormCourseComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var courseKey = CourseKey.Parse(compositeKey ?? string.Empty);
                var reservation = new CourseCourtReservationItem
                {
                    ReserveId = ReservationIdGenerator.GenerateCourseReservationId(DateTime.Now),
                    CourtId = "00",
                    ClassId = courseKey?.ClassId ?? string.Empty,
                    TrainerId = courseKey?.TrainerId ?? string.Empty,
                    RequestDate = DateTime.Now,
                    ReserveDate = reserveDate,
                    ReserveTime = reserveTime,
                    Duration = duration,
                    ReserveName = name,
                    ReservePhone = phone,
                    Status = "booked"
                };
                success = await _vm.AddCourseReservationAsync(reservation);
            }

            if (success)
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

    private bool ValidateForm(out string message)
    {
        message = string.Empty;

        if (FormTimeComboBox.SelectedIndex < 0) { message = "กรุณาเลือกเวลาที่ต้องการใช้สนาม"; return false; }
        if (FormDurationComboBox.SelectedIndex < 0) { message = "กรุณาเลือกระยะเวลาที่ต้องการใช้สนาม"; return false; }
        if (string.IsNullOrWhiteSpace(FormNameTextBox.Text)) { message = "กรุณากรอกชื่อผู้จอง"; return false; }

        var typeTag = (FormTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (typeTag == "Course" && FormCourseComboBox.SelectedIndex <= 0)
        { message = "กรุณาเลือกคอร์สเรียน"; return false; }

        if (!string.IsNullOrWhiteSpace(FormPhoneTextBox.Text))
        {
            var phone = FormPhoneTextBox.Text.Trim();
            if (phone.Length != 10 || !phone.All(char.IsDigit))
            { message = "เบอร์โทรศัพท์ต้องเป็นตัวเลข 10 หลัก"; return false; }
        }

        return true;
    }
}
