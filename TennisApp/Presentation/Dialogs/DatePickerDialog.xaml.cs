using System;
using Microsoft.UI.Xaml.Controls;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class DatePickerDialog : ContentDialog
{
    public DateTimeOffset? SelectedDate { get; private set; }

    public DatePickerDialog(DateTimeOffset? initialDate = null, bool allowPastDates = false)
    {
        InitializeComponent();

        // ล็อควันก่อนวันปัจจุบัน (ห้ามเลือกวันในอดีต)
        if (!allowPastDates)
        {
            Calendar.MinDate = new DateTimeOffset(DateTime.Today);
        }

        if (initialDate.HasValue)
        {
            // ถ้า initialDate อยู่ในอดีตและไม่อนุญาต ให้ข้ามการ select
            if (allowPastDates || initialDate.Value.Date >= DateTime.Today)
            {
                Calendar.SelectedDates.Add(initialDate.Value);
            }
            Calendar.SetDisplayDate(initialDate.Value);
        }

        this.PrimaryButtonClick += (s, e) =>
        {
            if (Calendar.SelectedDates.Count > 0)
                SelectedDate = Calendar.SelectedDates[0];
            else
                e.Cancel = true;
        };
    }

    private void Calendar_DayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
    {
        // สามารถ customize วันที่แสดงได้ในอนาคต
    }

    /// <summary>
    /// เปิด Dialog เลือกวันที่ตรงกลางจอ
    /// คืนค่า DateTime? (null = ยกเลิก)
    /// </summary>
    public static async System.Threading.Tasks.Task<DateTime?> ShowAsync(
        Microsoft.UI.Xaml.XamlRoot xamlRoot, DateTime? currentDate = null, bool allowPastDates = false)
    {
        var initialOffset = currentDate.HasValue
            ? new DateTimeOffset(currentDate.Value)
            : (DateTimeOffset?)null;

        var dialog = new DatePickerDialog(initialOffset, allowPastDates)
        {
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.SelectedDate.HasValue)
            return dialog.SelectedDate.Value.DateTime;

        return null;
    }
}
