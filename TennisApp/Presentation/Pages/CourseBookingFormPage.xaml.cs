using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Presentation.Dialogs;
using TennisApp.Helpers;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseBookingFormPage : Page
{
    private readonly BookingPageViewModel _vm = new();
    private NotificationService? _notify;
    private List<CourseItem> _allCourses = new();
    private CourseItem? _selectedCourse;
    private DateTime? _selectedDate;
    private CourseCourtReservationItem? _editingReservation;
    private bool _isEditMode;
    private bool _editDataLoaded;

    public CourseBookingFormPage()
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
        if (e.Parameter is CourseCourtReservationItem reservation)
        {
            _editingReservation = reservation;
            _isEditMode = true;
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        InputScrollHelper.Attach(this);
        try
        {
            await _vm.LoadAvailableCoursesAsync();
            _allCourses = _vm.AvailableCourses.ToList();
            RenderCourseList(_allCourses);

            if (_isEditMode && _editingReservation != null && !_editDataLoaded)
            {
                _editDataLoaded = true;
                LoadEditData(_editingReservation);
            }
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        InputScrollHelper.Detach(this);
    }

    // ========================================================================
    // Edit Mode — Pre-fill form
    // ========================================================================

    private void LoadEditData(CourseCourtReservationItem r)
    {
        // Header
        HeaderText.Text = "แก้ไขการจองคอร์ส";
        SubmitButtonText.Text = "บันทึกการแก้ไข";

        // Select course — ใช้ "|" ตาม CourseItem.CompositeKey
        var matchKey = $"{r.ClassId}|{r.TrainerId}";
        _selectedCourse = _allCourses.FirstOrDefault(c => c.CompositeKey == matchKey);
        if (_selectedCourse != null)
        {
            RenderCourseList(_allCourses);
        }

        // Date
        _selectedDate = r.ReserveDate;
        TxtDateDisplay.Text = r.ReserveDate.ToString("dd/MM/yyyy");
        TxtDateDisplay.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);

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

        // End Time
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

        var stack = new StackPanel { Spacing = 3 };

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
    // Date Picker Dialog
    // ========================================================================

    private async void BtnPickDate_Click(object sender, RoutedEventArgs e)
    {
        var result = await DatePickerDialog.ShowAsync(this.XamlRoot!, _selectedDate);
        if (result.HasValue)
        {
            _selectedDate = result.Value;
            TxtDateDisplay.Text = result.Value.ToString("dd/MM/yyyy");
            TxtDateDisplay.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
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

            var courseDisplay = $"{_selectedCourse!.ClassId} - {_selectedCourse.ClassTitle} ({_selectedCourse.TrainerDisplayName})";

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

            var confirmMsg = $"📚 {courseDisplay}\n"
                           + $"📅 {reserveDate:dd/MM/yyyy}\n"
                           + $"⏰ {reserveTime:hh\\:mm} - {endTime:hh\\:mm} ({duration:0.0} ชม.)\n"
                           + $"👤 {name}"
                           + (string.IsNullOrEmpty(phone) ? "" : $"\n📞 {phone}");

            var confirmTitle = _isEditMode ? "ยืนยันแก้ไขการจองคอร์ส" : "ยืนยันจองสนามคอร์ส";
            bool confirmed = _notify != null
                ? await _notify.ShowConfirmAsync(confirmTitle, confirmMsg, this.XamlRoot!)
                : await NotificationService.ConfirmAsync(confirmTitle, confirmMsg, this.XamlRoot!);
            if (!confirmed) return;

            bool success;
            if (_isEditMode)
            {
                var updated = _editingReservation!.Clone();
                updated.ClassId = _selectedCourse.ClassId;
                updated.TrainerId = _selectedCourse.TrainerId;
                updated.ReserveDate = reserveDate;
                updated.ReserveTime = reserveTime;
                updated.Duration = duration;
                updated.ReserveName = name;
                updated.ReservePhone = phone;
                success = await _vm.UpdateCourseReservationAsync(updated);
            }
            else
            {
                var dbService = ((App)Application.Current).DatabaseService;
                dbService.EnsureInitialized();
                var reserveId = await ReservationIdGenerator.GenerateCourseReservationIdAsync(dbService, DateTime.Now);
                var reservation = new CourseCourtReservationItem
                {
                    ReserveId = reserveId,
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
                success = await _vm.AddCourseReservationAsync(reservation);
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
        if (_selectedCourse == null) { message = "กรุณาเลือกคอร์สเรียน"; return false; }
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
