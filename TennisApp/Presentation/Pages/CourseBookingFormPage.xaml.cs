using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Helpers;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseBookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();
    private NotificationService? _notify;
    private List<CourseItem> _allCourses = new();
    private CourseItem? _selectedCourse;

    public CourseBookingFormPage()
    {
        this.InitializeComponent();
        this.Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        try
        {
            await _vm.LoadAvailableCoursesAsync();
            _allCourses = _vm.AvailableCourses.ToList();
            RenderCourseList(_allCourses);
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    // ========================================================================
    // Course List — Rich Card Items
    // ========================================================================

    private void RenderCourseList(List<CourseItem> courses)
    {
        CourseListView.Items.Clear();
        foreach (var course in courses)
        {
            CourseListView.Items.Add(CreateCourseCard(course));
        }
    }

    private Border CreateCourseCard(CourseItem course)
    {
        bool isSelected = _selectedCourse != null && _selectedCourse.CompositeKey == course.CompositeKey;

        var card = new Border
        {
            Background = isSelected
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 243, 229, 245))
                : new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(isSelected ? 2 : 1),
            BorderBrush = isSelected
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 74, 20, 140))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 232, 232, 232)),
            Padding = new Thickness(12, 10, 12, 10),
            Tag = course.CompositeKey
        };

        // Row 1: ClassId + ClassTitle + Price
        // Row 2: Sessions + Trainer
        var stack = new StackPanel { Spacing = 3 };

        // Row 1
        var row1 = new Grid();
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var idBadge = new Border
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 74, 20, 140)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 8, 0)
        };
        idBadge.Child = new TextBlock
        {
            Text = course.ClassId,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White)
        };
        Grid.SetColumn(idBadge, 0);
        row1.Children.Add(idBadge);

        var title = new TextBlock
        {
            Text = course.ClassTitle,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 51, 51, 51)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        row1.Children.Add(title);

        var price = new TextBlock
        {
            Text = course.ClassRate > 0 ? $"฿{course.ClassRate:N0}" : "",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 47, 47)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(price, 2);
        row1.Children.Add(price);

        stack.Children.Add(row1);

        // Row 2: Sessions + Trainer
        var row2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        row2.Children.Add(new TextBlock
        {
            Text = course.SessionCountText,
            FontSize = 11,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158))
        });

        var trainerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        trainerPanel.Children.Add(new FontIcon
        {
            Glyph = "\uE77B",
            FontSize = 11,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 33, 150, 243))
        });
        trainerPanel.Children.Add(new TextBlock
        {
            Text = course.TrainerDisplayName,
            FontSize = 11,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 97, 97, 97)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 160
        });
        row2.Children.Add(trainerPanel);

        stack.Children.Add(row2);

        card.Child = stack;
        return card;
    }

    // ========================================================================
    // Search
    // ========================================================================

    private void CourseSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = CourseSearchBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(keyword))
        {
            RenderCourseList(_allCourses);
            return;
        }

        var filtered = _allCourses.Where(c =>
            c.ClassId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            c.ClassTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            c.TrainerDisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        RenderCourseList(filtered);
    }

    // ========================================================================
    // Selection
    // ========================================================================

    private void CourseListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CourseListView.SelectedItem is Border selectedBorder && selectedBorder.Tag is string compositeKey)
        {
            _selectedCourse = _allCourses.FirstOrDefault(c => c.CompositeKey == compositeKey);
            if (_selectedCourse != null)
            {
                PreviewCourseTitle.Text = _selectedCourse.ClassTitle;
                PreviewCourseId.Text = _selectedCourse.ClassId;
                PreviewTrainer.Text = $"👤 {_selectedCourse.TrainerDisplayName}";
                PreviewPrice.Text = _selectedCourse.ClassRate > 0 ? $"฿{_selectedCourse.ClassRate:N0}" : "";
                SelectedCoursePreview.Visibility = Visibility.Visible;

                // Refresh visual state
                var keyword = CourseSearchBox.Text?.Trim() ?? string.Empty;
                var list = string.IsNullOrEmpty(keyword) ? _allCourses
                    : _allCourses.Where(c =>
                        c.ClassId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        c.ClassTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        c.TrainerDisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                RenderCourseList(list);
            }
        }
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

            var courseDisplay = $"{_selectedCourse!.ClassId} - {_selectedCourse.ClassTitle} ({_selectedCourse.TrainerDisplayName})";

            var confirmMsg = $"📚 {courseDisplay}\n"
                           + $"📅 {reserveDate:dd/MM/yyyy}\n"
                           + $"⏰ {reserveTime:hh\\:mm} - {endTime:hh\\:mm} ({duration} ชม.)\n"
                           + $"👤 {name}"
                           + (string.IsNullOrEmpty(phone) ? "" : $"\n📞 {phone}");

            bool confirmed = _notify != null
                ? await _notify.ShowConfirmAsync("ยืนยันจองสนามคอร์ส", confirmMsg, this.XamlRoot!)
                : await NotificationService.ConfirmAsync("ยืนยันจองสนามคอร์ส", confirmMsg, this.XamlRoot!);
            if (!confirmed) return;

            var duplicateMsg = await _vm.GetDuplicateReservationMessageAsync(name, reserveDate, reserveTime, duration);
            if (duplicateMsg != null)
            {
                _notify?.ShowWarning(duplicateMsg);
                return;
            }

            var reservation = new CourseCourtReservationItem
            {
                ReserveId = ReservationIdGenerator.GenerateCourseReservationId(DateTime.Now),
                CourtId = "00",
                ClassId = _selectedCourse.ClassId,
                TrainerId = _selectedCourse.TrainerId,
                RequestDate = DateTime.Now,
                ReserveDate = reserveDate,
                ReserveTime = reserveTime,
                Duration = duration,
                ReserveName = name,
                ReservePhone = phone,
                Status = "booked"
            };

            if (await _vm.AddCourseReservationAsync(reservation))
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
        if (_selectedCourse == null) { message = "กรุณาเลือกคอร์สเรียน"; return false; }
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
