using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourtUsageLogPage : Page
{
    public CourtUsageLogPageViewModel VM { get; } = new();
    private NotificationService? _notify;

    // State
    private DateTime _checkinDate = DateTime.Today;
    private string _selectedReservationId = string.Empty;
    private string _selectedReservationType = string.Empty; // "Paid" or "Course"
    private string _expandedCourtId = string.Empty; // which court card is expanded

    // Merged reservation list for date navigator
    private List<object> _bookedReservations = new();

    public CourtUsageLogPage()
    {
        this.InitializeComponent();
        DataContext = VM;
        this.Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);

        try
        {
            await Task.WhenAll(
                VM.LoadAvailableCourtsAsync(),
                VM.LoadAvailableCoursesAsync(),
                VM.LoadCourtStatusesAsync()
            );

            PopulateCourtComboBoxes();
            PopulateCourseComboBox();
            BuildCourtStatusCards();
            UpdateDateDisplay();
            await LoadBookedReservationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Load failed: {ex.Message}");
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    // ====================================================================
    // Tab Switching
    // ====================================================================

    private void TabCourtStatus_Click(object sender, RoutedEventArgs e)
    {
        SetTabActive(TabCourtStatus, TabCheckIn);
        CourtStatusTab.Visibility = Visibility.Visible;
        CheckInTab.Visibility = Visibility.Collapsed;
    }

    private void TabCheckIn_Click(object sender, RoutedEventArgs e)
    {
        SetTabActive(TabCheckIn, TabCourtStatus);
        CourtStatusTab.Visibility = Visibility.Collapsed;
        CheckInTab.Visibility = Visibility.Visible;
    }

    private static void SetTabActive(Button active, Button inactive)
    {
        active.Background = new SolidColorBrush(ParseColor("#4A148C"));
        active.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        inactive.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        inactive.Foreground = new SolidColorBrush(ParseColor("#666666"));
    }

    // ====================================================================
    // Mode Switching (Reservation / Walk-in)
    // ====================================================================

    private void ModeReservation_Click(object sender, RoutedEventArgs e)
    {
        SetTabActive(ModeReservation, ModeWalkIn);
        ReservationModePanel.Visibility = Visibility.Visible;
        WalkInModePanel.Visibility = Visibility.Collapsed;
    }

    private void ModeWalkIn_Click(object sender, RoutedEventArgs e)
    {
        SetTabActive(ModeWalkIn, ModeReservation);
        ReservationModePanel.Visibility = Visibility.Collapsed;
        WalkInModePanel.Visibility = Visibility.Visible;
    }

    // ====================================================================
    // Tab 1: Court Status Cards (built in code)
    // ====================================================================

    private void BuildCourtStatusCards()
    {
        CourtStatusListView.Items.Clear();

        foreach (var court in VM.CourtStatuses)
        {
            var isExpanded = court.IsInUse && court.CourtId == _expandedCourtId;
            CourtStatusListView.Items.Add(BuildCourtCard(court, isExpanded));
        }
    }

    private Border BuildCourtCard(CourtStatusItem court, bool expanded)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(ParseColor(court.IsInUse ? "#FFE0B2" : "#E8E8E8")),
            BorderThickness = new Thickness(court.IsInUse ? 1.5 : 1),
            Padding = new Thickness(16, 14, 16, 14),
            Tag = court.CourtId
        };

        var stack = new StackPanel { Spacing = 8 };

        // Header row: Court name + Status badge (tappable to expand/collapse)
        var headerGrid = new Grid { Tag = court.CourtId };
        if (court.IsInUse)
        {
            headerGrid.Tapped += CourtCard_HeaderTapped;
        }

        var courtNamePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        courtNamePanel.Children.Add(new FontIcon
        {
            Glyph = "\uE707",
            FontSize = 16,
            Foreground = new SolidColorBrush(ParseColor("#4A148C"))
        });
        courtNamePanel.Children.Add(new TextBlock
        {
            Text = court.CourtDisplayName,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ParseColor("#333333"))
        });
        headerGrid.Children.Add(courtNamePanel);

        var badge = new Border
        {
            Background = new SolidColorBrush(ParseColor(court.IsInUse ? "#FF9800" : "#4CAF50")),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 3, 10, 3),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        badge.Child = new TextBlock
        {
            Text = court.IsInUse ? "กำลังใช้งาน" : "ว่าง",
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
        };
        headerGrid.Children.Add(badge);
        stack.Children.Add(headerGrid);

        if (court.IsInUse)
        {
            // User info row
            var infoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
            infoRow.Children.Add(MakeIconText("\uE77B", court.UserName, "#333"));
            infoRow.Children.Add(MakeIconText("\uE823", $"{court.StartTimeDisplay}→{court.EndTimeDisplay}", "#555"));
            stack.Children.Add(infoRow);

            // Type row
            var typeDisplay = court.UsageType == "Paid" ? "เช่าสนาม" : court.CourseTitle;
            var typeBg = court.UsageType == "Paid" ? "#E3F2FD" : "#F3E5F5";
            var typeFg = court.UsageType == "Paid" ? "#1565C0" : "#7B1FA2";
            var typeIcon = court.UsageType == "Paid" ? "\uE8CB" : "\uE82D";

            var typeBorder = new Border
            {
                Background = new SolidColorBrush(ParseColor(typeBg)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4)
            };
            typeBorder.Child = MakeIconText(typeIcon, typeDisplay, typeFg);
            stack.Children.Add(typeBorder);

            if (expanded)
            {
                // Divider
                stack.Children.Add(new Border
                {
                    Background = new SolidColorBrush(ParseColor("#F0F0F0")),
                    Height = 1,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                // Phone
                if (!string.IsNullOrEmpty(court.UserPhone))
                {
                    stack.Children.Add(MakeIconText("\uE717", court.UserPhone, "#888"));
                }

                // Duration
                stack.Children.Add(MakeIconText("\uE916", $"ระยะเวลา: {court.DurationDisplay}", "#555"));

                // Extend Time
                var extendGrid = new Grid { ColumnSpacing = 8 };
                extendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                extendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var extendCombo = new ComboBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 38,
                    CornerRadius = new CornerRadius(6),
                    FontSize = 13,
                    Tag = court.CourtId
                };
                extendCombo.Items.Add(new ComboBoxItem { Content = "1.0 ชม.", Tag = "1.0", IsSelected = true });
                extendCombo.Items.Add(new ComboBoxItem { Content = "1.5 ชม.", Tag = "1.5" });
                extendCombo.Items.Add(new ComboBoxItem { Content = "2.0 ชม.", Tag = "2.0" });
                extendCombo.Items.Add(new ComboBoxItem { Content = "2.5 ชม.", Tag = "2.5" });
                extendCombo.Items.Add(new ComboBoxItem { Content = "3.0 ชม.", Tag = "3.0" });
                Grid.SetColumn(extendCombo, 0);
                extendGrid.Children.Add(extendCombo);

                var extendBtn = new Button
                {
                    Content = "ขยายเวลา",
                    Background = new SolidColorBrush(ParseColor("#E3F2FD")),
                    Foreground = new SolidColorBrush(ParseColor("#1565C0")),
                    Height = 38,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(14, 0, 14, 0),
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    Tag = court.CourtId
                };
                extendBtn.Click += BtnExtendTime_Click;
                Grid.SetColumn(extendBtn, 1);
                extendGrid.Children.Add(extendBtn);

                stack.Children.Add(extendGrid);

                // Price input (Paid only)
                if (court.UsageType == "Paid")
                {
                    var pricePanel = new StackPanel { Spacing = 5 };
                    pricePanel.Children.Add(new TextBlock
                    {
                        Text = "ค่าบริการ (บาท)",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(ParseColor("#666"))
                    });
                    var priceBox = new TextBox
                    {
                        Text = court.Price > 0 ? court.Price.ToString() : "",
                        PlaceholderText = "0",
                        Height = 38,
                        CornerRadius = new CornerRadius(6),
                        BorderBrush = new SolidColorBrush(ParseColor("#E0E0E0")),
                        InputScope = new InputScope(),
                        FontSize = 14,
                        Tag = court.CourtId
                    };
                    priceBox.InputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
                    pricePanel.Children.Add(priceBox);
                    stack.Children.Add(pricePanel);
                }

                // Action buttons
                var btnGrid = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 4, 0, 0) };
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var cancelBtn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 42,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(ParseColor("#FFEBEE")),
                    Foreground = new SolidColorBrush(ParseColor("#C62828")),
                    BorderThickness = new Thickness(0),
                    Tag = court.CourtId
                };
                cancelBtn.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE711", FontSize = 13, Foreground = new SolidColorBrush(ParseColor("#C62828")) },
                        new TextBlock { Text = "ยกเลิก", FontSize = 13 }
                    }
                };
                cancelBtn.Click += BtnCancelUsage_Click;
                Grid.SetColumn(cancelBtn, 0);
                btnGrid.Children.Add(cancelBtn);

                var endBtn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 42,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(ParseColor("#4A148C")),
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                    BorderThickness = new Thickness(0),
                    Tag = court.CourtId
                };
                endBtn.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE73E", FontSize = 13, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) },
                        new TextBlock { Text = "สิ้นสุดการใช้งาน", FontSize = 13 }
                    }
                };
                endBtn.Click += BtnEndUsage_Click;
                Grid.SetColumn(endBtn, 1);
                btnGrid.Children.Add(endBtn);

                stack.Children.Add(btnGrid);
            }
        }

        card.Child = stack;
        return card;
    }

    // ====================================================================
    // Tab 1: Court Actions (Extend / End / Cancel)
    // ====================================================================

    private async void BtnExtendTime_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var courtId = btn.Tag?.ToString() ?? "";
        var court = VM.CourtStatuses.FirstOrDefault(c => c.CourtId == courtId);
        if (court == null) return;

        VM.ShowDetailCard(court);

        // Find the ComboBox in same parent
        var parent = btn.Parent as Grid;
        var combo = parent?.Children.OfType<ComboBox>().FirstOrDefault();
        if (combo?.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            if (double.TryParse(item.Tag.ToString(), out var hours))
                VM.ExtendHours = hours;
        }

        var conflictMessage = await VM.CheckExtendConflictAsync();
        if (conflictMessage != null)
        {
            _notify?.ShowWarning($"ไม่สามารถขยายเวลาได้\n{conflictMessage}");
            return;
        }

        var newDuration = court.Duration + VM.ExtendHours;
        var newEndTime = court.StartTime.Add(TimeSpan.FromHours(newDuration));

        var confirmed = await ShowConfirm("ขยายเวลา",
            $"ขยายเวลา {court.CourtDisplayName}?\n" +
            $"เพิ่ม: {VM.ExtendHours:0.0} ชั่วโมง → รวม {newDuration:0.0} ชม.\n" +
            $"สิ้นสุดใหม่: {newEndTime:hh\\:mm}");
        if (!confirmed) return;

        if (await VM.ExtendUsageTimeAsync())
        {
            _notify?.ShowSuccess("ขยายเวลาเรียบร้อย");
            await RefreshCourtStatus();
        }
        else
            _notify?.ShowError("ไม่สามารถขยายเวลาได้");
    }

    private async void BtnEndUsage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var courtId = btn.Tag?.ToString() ?? "";
        var court = VM.CourtStatuses.FirstOrDefault(c => c.CourtId == courtId);
        if (court == null) return;

        VM.ShowDetailCard(court);

        // Find price TextBox in card
        if (court.UsageType == "Paid")
        {
            var priceText = FindPriceTextBox(courtId);
            if (priceText != null && double.TryParse(priceText, out var price))
                VM.EndUsagePrice = (int)price;
            else
            {
                _notify?.ShowWarning("กรุณากรอกค่าบริการ");
                return;
            }
        }
        else
        {
            VM.EndUsagePrice = 0;
        }

        var confirmMsg = court.UsageType == "Paid"
            ? $"สิ้นสุดการใช้งาน {court.CourtDisplayName}?\n" +
              $"ผู้ใช้: {court.UserName}\nระยะเวลา: {court.DurationDisplay}\nค่าบริการ: ฿{VM.EndUsagePrice:N0}"
            : $"สิ้นสุดการใช้งาน {court.CourtDisplayName}?\n" +
              $"คอร์ส: {court.CourseTitle}\nผู้ใช้: {court.UserName}\nระยะเวลา: {court.DurationDisplay}";

        if (!await ShowConfirm("สิ้นสุดการใช้งาน", confirmMsg)) return;

        if (await VM.EndUsageAsync())
        {
            _notify?.ShowSuccess("สิ้นสุดการใช้งานเรียบร้อย");
            await RefreshCourtStatus();
        }
        else
            _notify?.ShowError("ไม่สามารถสิ้นสุดการใช้งานได้");
    }

    private async void BtnCancelUsage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var courtId = btn.Tag?.ToString() ?? "";
        var court = VM.CourtStatuses.FirstOrDefault(c => c.CourtId == courtId);
        if (court == null) return;

        VM.ShowDetailCard(court);

        if (!await ShowConfirm("ยกเลิกการใช้งาน",
            $"ยกเลิก {court.CourtDisplayName}?\nผู้ใช้: {court.UserName}\nไม่สามารถย้อนกลับได้"))
            return;

        if (await VM.CancelUsageAsync())
        {
            _notify?.ShowSuccess("ยกเลิกเรียบร้อย");
            await RefreshCourtStatus();
        }
        else
            _notify?.ShowError("ไม่สามารถยกเลิกได้");
    }

    private string? FindPriceTextBox(string courtId)
    {
        // Walk the visual tree of CourtStatusListView to find TextBox with matching Tag
        foreach (var item in CourtStatusListView.Items)
        {
            if (item is Border border && border.Tag?.ToString() == courtId)
            {
                return FindTextBoxInElement(border, courtId);
            }
        }
        return null;
    }

    private string? FindTextBoxInElement(DependencyObject parent, string courtId)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox tb && tb.Tag?.ToString() == courtId)
                return tb.Text;
            var result = FindTextBoxInElement(child, courtId);
            if (result != null) return result;
        }
        return null;
    }

    // ====================================================================
    // Tab 2: Date Navigator
    // ====================================================================

    private void BtnPrevDate_Click(object sender, RoutedEventArgs e)
    {
        _checkinDate = _checkinDate.AddDays(-1);
        UpdateDateDisplay();
        _ = LoadBookedReservationsAsync();
    }

    private void BtnNextDate_Click(object sender, RoutedEventArgs e)
    {
        _checkinDate = _checkinDate.AddDays(1);
        UpdateDateDisplay();
        _ = LoadBookedReservationsAsync();
    }

    private void UpdateDateDisplay()
    {
        var thai = new CultureInfo("th-TH");
        var isToday = _checkinDate.Date == DateTime.Today;
        CheckInDateText.Text = $"{(isToday ? "วันนี้ — " : "")}{_checkinDate:dd/MM/yyyy}";
        CheckInDayText.Text = _checkinDate.ToString("dddd", thai);
    }

    // ====================================================================
    // Tab 2: Load Booked Reservations for Date
    // ====================================================================

    private async Task LoadBookedReservationsAsync()
    {
        try
        {
            _selectedReservationId = string.Empty;
            _selectedReservationType = string.Empty;
            SelectedReservationPanel.Visibility = Visibility.Collapsed;

            _bookedReservations.Clear();
            BookedReservationsListView.Items.Clear();

            // Load both paid and course reservations for the date (status=booked only)
            var paidRes = (await VM.LoadPaidReservationsByDateAsync(_checkinDate))
                .Where(r => r.Status == "booked").ToList();
            var courseRes = (await VM.LoadCourseReservationsByDateAsync(_checkinDate))
                .Where(r => r.Status == "booked").ToList();

            foreach (var r in paidRes.OrderBy(r => r.ReserveTime))
            {
                _bookedReservations.Add(r);
                BookedReservationsListView.Items.Add(BuildReservationCard(r));
            }

            foreach (var r in courseRes.OrderBy(r => r.ReserveTime))
            {
                _bookedReservations.Add(r);
                BookedReservationsListView.Items.Add(BuildCourseReservationCard(r));
            }

            var hasItems = _bookedReservations.Count > 0;
            BookedReservationsListView.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            EmptyReservationPanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            EmptyReservationText.Text = _checkinDate.Date == DateTime.Today
                ? "ไม่มีรายการจองวันนี้"
                : $"ไม่มีรายการจองวันที่ {_checkinDate:dd/MM/yyyy}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadBookedReservations: {ex.Message}");
        }
    }

    private Border BuildReservationCard(PaidCourtReservationItem r)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(ParseColor("#E8E8E8")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 12, 14, 12),
            Tag = r.ReserveId
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel { Spacing = 4 };
        info.Children.Add(new TextBlock
        {
            Text = r.ReserveName,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ParseColor("#333"))
        });

        var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        timeRow.Children.Add(MakeIconText("\uE823", r.TimeRangeDisplay, "#555"));
        timeRow.Children.Add(MakeIconText("\uE916", r.DurationDisplay, "#555"));
        info.Children.Add(timeRow);

        var typeBorder = new Border
        {
            Background = new SolidColorBrush(ParseColor("#E3F2FD")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        typeBorder.Child = new TextBlock
        {
            Text = "เช่าสนาม",
            FontSize = 11,
            Foreground = new SolidColorBrush(ParseColor("#1565C0"))
        };
        info.Children.Add(typeBorder);

        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var selectBtn = new Button
        {
            Content = "เลือก",
            Background = new SolidColorBrush(ParseColor("#4A148C")),
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(0),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = r.ReserveId
        };
        selectBtn.Click += (s, e) => SelectReservation(r.ReserveId, "Paid", r);
        Grid.SetColumn(selectBtn, 1);
        grid.Children.Add(selectBtn);

        card.Child = grid;
        return card;
    }

    private Border BuildCourseReservationCard(CourseCourtReservationItem r)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(ParseColor("#E8E8E8")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 12, 14, 12),
            Tag = r.ReserveId
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel { Spacing = 4 };
        info.Children.Add(new TextBlock
        {
            Text = r.ReserveName,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(ParseColor("#333"))
        });

        var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        timeRow.Children.Add(MakeIconText("\uE823", r.TimeRangeDisplay, "#555"));
        timeRow.Children.Add(MakeIconText("\uE916", r.DurationDisplay, "#555"));
        info.Children.Add(timeRow);

        var typeBorder = new Border
        {
            Background = new SolidColorBrush(ParseColor("#F3E5F5")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        typeBorder.Child = new TextBlock
        {
            Text = r.ClassDisplayName,
            FontSize = 11,
            Foreground = new SolidColorBrush(ParseColor("#7B1FA2"))
        };
        info.Children.Add(typeBorder);

        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        var selectBtn = new Button
        {
            Content = "เลือก",
            Background = new SolidColorBrush(ParseColor("#4A148C")),
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            Height = 34,
            Padding = new Thickness(14, 0, 14, 0),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(0),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = r.ReserveId
        };
        selectBtn.Click += (s, e) => SelectReservation(r.ReserveId, "Course", r);
        Grid.SetColumn(selectBtn, 1);
        grid.Children.Add(selectBtn);

        card.Child = grid;
        return card;
    }

    private void SelectReservation(string reserveId, string type, object reservation)
    {
        _selectedReservationId = reserveId;
        _selectedReservationType = type;

        string infoText;
        if (type == "Paid" && reservation is PaidCourtReservationItem paid)
        {
            VM.SelectedReserveId = paid.ReserveId;
            VM.UsageType = "Paid";
            VM.UsageDate = paid.ReserveDate;
            VM.UsageTime = paid.ReserveTime;
            VM.UsageDuration = paid.Duration;
            VM.CustomerName = paid.ReserveName;
            VM.CustomerPhone = paid.ReservePhone;
            VM.IsFromReservation = true;
            VM.IsWalkIn = false;

            infoText = $"👤 {paid.ReserveName}\n⏰ {paid.TimeRangeDisplay} ({paid.DurationDisplay})\n🏷️ เช่าสนาม";
        }
        else if (type == "Course" && reservation is CourseCourtReservationItem course)
        {
            VM.SelectedReserveId = course.ReserveId;
            VM.UsageType = "Course";
            VM.SelectedCourseId = course.ClassId;
            VM.UsageDate = course.ReserveDate;
            VM.UsageTime = course.ReserveTime;
            VM.UsageDuration = course.Duration;
            VM.CustomerName = course.ReserveName;
            VM.CustomerPhone = course.ReservePhone;
            VM.IsFromReservation = true;
            VM.IsWalkIn = false;

            infoText = $"👤 {course.ReserveName}\n⏰ {course.TimeRangeDisplay} ({course.DurationDisplay})\n📚 {course.ClassDisplayName}";
        }
        else return;

        SelectedReservationInfo.Text = infoText;
        SelectedReservationPanel.Visibility = Visibility.Visible;
    }

    // ====================================================================
    // Tab 2: Check-in from Reservation
    // ====================================================================

    private async void BtnCheckinFromReservation_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedReservationId))
        {
            _notify?.ShowWarning("กรุณาเลือกรายการจองก่อน");
            return;
        }

        if (ReservationCourtComboBox.SelectedItem is not ComboBoxItem courtItem || courtItem.Tag == null)
        {
            _notify?.ShowWarning("กรุณาเลือกสนาม");
            return;
        }

        VM.SelectedCourtId = courtItem.Tag.ToString()!;

        // ✅ ตรวจสอบซ้อนทับก่อนเช็คอิน
        var conflict = await VM.CheckCourtConflictForCheckinAsync(
            VM.SelectedCourtId, VM.UsageDate, VM.UsageTime, VM.UsageDuration, VM.SelectedReserveId);
        if (conflict != null)
        {
            _notify?.ShowWarning(conflict);
            return;
        }

        var confirmed = await ShowConfirm("ยืนยันเช็คอิน",
            $"เช็คอินเข้า สนาม {VM.SelectedCourtId}?\n\n{SelectedReservationInfo.Text}");
        if (!confirmed) return;

        bool success = _selectedReservationType == "Paid"
            ? await VM.StartPaidUsageAsync()
            : await VM.StartCourseUsageAsync();

        if (success)
        {
            _notify?.ShowSuccess("เช็คอินเรียบร้อย");
            SelectedReservationPanel.Visibility = Visibility.Collapsed;
            _selectedReservationId = string.Empty;
            await RefreshCourtStatus();
            await LoadBookedReservationsAsync();
        }
        else
            _notify?.ShowError("ไม่สามารถเช็คอินได้");
    }

    // ====================================================================
    // Tab 2: Walk-in Check-in
    // ====================================================================

    private async void BtnCheckinWalkIn_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (WalkInCourtComboBox.SelectedItem is not ComboBoxItem courtItem || courtItem.Tag == null)
        {
            _notify?.ShowWarning("กรุณาเลือกสนาม"); return;
        }
        if (WalkInTimeComboBox.SelectedItem is not ComboBoxItem timeItem || timeItem.Tag == null)
        {
            _notify?.ShowWarning("กรุณาเลือกเวลาเริ่ม"); return;
        }
        if (WalkInDurationComboBox.SelectedItem is not ComboBoxItem durItem || durItem.Tag == null)
        {
            _notify?.ShowWarning("กรุณาเลือกระยะเวลา"); return;
        }
        if (string.IsNullOrWhiteSpace(WalkInNameTextBox.Text))
        {
            _notify?.ShowWarning("กรุณากรอกชื่อผู้ใช้งาน"); return;
        }

        var usageTypeItem = WalkInTypeComboBox.SelectedItem as ComboBoxItem;
        var usageType = usageTypeItem?.Tag?.ToString() ?? "Paid";

        if (usageType == "Course" && (WalkInCourseComboBox.SelectedItem is not ComboBoxItem courseItem || courseItem.Tag == null))
        {
            _notify?.ShowWarning("กรุณาเลือกคอร์ส"); return;
        }

        var selectedCourtId = courtItem.Tag.ToString()!;
        var selectedTime = TimeSpan.Parse(timeItem.Tag.ToString()!);
        var selectedDuration = double.Parse(durItem.Tag.ToString()!, CultureInfo.InvariantCulture);

        // ✅ ตรวจสอบซ้อนทับก่อนเช็คอิน Walk-in
        var conflict = await VM.CheckCourtConflictForCheckinAsync(
            selectedCourtId, DateTime.Today, selectedTime, selectedDuration);
        if (conflict != null)
        {
            _notify?.ShowWarning(conflict);
            return;
        }

        VM.SelectedCourtId = selectedCourtId;
        VM.UsageType = usageType;
        VM.UsageDate = DateTime.Today;
        VM.UsageTime = selectedTime;
        VM.UsageDuration = selectedDuration;
        VM.CustomerName = WalkInNameTextBox.Text.Trim();
        VM.CustomerPhone = WalkInPhoneTextBox.Text.Trim();
        VM.IsWalkIn = true;
        VM.IsFromReservation = false;
        VM.SelectedReserveId = string.Empty;

        if (usageType == "Course")
        {
            VM.SelectedCourseId = (WalkInCourseComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        }

        var confirmed = await ShowConfirm("ยืนยันเช็คอิน Walk-in",
            $"สนาม {VM.SelectedCourtId}\n" +
            $"เวลา: {VM.UsageTime:hh\\:mm} → {VM.EstimatedEndTime}\n" +
            $"ผู้ใช้: {VM.CustomerName}\n" +
            (usageType == "Paid" ? "ประเภท: เช่าสนาม" : $"คอร์ส: {(WalkInCourseComboBox.SelectedItem as ComboBoxItem)?.Content}"));
        if (!confirmed) return;

        bool success = usageType == "Paid"
            ? await VM.StartPaidUsageAsync()
            : await VM.StartCourseUsageAsync();

        if (success)
        {
            _notify?.ShowSuccess("เช็คอินเรียบร้อย");
            BtnClearWalkIn_Click(sender, e);
            await RefreshCourtStatus();
        }
        else
            _notify?.ShowError("ไม่สามารถเช็คอินได้");
    }

    private void BtnClearWalkIn_Click(object sender, RoutedEventArgs e)
    {
        WalkInCourtComboBox.SelectedIndex = -1;
        WalkInTimeComboBox.SelectedIndex = -1;
        WalkInDurationComboBox.SelectedIndex = -1;
        WalkInTypeComboBox.SelectedIndex = 0;
        WalkInCourseComboBox.SelectedIndex = -1;
        WalkInNameTextBox.Text = string.Empty;
        WalkInPhoneTextBox.Text = string.Empty;
        WalkInEndTimePanel.Visibility = Visibility.Collapsed;
        WalkInCoursePanel.Visibility = Visibility.Collapsed;
        VM.ClearForm();
    }

    // ====================================================================
    // Walk-in ComboBox events
    // ====================================================================

    private void WalkInTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WalkInCoursePanel == null) return;
        var tag = (WalkInTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        WalkInCoursePanel.Visibility = tag == "Course" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void WalkInTimeOrDuration_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (WalkInTimeComboBox.SelectedItem is ComboBoxItem timeItem && timeItem.Tag != null &&
            WalkInDurationComboBox.SelectedItem is ComboBoxItem durItem && durItem.Tag != null)
        {
            if (TimeSpan.TryParse(timeItem.Tag.ToString(), out var start) &&
                double.TryParse(durItem.Tag.ToString(), System.Globalization.NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var dur))
            {
                var end = start.Add(TimeSpan.FromHours(dur));
                WalkInEndTimeText.Text = end.ToString(@"hh\:mm");
                WalkInEndTimePanel.Visibility = Visibility.Visible;
                return;
            }
        }
        WalkInEndTimePanel.Visibility = Visibility.Collapsed;
    }

    // ====================================================================
    // Populate ComboBoxes
    // ====================================================================

    private void PopulateCourtComboBoxes()
    {
        PopulateCourtCombo(ReservationCourtComboBox);
        PopulateCourtCombo(WalkInCourtComboBox);
    }

    private void PopulateCourtCombo(ComboBox combo)
    {
        combo.Items.Clear();
        foreach (var court in VM.AvailableCourts)
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content = $"สนาม {court.CourtID}",
                Tag = court.CourtID
            });
        }
    }

    private void PopulateCourseComboBox()
    {
        WalkInCourseComboBox.Items.Clear();
        foreach (var course in VM.AvailableCourses)
        {
            WalkInCourseComboBox.Items.Add(new ComboBoxItem
            {
                Content = course.ClassTitle,
                Tag = course.ClassId
            });
        }
    }

    // ====================================================================
    // Refresh
    // ====================================================================

    private async Task RefreshCourtStatus()
    {
        await VM.LoadCourtStatusesAsync();
        BuildCourtStatusCards();
    }

    // ====================================================================
    // Helpers
    // ====================================================================

    private static StackPanel MakeIconText(string glyph, string text, string color)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        sp.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 13,
            Foreground = new SolidColorBrush(ParseColor(color))
        });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = new SolidColorBrush(ParseColor(color))
        });
        return sp;
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber));
        return Windows.UI.Color.FromArgb(255, 158, 158, 158);
    }

    private async Task<bool> ShowConfirm(string title, string content)
    {
        if (_notify == null) return false;
        return await _notify.ShowConfirmAsync(title, content, this.XamlRoot!);
    }

    private void CourtCard_HeaderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        var courtId = el.Tag?.ToString() ?? "";
        var court = VM.CourtStatuses.FirstOrDefault(c => c.CourtId == courtId);
        if (court == null || !court.IsInUse) return;

        // Toggle expand
        _expandedCourtId = _expandedCourtId == courtId ? string.Empty : courtId;

        if (_expandedCourtId == courtId)
            VM.ShowDetailCard(court);

        BuildCourtStatusCards();
    }
}
