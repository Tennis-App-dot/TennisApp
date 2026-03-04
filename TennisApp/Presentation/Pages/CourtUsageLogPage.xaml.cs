using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

/// <summary>
/// หน้าบันทึกการเข้าใช้งานสนาม (Court Usage Log Page)
/// รองรับทั้งการ Walk-in และการเข้าใช้จากการจองล่วงหน้า
/// </summary>
public sealed partial class CourtUsageLogPage : Page
{
    public CourtUsageLogPageViewModel VM { get; } = new();

    public CourtUsageLogPage()
    {
        this.InitializeComponent();
        DataContext = VM;
        this.Loaded += CourtUsageLogPage_Loaded;
    }

    private async void CourtUsageLogPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // โหลดข้อมูลพื้นฐานแบบ parallel (ไม่ depend กัน)
            await Task.WhenAll(
                VM.LoadAvailableCourtsAsync(),
                VM.LoadAvailableCoursesAsync(),
                VM.LoadReservationsAsync()
            );

            // โหลดข้อมูลที่ต้อง populate UI หลังจากข้อมูลพื้นฐานพร้อม
            PopulateCourtComboBox();
            PopulateCourseComboBox();
            UpdateCourseVisibility();

            // โหลด court status + logs แบบ parallel
            await Task.WhenAll(
                VM.LoadUsageLogsAsync(),
                VM.LoadCourtStatusesAsync()
            );

            CourtStatusListView.ItemsSource = VM.CourtStatuses;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ โหลดข้อมูลล้มเหลว: {ex.Message}");
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถโหลดข้อมูลได้: {ex.Message}");
        }
    }

    // ========================================================================
    // Populate ComboBoxes
    // ========================================================================

    private void PopulateCourtComboBox()
    {
        CourtSelectionComboBox.Items.Clear();
        var defaultItem = new ComboBoxItem { Content = "เลือกสนาม", IsSelected = true };
        CourtSelectionComboBox.Items.Add(defaultItem);

        foreach (var court in VM.AvailableCourts)
        {
            CourtSelectionComboBox.Items.Add(new ComboBoxItem
            {
                Content = $"สนาม {court.CourtID}",
                Tag = court.CourtID
            });
        }
    }

    private void PopulateCourseComboBox()
    {
        CourseSelectionComboBox.Items.Clear();
        var defaultItem = new ComboBoxItem { Content = "เลือกคอร์ส", IsSelected = true };
        CourseSelectionComboBox.Items.Add(defaultItem);

        foreach (var course in VM.AvailableCourses)
        {
            CourseSelectionComboBox.Items.Add(new ComboBoxItem
            {
                Content = course.ClassTitle,
                Tag = course.ClassId
            });
        }
    }

    // ========================================================================
    // Form Helpers
    // ========================================================================

    private void UpdateEstimatedEndTime()
    {
        if (UsageTimeComboBox.SelectedItem is ComboBoxItem timeItem &&
            UsageDurationComboBox.SelectedItem is ComboBoxItem durationItem)
        {
            var timeTag = timeItem.Tag?.ToString();
            var durationTag = durationItem.Tag?.ToString();

            if (!string.IsNullOrEmpty(timeTag) && !string.IsNullOrEmpty(durationTag))
            {
                if (TimeSpan.TryParse(timeTag, out var startTime) &&
                    double.TryParse(durationTag, out var duration))
                {
                    var endTime = startTime.Add(TimeSpan.FromHours(duration));
                    EstimatedEndTimeTextBox.Text = endTime.ToString(@"hh\:mm");
                    return;
                }
            }
        }
        EstimatedEndTimeTextBox.Text = "--:--";
    }

    private void UpdateCourseVisibility()
    {
        if (UsageTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var usageType = selectedItem.Tag?.ToString();
            CourseSelectionPanel.Visibility = usageType == "Course" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ========================================================================
    // Search Reservation
    // ========================================================================

    private async void BtnSearchReservation_Click(object sender, RoutedEventArgs e)
    {
        var reserveId = ReserveIdSearchTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(reserveId))
        {
            await ShowMessage("ข้อมูลไม่ครบถ้วน", "กรุณากรอกรหัสการจองที่ต้องการค้นหา");
            return;
        }

        var found = await VM.SearchReservationAsync(reserveId);

        if (found)
        {
            // Auto-fill form
            foreach (ComboBoxItem item in UsageTimeComboBox.Items)
            {
                if (item.Tag?.ToString() == VM.UsageTime.ToString(@"hh\:mm"))
                {
                    UsageTimeComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in UsageDurationComboBox.Items)
            {
                if (item.Tag?.ToString() == VM.UsageDuration.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                {
                    UsageDurationComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in CourtSelectionComboBox.Items)
            {
                if (item.Tag?.ToString() == VM.SelectedCourtId)
                {
                    CourtSelectionComboBox.SelectedItem = item;
                    break;
                }
            }

            // Set usage type
            foreach (ComboBoxItem item in UsageTypeComboBox.Items)
            {
                if (item.Tag?.ToString() == VM.UsageType)
                {
                    UsageTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            if (VM.UsageType == "Course")
            {
                foreach (ComboBoxItem item in CourseSelectionComboBox.Items)
                {
                    if (item.Tag?.ToString() == VM.SelectedCourseId)
                    {
                        CourseSelectionComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            CustomerNameTextBox.Text = VM.CustomerName;
            CustomerPhoneTextBox.Text = VM.CustomerPhone;

            // Disable form fields since data comes from reservation (except court selection)
            SetFormFieldsEnabled(false);

            await ShowMessage("พบข้อมูลการจอง", $"พบการจอง: {reserveId}\nข้อมูลถูกกรอกลงในฟอร์มอัตโนมัติ");
        }
        else
        {
            await ShowMessage("ไม่พบข้อมูล", $"ไม่พบการจอง: {reserveId}\nหรือการจองนี้ถูกใช้งานไปแล้ว\nกรุณาตรวจสอบรหัสอีกครั้ง");
        }
    }

    // ========================================================================
    // Log Usage (Check-in)
    // ========================================================================

    private async void BtnLogUsage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!ValidateUsageInputs(out var validationMessage))
            {
                await ShowMessage("ข้อมูลไม่ครบถ้วน", validationMessage);
                return;
            }

            var usageTypeItem = UsageTypeComboBox.SelectedItem as ComboBoxItem;
            var usageType = usageTypeItem?.Tag?.ToString();

            VM.SelectedCourtId = (CourtSelectionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            VM.CustomerName = CustomerNameTextBox.Text.Trim();
            VM.CustomerPhone = CustomerPhoneTextBox.Text.Trim();
            VM.UsageType = usageType ?? "Paid";

            if (usageType == "Course")
            {
                VM.SelectedCourseId = (CourseSelectionComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
                VM.CalculatedPrice = 0;
            }

            // Confirmation
            var confirmMessage = usageType == "Paid"
                ? $"ยืนยันการเข้าใช้งาน?\n\n" +
                  $"สนาม: สนาม {VM.SelectedCourtId}\n" +
                  $"เวลา: {VM.UsageTime:hh\\:mm} - {EstimatedEndTimeTextBox.Text}\n" +
                  $"ผู้ใช้งาน: {VM.CustomerName}\n" +
                  $"ค่าบริการ: ฿{VM.CalculatedPrice}"
                : $"ยืนยันการเข้าใช้งาน?\n\n" +
                  $"สนาม: สนาม {VM.SelectedCourtId}\n" +
                  $"เวลา: {VM.UsageTime:hh\\:mm} - {EstimatedEndTimeTextBox.Text}\n" +
                  $"คอร์ส: {(CourseSelectionComboBox.SelectedItem as ComboBoxItem)?.Content}\n" +
                  $"ผู้ใช้งาน: {VM.CustomerName}";

            var confirmed = await ShowConfirm("ยืนยันเข้าใช้งาน", confirmMessage);
            if (!confirmed) return;

            bool success = usageType == "Paid"
                ? await VM.LogPaidUsageAsync()
                : await VM.LogCourseUsageAsync();

            if (success)
            {
                await ShowMessage("สำเร็จ", "บันทึกการเข้าใช้งานเรียบร้อย\nสถานะสนามจะอัปเดตอัตโนมัติ");
                BtnClearForm_Click(sender, e);
                CourtStatusListView.ItemsSource = null;
                CourtStatusListView.ItemsSource = VM.CourtStatuses;
            }
            else
            {
                await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถบันทึกการเข้าใช้งานได้");
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("เกิดข้อผิดพลาด", $"ไม่สามารถบันทึกได้: {ex.Message}");
        }
    }

    private bool ValidateUsageInputs(out string message)
    {
        message = string.Empty;

        if (UsageTimeComboBox.SelectedIndex <= 0)
        {
            message = "กรุณาเลือกเวลาที่เข้าใช้งาน";
            return false;
        }

        if (UsageDurationComboBox.SelectedIndex <= 0)
        {
            message = "กรุณาเลือกระยะเวลาที่ใช้งาน";
            return false;
        }

        if (CourtSelectionComboBox.SelectedIndex <= 0)
        {
            message = "กรุณาเลือกสนามที่ใช้งาน";
            return false;
        }

        var usageTypeItem = UsageTypeComboBox.SelectedItem as ComboBoxItem;
        var usageType = usageTypeItem?.Tag?.ToString();

        if (usageType == "Course" && CourseSelectionComboBox.SelectedIndex <= 0)
        {
            message = "กรุณาเลือกคอร์ส";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
        {
            message = "กรุณากรอกชื่อผู้ใช้งาน";
            return false;
        }

        return true;
    }

    // ========================================================================
    // Form Field Enable/Disable
    // ========================================================================

    /// <summary>
    /// Enable/Disable form fields when reservation data is loaded.
    /// Court selection stays enabled because court is assigned on this page.
    /// </summary>
    private void SetFormFieldsEnabled(bool enabled)
    {
        CustomerNameTextBox.IsEnabled = enabled;
        CustomerPhoneTextBox.IsEnabled = enabled;
        UsageTimeComboBox.IsEnabled = enabled;
        UsageDurationComboBox.IsEnabled = enabled;
        UsageTypeComboBox.IsEnabled = enabled;
        CourseSelectionComboBox.IsEnabled = enabled;
        // CourtSelectionComboBox stays enabled — court is chosen on this page
    }

    // ========================================================================
    // Clear Form
    // ========================================================================

    private void BtnClearForm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReserveIdSearchTextBox.Text = string.Empty;
            UsageTimeComboBox.SelectedIndex = 0;
            UsageDurationComboBox.SelectedIndex = 0;
            CourtSelectionComboBox.SelectedIndex = 0;
            CourseSelectionComboBox.SelectedIndex = 0;
            CustomerNameTextBox.Text = string.Empty;
            CustomerPhoneTextBox.Text = string.Empty;
            EstimatedEndTimeTextBox.Text = "--:--";

            PopulateCourtComboBox();
            VM.ClearForm();

            // Re-enable all form fields
            SetFormFieldsEnabled(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ล้างข้อมูลฟอร์มล้มเหลว: {ex.Message}");
        }
    }

    // ========================================================================
    // Court Status Detail Card
    // ========================================================================

    private void CourtStatusListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not CourtStatusItem courtStatus) return;
        if (!courtStatus.IsInUse) return;

        VM.ShowDetailCard(courtStatus);

        // Fill detail card UI
        DetailCardTitle.Text = courtStatus.CourtDisplayName;
        DetailUserNameText.Text = $"ชื่อผู้ใช้งาน: {courtStatus.UserName}";
        DetailPhoneText.Text = $"เบอร์โทรศัพท์: {courtStatus.UserPhone}";
        DetailStartTimeText.Text = $"เวลาที่เข้าใช้งานสนาม: {courtStatus.StartTimeDisplay}";
        DetailDurationText.Text = $"ระยะเวลาที่ต้องการใช้งาน: {courtStatus.DurationDisplay}";
        DetailPurposeText.Text = $"จุดประสงค์การใช้งาน: {courtStatus.UsageTypeDisplay}";
        DetailCourseText.Text = $"คอร์สเรียน: {(string.IsNullOrEmpty(courtStatus.CourseTitle) ? "-" : courtStatus.CourseTitle)}";
        DetailTotalDurationText.Text = $"{courtStatus.Duration:0.00} ชั่วโมง";
        DetailPriceTextBox.Text = courtStatus.Price.ToString();

        // Show/hide price panel based on type
        DetailPricePanel.Visibility = courtStatus.UsageType == "Paid" ? Visibility.Visible : Visibility.Collapsed;

        // Reset to Step 1
        DetailStep1Panel.Visibility = Visibility.Visible;
        DetailStep2Panel.Visibility = Visibility.Collapsed;

        DetailCardBorder.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// เปลี่ยนจาก Step 1 (รายละเอียด + ขยายเวลา) → Step 2 (สิ้นสุดการใช้งาน + กรอกค่าบริการ)
    /// </summary>
    private void BtnShowEndUsageStep_Click(object sender, RoutedEventArgs e)
    {
        if (VM.SelectedCourtStatus == null) return;

        // อัปเดตข้อมูล Step 2
        DetailTotalDurationText.Text = $"{VM.SelectedCourtStatus.Duration:0.00} ชั่วโมง";
        DetailPriceTextBox.Text = VM.SelectedCourtStatus.Price.ToString();

        // Show/hide price panel based on type
        DetailPricePanel.Visibility = VM.SelectedCourtStatus.UsageType == "Paid" ? Visibility.Visible : Visibility.Collapsed;

        // สลับจาก Step 1 → Step 2
        DetailStep1Panel.Visibility = Visibility.Collapsed;
        DetailStep2Panel.Visibility = Visibility.Visible;
    }

    private async void BtnExtendTime_Click(object sender, RoutedEventArgs e)
    {
        if (VM.SelectedCourtStatus == null) return;

        if (ExtendTimeComboBox.SelectedItem is ComboBoxItem extendItem && extendItem.Tag != null)
        {
            if (double.TryParse(extendItem.Tag.ToString(), out var hours))
            {
                VM.ExtendHours = hours;
            }
        }

        // ตรวจสอบว่าการขยายเวลาจะชนกับการจองถัดไปหรือไม่
        var conflictMessage = await VM.CheckExtendConflictAsync();
        if (conflictMessage != null)
        {
            await ShowMessage("ไม่สามารถขยายเวลาได้",
                $"⚠️ {conflictMessage}\n\nกรุณาเลือกระยะเวลาที่สั้นลง หรือแจ้งผู้จองคนถัดไป");
            return;
        }

        var newDuration = VM.SelectedCourtStatus.Duration + VM.ExtendHours;
        var newEndTime = VM.SelectedCourtStatus.StartTime.Add(TimeSpan.FromHours(newDuration));

        var confirmed = await ShowConfirm("ขยายเวลา",
            $"ขยายเวลาการใช้งาน {VM.SelectedCourtStatus.CourtDisplayName}?\n" +
            $"เพิ่ม: {VM.ExtendHours:0.0} ชั่วโมง\n" +
            $"รวมเป็น: {newDuration:0.0} ชั่วโมง\n" +
            $"ระยะเวลาสิ้นสุดใหม่: {newEndTime:hh\\:mm}");

        if (!confirmed) return;

        var success = await VM.ExtendUsageTimeAsync();

        if (success)
        {
            // Refresh detail card
            DetailTotalDurationText.Text = $"{VM.SelectedCourtStatus.Duration:0.00} ชั่วโมง";
            DetailDurationText.Text = $"ระยะเวลาที่ต้องการใช้งาน: {VM.SelectedCourtStatus.DurationDisplay}";
            CourtStatusListView.ItemsSource = null;
            CourtStatusListView.ItemsSource = VM.CourtStatuses;
            await ShowMessage("สำเร็จ", "ขยายระยะเวลาการใช้งานเรียบร้อย");
        }
        else
        {
            await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถขยายเวลาได้");
        }
    }

    private async void BtnEndUsage_Click(object sender, RoutedEventArgs e)
    {
        if (VM.SelectedCourtStatus == null) return;

        // Get price from TextBox (Paid only — Course ไม่คิดเงิน)
        if (VM.SelectedCourtStatus.UsageType == "Paid")
        {
            if (double.TryParse(DetailPriceTextBox.Text, out var price))
            {
                VM.EndUsagePrice = (int)price;
            }
            else
            {
                await ShowMessage("ข้อมูลไม่ถูกต้อง", "กรุณากรอกค่าบริการเป็นตัวเลข");
                return;
            }
        }
        else
        {
            // Course ไม่คิดเงิน — นักเรียนจ่ายค่าคอร์สแล้ว
            VM.EndUsagePrice = 0;
        }

        var confirmMessage = VM.SelectedCourtStatus.UsageType == "Paid"
            ? $"สิ้นสุดการใช้งาน {VM.SelectedCourtStatus.CourtDisplayName}?\n\n" +
              $"ผู้ใช้งาน: {VM.SelectedCourtStatus.UserName}\n" +
              $"ระยะเวลา: {VM.SelectedCourtStatus.DurationDisplay}\n" +
              $"ค่าบริการ: ฿{VM.EndUsagePrice:N0}"
            : $"สิ้นสุดการใช้งาน {VM.SelectedCourtStatus.CourtDisplayName}?\n\n" +
              $"คอร์ส: {VM.SelectedCourtStatus.CourseTitle}\n" +
              $"ผู้ใช้งาน: {VM.SelectedCourtStatus.UserName}\n" +
              $"ระยะเวลา: {VM.SelectedCourtStatus.DurationDisplay}";

        var confirmed = await ShowConfirm("สิ้นสุดการใช้งาน", confirmMessage);
        if (!confirmed) return;

        var success = await VM.EndUsageAsync();

        if (success)
        {
            DetailCardBorder.Visibility = Visibility.Collapsed;
            CourtStatusListView.ItemsSource = null;
            CourtStatusListView.ItemsSource = VM.CourtStatuses;
            await ShowMessage("สำเร็จ", "สิ้นสุดการใช้งานเรียบร้อย");
        }
        else
        {
            await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถสิ้นสุดการใช้งานได้");
        }
    }

    private async void BtnCancelUsage_Click(object sender, RoutedEventArgs e)
    {
        if (VM.SelectedCourtStatus == null) return;

        var confirmed = await ShowConfirm("ยกเลิกการใช้งาน",
            $"ยกเลิกการใช้งาน {VM.SelectedCourtStatus.CourtDisplayName}?\n\n" +
            $"ผู้ใช้งาน: {VM.SelectedCourtStatus.UserName}\n" +
            $"การดำเนินการนี้ไม่สามารถย้อนกลับได้");

        if (!confirmed) return;

        var success = await VM.CancelUsageAsync();

        if (success)
        {
            DetailCardBorder.Visibility = Visibility.Collapsed;
            CourtStatusListView.ItemsSource = null;
            CourtStatusListView.ItemsSource = VM.CourtStatuses;
            await ShowMessage("สำเร็จ", "ยกเลิกการใช้งานเรียบร้อย");
        }
        else
        {
            await ShowMessage("เกิดข้อผิดพลาด", "ไม่สามารถยกเลิกได้");
        }
    }

    // ========================================================================
    // Event Handlers
    // ========================================================================

    private void UsageTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCourseVisibility();
    }

    private void UsageTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsageTimeComboBox.SelectedItem is ComboBoxItem timeItem && timeItem.Tag != null)
        {
            if (TimeSpan.TryParse(timeItem.Tag.ToString(), out var time))
            {
                VM.UsageTime = time;
            }
        }
        UpdateEstimatedEndTime();
    }

    private void UsageDurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsageDurationComboBox.SelectedItem is ComboBoxItem durationItem && durationItem.Tag != null)
        {
            if (double.TryParse(durationItem.Tag.ToString(), out var duration))
            {
                VM.UsageDuration = duration;
            }
        }
        UpdateEstimatedEndTime();
    }

    // ========================================================================
    // Dialog Helpers
    // ========================================================================

    private async Task ShowMessage(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = new TextBlock
            {
                Text = title,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = content,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "ตกลง",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };
        await dialog.ShowAsync();
    }

    private async Task<bool> ShowConfirm(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = new TextBlock
            {
                Text = title,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = content,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "ยืนยัน",
            SecondaryButtonText = "ยกเลิก",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
