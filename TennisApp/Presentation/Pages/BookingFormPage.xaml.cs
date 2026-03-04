using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

public sealed partial class BookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();

    public BookingFormPage()
    {
        this.InitializeComponent();
        this.Loaded += BookingFormPage_Loaded;
    }

    private async void BookingFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _vm.LoadAvailableCoursesAsync();
            PopulateCourses();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ BookingFormPage_Loaded error: {ex.Message}");
        }
    }

    private void PopulateCourses()
    {
        FormCourseComboBox.Items.Clear();
        FormCourseComboBox.Items.Add(new ComboBoxItem { Content = "เลือกคอร์ส", IsSelected = true });
        foreach (var course in _vm.AvailableCourses)
            FormCourseComboBox.Items.Add(new ComboBoxItem { Content = course.ClassTitle, Tag = course.ClassId });
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
                await ShowMessage("ข้อมูลไม่ครบถ้วน", message);
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

            if (!await ShowConfirm("ยืนยันการจอง", confirmMsg)) return;

            // ตรวจสอบการจองซ้ำ (ชื่อเดียวกัน + วันเดียวกัน + เวลาซ้อนทับ)
            var isDuplicate = await _vm.HasDuplicateReservationAsync(name, reserveDate, reserveTime, duration);
            if (isDuplicate)
            {
                await ShowMessage("จองซ้ำ", $"พบการจองของ \"{name}\" ในวันที่ {reserveDate:dd/MM/yyyy} ช่วงเวลา {reserveTime:hh\\:mm} - {endTime:hh\\:mm} อยู่แล้ว\nกรุณาเลือกวันหรือเวลาอื่น");
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
                var courseId = (FormCourseComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var reservation = new CourseCourtReservationItem
                {
                    ReserveId = ReservationIdGenerator.GenerateCourseReservationId(DateTime.Now),
                    CourtId = "00",
                    ClassId = courseId ?? string.Empty,
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
                await ShowMessage("สำเร็จ", "บันทึกการจองเรียบร้อย");
                if (Frame.CanGoBack) Frame.GoBack();
            }
            else
            {
                await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถบันทึกการจองได้");
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("เกิดข้อผิดพลาด", ex.Message);
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

    private async Task ShowMessage(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "ตกลง",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirm(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "ยืนยัน",
            SecondaryButtonText = "ยกเลิก",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
