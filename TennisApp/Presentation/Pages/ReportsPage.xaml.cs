using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TennisApp.Helpers;
using TennisApp.Services;
using SkiaSharp;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace TennisApp.Presentation.Pages;

public sealed partial class ReportsPage : Page
{
    private DatabaseService _db = null!;
    private string _connectionString = string.Empty;
    private string _selectedPeriod = "all";
    private NotificationService? _notify;
    private DateTime? _customStartDate;
    private DateTime? _customEndDate;

    // Cache latest data for PDF export
    private ReservationSummary? _lastPaidRes;
    private ReservationSummary? _lastCourseRes;
    private UsageSummary? _lastPaidUsage;
    private UsageSummary? _lastCourseUsage;
    private List<CourtUsageRank>? _lastCourtRanking;
    private SystemStats? _lastSystemStats;
    private CourseRegistrationRevenue? _lastCourseRegRevenue;

    public ReportsPage()
    {
        this.InitializeComponent();
        this.Loaded += Page_Loaded;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        try
        {
            _db = ((App)Application.Current).DatabaseService;
            _db.EnsureInitialized();
            _connectionString = $"Data Source={_db.GetDatabasePath()};Pooling=false";

            BuildPeriodFilters();
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ReportsPage Load Error: {ex.Message}");
        }
    }

    // ====================================================================
    // Period Filter Chips
    // ====================================================================

    private void BuildPeriodFilters()
    {
        var periods = new (string Tag, string Label)[]
        {
            ("today", "วันนี้"),
            ("7", "7 วัน"),
            ("30", "30 วัน"),
            ("all", "ทั้งหมด"),
            ("custom", "เลือกวันที่")
        };

        PeriodFilterPanel.Children.Clear();
        foreach (var (tag, label) in periods)
        {
            var isActive = tag == _selectedPeriod;
            var chip = new Border
            {
                Background = new SolidColorBrush(UIHelper.ParseColor(isActive ? "#4A148C" : "#E8E8E8")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16, 8, 16, 8),
                Tag = tag
            };
            chip.Child = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(UIHelper.ParseColor(isActive ? "#FFFFFF" : "#666666"))
            };
            chip.Tapped += PeriodChip_Tapped;
            PeriodFilterPanel.Children.Add(chip);
        }
    }

    private async void PeriodChip_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border chip && chip.Tag is string tag)
        {
            _selectedPeriod = tag;
            BuildPeriodFilters();

            // Show/hide inline date panel
            CustomDatePanel.Visibility = tag == "custom" ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "custom")
            {
                // Update button labels with current dates
                UpdateCustomDateButtons();
                // Don't auto-load — wait for user to pick dates and press search
                if (_customStartDate.HasValue && _customEndDate.HasValue)
                    UpdateDateRangeDisplay(GetStartDate());
            }
            else
            {
                await LoadDashboardAsync();
            }
        }
    }

    private void UpdateCustomDateButtons()
    {
        TxtPickStart.Text = _customStartDate?.ToString("dd/MM/yyyy") ?? "เริ่มต้น";
        TxtPickEnd.Text = _customEndDate?.ToString("dd/MM/yyyy") ?? "สิ้นสุด";
    }

    private async void BtnPickStart_Click(object sender, RoutedEventArgs e)
    {
        var result = await Dialogs.DatePickerDialog.ShowAsync(this.XamlRoot!, _customStartDate, allowPastDates: true);
        if (result.HasValue)
        {
            _customStartDate = result.Value.Date;
            TxtPickStart.Text = _customStartDate.Value.ToString("dd/MM/yyyy");
            await TryLoadCustomRangeAsync();
        }
    }

    private async void BtnPickEnd_Click(object sender, RoutedEventArgs e)
    {
        var result = await Dialogs.DatePickerDialog.ShowAsync(this.XamlRoot!, _customEndDate ?? DateTime.Today, allowPastDates: true);
        if (result.HasValue)
        {
            _customEndDate = result.Value.Date;
            TxtPickEnd.Text = _customEndDate.Value.ToString("dd/MM/yyyy");
            await TryLoadCustomRangeAsync();
        }
    }

    private async Task TryLoadCustomRangeAsync()
    {
        if (_customStartDate == null || _customEndDate == null)
        {
            UpdateDateRangeDisplay(GetStartDate());
            return;
        }

        if (_customEndDate < _customStartDate)
            (_customStartDate, _customEndDate) = (_customEndDate, _customStartDate);

        UpdateCustomDateButtons();
        await LoadDashboardAsync();
    }

    // ====================================================================
    // Load Dashboard Data
    // ====================================================================

    private async Task LoadDashboardAsync()
    {
        LoadingRing.IsActive = true;
        ContentScrollViewer.Visibility = Visibility.Collapsed;

        try
        {
            var startDate = GetStartDate();
            var endDate = GetEndDate();
            UpdateDateRangeDisplay(startDate);

            var paidResSummary = await QueryPaidReservationSummaryAsync(startDate, endDate);
            var courseResSummary = await QueryCourseReservationSummaryAsync(startDate, endDate);
            var paidUsageSummary = await QueryPaidUsageSummaryAsync(startDate, endDate);
            var courseUsageSummary = await QueryCourseUsageSummaryAsync(startDate, endDate);
            var courseRegRevenue = await QueryCourseRegistrationRevenueAsync(startDate, endDate);
            var courtRanking = await QueryCourtRankingAsync(startDate, endDate);
            var systemStats = await QuerySystemStatsAsync();

            // Cache for PDF
            _lastPaidRes = paidResSummary;
            _lastCourseRes = courseResSummary;
            _lastPaidUsage = paidUsageSummary;
            _lastCourseUsage = courseUsageSummary;
            _lastCourseRegRevenue = courseRegRevenue;
            _lastCourtRanking = courtRanking;
            _lastSystemStats = systemStats;

            // Summary Cards
            var totalRevenue = paidUsageSummary.TotalRevenue + courseRegRevenue.TotalRevenue;
            var totalRevenueCount = paidUsageSummary.Count + courseRegRevenue.Count;
            var totalBookings = paidResSummary.Total + courseResSummary.Total;
            var totalUsage = paidUsageSummary.Count + courseUsageSummary.Count;
            var totalCancelled = paidResSummary.Cancelled + courseResSummary.Cancelled;
            var avgDuration = totalUsage > 0
                ? (paidUsageSummary.TotalDuration + courseUsageSummary.TotalDuration) / totalUsage
                : 0;
            var cancelPercent = totalBookings > 0
                ? (double)totalCancelled / totalBookings * 100
                : 0;

            TxtTotalRevenue.Text = $"฿{totalRevenue:N0}";
            TxtRevenueDetail.Text = $"เช่า ฿{paidUsageSummary.TotalRevenue:N0} / คอร์ส ฿{courseRegRevenue.TotalRevenue:N0}";
            TxtTotalBookings.Text = totalBookings.ToString();
            TxtBookingDetail.Text = $"เช่า {paidResSummary.Total} / คอร์ส {courseResSummary.Total}";
            TxtTotalUsage.Text = $"{totalUsage} ครั้ง";
            TxtUsageDetail.Text = $"เฉลี่ย {avgDuration:0.0} ชม./ครั้ง";
            TxtTotalCancelled.Text = totalCancelled.ToString();
            TxtCancelDetail.Text = $"{cancelPercent:0.0}% ของทั้งหมด";

            // Detail Sections
            DetailSections.Children.Clear();
            DetailSections.Children.Add(BuildRevenueChartCard(paidUsageSummary, courseRegRevenue));
            DetailSections.Children.Add(BuildPaidDetailCard(paidResSummary, paidUsageSummary));
            DetailSections.Children.Add(BuildCourseDetailCard(courseResSummary, courseUsageSummary, courseRegRevenue));
            DetailSections.Children.Add(BuildCourtRankingCard(courtRanking));
            DetailSections.Children.Add(BuildSystemStatsCard(systemStats));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ LoadDashboard Error: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
            ContentScrollViewer.Visibility = Visibility.Visible;
        }
    }

    private string? GetStartDate()
    {
        return _selectedPeriod switch
        {
            "today" => DateTime.Today.ToString("yyyy-MM-dd"),
            "7" => DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"),
            "30" => DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd"),
            "custom" => _customStartDate?.ToString("yyyy-MM-dd"),
            _ => null
        };
    }

    private string? GetEndDate()
    {
        return _selectedPeriod == "custom" ? _customEndDate?.ToString("yyyy-MM-dd") : null;
    }

    private void UpdateDateRangeDisplay(string? startDate)
    {
        var today = DateTime.Today;
        var todayStr = today.ToString("dd/MM/yyyy");

        if (_selectedPeriod == "custom" && _customStartDate.HasValue && _customEndDate.HasValue)
        {
            var days = (_customEndDate.Value - _customStartDate.Value).Days + 1;
            TxtDateRange.Text = $"📅 {_customStartDate.Value:dd/MM/yyyy} — {_customEndDate.Value:dd/MM/yyyy} ({days} วัน)";
            return;
        }

        TxtDateRange.Text = _selectedPeriod switch
        {
            "today" => $"📅 {todayStr} (วันนี้)",
            "7" => $"📅 {today.AddDays(-7):dd/MM/yyyy} — {todayStr} (7 วัน)",
            "30" => $"📅 {today.AddDays(-30):dd/MM/yyyy} — {todayStr} (30 วัน)",
            _ => "📅 ข้อมูลทั้งหมด (ไม่จำกัดช่วงเวลา)"
        };
    }

    // ====================================================================
    // SQL Aggregate Queries
    // ====================================================================

    private record ReservationSummary(int Total, int Booked, int InUse, int Completed, int Cancelled);
    private record UsageSummary(int Count, int TotalRevenue, double TotalDuration, double AvgDuration, double AvgPrice);
    private record CourseRegistrationRevenue(int Count, int TotalRevenue);
    private record CourtUsageRank(string CourtId, int UsageCount);
    private record SystemStats(int TotalCourts, int ActiveCourts, int Trainers, int Trainees, int Courses, int Registrations);

    private async Task<ReservationSummary> QueryPaidReservationSummaryAsync(string? startDate, string? endDate)
    {
        var sql = @"SELECT 
            COUNT(*),
            COALESCE(SUM(CASE WHEN p_status='booked' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN p_status='in_use' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN p_status='completed' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN p_status='cancelled' THEN 1 ELSE 0 END), 0)
            FROM PaidCourtReservation";

        if (startDate != null) sql += " WHERE p_reserve_date >= @start";
        if (endDate != null) sql += (startDate != null ? " AND" : " WHERE") + " p_reserve_date <= @end";

        return await ExecuteQueryAsync(sql, startDate, endDate, r =>
            new ReservationSummary(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4)));
    }

    private async Task<ReservationSummary> QueryCourseReservationSummaryAsync(string? startDate, string? endDate)
    {
        var sql = @"SELECT 
            COUNT(*),
            COALESCE(SUM(CASE WHEN c_status='booked' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN c_status='in_use' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN c_status='completed' THEN 1 ELSE 0 END), 0),
            COALESCE(SUM(CASE WHEN c_status='cancelled' THEN 1 ELSE 0 END), 0)
            FROM CourseCourtReservation";

        if (startDate != null) sql += " WHERE c_reserve_date >= @start";
        if (endDate != null) sql += (startDate != null ? " AND" : " WHERE") + " c_reserve_date <= @end";

        return await ExecuteQueryAsync(sql, startDate, endDate, r =>
            new ReservationSummary(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4)));
    }

    private async Task<UsageSummary> QueryPaidUsageSummaryAsync(string? startDate, string? endDate)
    {
        var sql = @"SELECT 
            COUNT(*),
            COALESCE(SUM(p_log_price), 0),
            COALESCE(SUM(p_log_duration), 0),
            COALESCE(AVG(p_log_duration), 0),
            COALESCE(AVG(p_log_price), 0)
            FROM PaidCourtUseLog WHERE p_log_status = 'completed'";

        if (startDate != null) sql += " AND date(p_checkin_time) >= @start";
        if (endDate != null) sql += " AND date(p_checkin_time) <= @end";

        return await ExecuteQueryAsync(sql, startDate, endDate, r =>
            new UsageSummary(r.GetInt32(0), r.GetInt32(1), r.GetDouble(2), r.GetDouble(3), r.GetDouble(4)));
    }

    private async Task<UsageSummary> QueryCourseUsageSummaryAsync(string? startDate, string? endDate)
    {
        var sql = @"SELECT 
            COUNT(*),
            0,
            COALESCE(SUM(c_log_duration), 0),
            COALESCE(AVG(c_log_duration), 0),
            0
            FROM CourseCourtUseLog WHERE c_log_status = 'completed'";

        if (startDate != null) sql += " AND date(c_checkin_time) >= @start";
        if (endDate != null) sql += " AND date(c_checkin_time) <= @end";

        return await ExecuteQueryAsync(sql, startDate, endDate, r =>
            new UsageSummary(r.GetInt32(0), r.GetInt32(1), r.GetDouble(2), r.GetDouble(3), r.GetDouble(4)));
    }

    private async Task<CourseRegistrationRevenue> QueryCourseRegistrationRevenueAsync(string? startDate, string? endDate)
    {
        var sql = @"SELECT
            COUNT(*),
            COALESCE(SUM(c.class_rate), 0)
            FROM ClassRegisRecord r
            JOIN Course c ON r.class_id = c.class_id AND r.trainer_id = c.trainer_id";

        if (startDate != null) sql += " WHERE date(r.regis_date) >= @start";
        if (endDate != null) sql += (startDate != null ? " AND" : " WHERE") + " date(r.regis_date) <= @end";

        return await ExecuteQueryAsync(sql, startDate, endDate, r =>
            new CourseRegistrationRevenue(r.GetInt32(0), r.GetInt32(1)));
    }

    private async Task<List<CourtUsageRank>> QueryCourtRankingAsync(string? startDate, string? endDate)
    {
        var dateFilter1 = startDate != null ? " AND date(l.p_checkin_time) >= @start" : "";
        var dateFilter2 = startDate != null ? " AND date(l.c_checkin_time) >= @start" : "";
        if (endDate != null)
        {
            dateFilter1 += " AND date(l.p_checkin_time) <= @end";
            dateFilter2 += " AND date(l.c_checkin_time) <= @end";
        }

        var sql = $@"SELECT court_id, COUNT(*) as cnt FROM (
            SELECT r.court_id FROM PaidCourtUseLog l
              JOIN PaidCourtReservation r ON l.p_reserve_id = r.p_reserve_id
              WHERE l.p_log_status='completed' AND r.court_id != '00'{dateFilter1}
            UNION ALL
            SELECT r.court_id FROM CourseCourtUseLog l
              JOIN CourseCourtReservation r ON l.c_reserve_id = r.c_reserve_id
              WHERE l.c_log_status='completed' AND r.court_id != '00'{dateFilter2}
        ) GROUP BY court_id ORDER BY cnt DESC";

        var results = new List<CourtUsageRank>();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (startDate != null)
                cmd.Parameters.AddWithValue("@start", startDate);
            if (endDate != null)
                cmd.Parameters.AddWithValue("@end", endDate);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(new CourtUsageRank(reader.GetString(0), reader.GetInt32(1)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CourtRanking Error: {ex.Message}");
        }
        return results;
    }

    private async Task<SystemStats> QuerySystemStatsAsync()
    {
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            async Task<int> Count(string sql)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            var totalCourts = await Count("SELECT COUNT(*) FROM Court WHERE court_id != '00'");
            var activeCourts = await Count("SELECT COUNT(*) FROM Court WHERE court_status = '1' AND court_id != '00'");
            var trainers = await Count("SELECT COUNT(*) FROM Trainer");
            var trainees = await Count("SELECT COUNT(*) FROM Trainee");
            var courses = await Count("SELECT COUNT(*) FROM Course");
            var registrations = await Count("SELECT COUNT(*) FROM ClassRegisRecord");

            return new SystemStats(totalCourts, activeCourts, trainers, trainees, courses, registrations);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SystemStats Error: {ex.Message}");
            return new SystemStats(0, 0, 0, 0, 0, 0);
        }
    }

    private async Task<T> ExecuteQueryAsync<T>(string sql, string? startDate, string? endDate, Func<SqliteDataReader, T> map)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (startDate != null)
            cmd.Parameters.AddWithValue("@start", startDate);
        if (endDate != null)
            cmd.Parameters.AddWithValue("@end", endDate);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return map(reader);
        throw new InvalidOperationException("No results from aggregate query");
    }

    // ====================================================================
    // Revenue Chart Card (SkiaSharp bar chart)
    // ====================================================================

    private Border BuildRevenueChartCard(UsageSummary paidUsage, CourseRegistrationRevenue courseReg)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(UIHelper.ParseColor("#E8E8E8")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 16, 18, 16)
        };

        var stack = new StackPanel { Spacing = 10 };

        // Header
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(new FontIcon
        {
            Glyph = "\uE9D2", FontSize = 18,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#4A148C"))
        });
        header.Children.Add(new TextBlock
        {
            Text = "กราฟรายได้", FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#333333"))
        });
        stack.Children.Add(header);

        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(UIHelper.ParseColor("#F0F0F0")),
            Height = 1, Margin = new Thickness(0, 2, 0, 2)
        });

        var paidRevenue = paidUsage.TotalRevenue;
        var courseRevenue = courseReg.TotalRevenue;
        var totalRevenue = paidRevenue + courseRevenue;

        if (totalRevenue > 0)
        {
            // Create chart image using SkiaSharp
            var chartImage = new Image
            {
                Height = 240,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _ = RenderRevenueChartAsync(chartImage, paidRevenue, courseRevenue);
            stack.Children.Add(chartImage);

            // Legend
            var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, HorizontalAlignment = HorizontalAlignment.Center };

            var paidLegend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            paidLegend.Children.Add(new Border
            {
                Background = new SolidColorBrush(UIHelper.ParseColor("#7B1FA2")),
                Width = 12, Height = 12, CornerRadius = new CornerRadius(3)
            });
            paidLegend.Children.Add(new TextBlock
            {
                Text = $"เช่าสนาม ฿{paidRevenue:N0}", FontSize = 12,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#555555"))
            });
            legend.Children.Add(paidLegend);

            var courseLegend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            courseLegend.Children.Add(new Border
            {
                Background = new SolidColorBrush(UIHelper.ParseColor("#1565C0")),
                Width = 12, Height = 12, CornerRadius = new CornerRadius(3)
            });
            courseLegend.Children.Add(new TextBlock
            {
                Text = $"คอร์สเรียน ฿{courseRevenue:N0}", FontSize = 12,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#555555"))
            });
            legend.Children.Add(courseLegend);

            stack.Children.Add(legend);

            // Percentage text
            var paidPercent = totalRevenue > 0 ? (double)paidRevenue / totalRevenue * 100 : 0;
            var coursePercent = totalRevenue > 0 ? (double)courseRevenue / totalRevenue * 100 : 0;
            stack.Children.Add(new TextBlock
            {
                Text = $"สัดส่วน: เช่าสนาม {paidPercent:0.0}% / คอร์ส {coursePercent:0.0}%",
                FontSize = 12,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#999999")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else
        {
            stack.Children.Add(new TextBlock
            {
                Text = "ยังไม่มีข้อมูลรายได้",
                FontSize = 13,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#999999")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            });
        }

        card.Child = stack;
        return card;
    }

    private static async Task RenderRevenueChartAsync(Image imageControl, int paidRevenue, int courseRevenue)
    {
        try
        {
            var bytes = await Task.Run(() =>
            {
                const int width = 680;
                const int height = 320;
                const int barWidth = 130;
                const int barGap = 50;
                const int chartLeft = 80;
                const int chartRight = 40;
                const int chartBottom = height - 55;
                const int chartTop = 50;
                int chartHeight = chartBottom - chartTop;
                int chartContentWidth = width - chartLeft - chartRight;

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;

                // ── Background: subtle gradient ──
                using var bgPaint = new SKPaint { IsAntialias = true };
                bgPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(0, height),
                    [SKColor.Parse("#FAFAFA"), SKColor.Parse("#F3F0F7")],
                    SKShaderTileMode.Clamp);
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, width, height), 16), bgPaint);

                var maxValue = Math.Max(Math.Max(paidRevenue, courseRevenue), 1);

                // ── Grid lines (dashed) + Y-axis labels ──
                using var gridPaint = new SKPaint
                {
                    Color = SKColor.Parse("#E8E0F0"),
                    StrokeWidth = 1,
                    IsAntialias = true,
                    PathEffect = SKPathEffect.CreateDash([6f, 4f], 0)
                };
                using var yLabelPaint = new SKPaint { Color = SKColor.Parse("#AAAAAA"), IsAntialias = true };
                using var yLabelFont = new SKFont(SKTypeface.Default, 19);

                for (int i = 0; i <= 4; i++)
                {
                    var y = chartBottom - (chartHeight * i / 4f);
                    var value = maxValue * i / 4;
                    canvas.DrawLine(chartLeft, y, width - chartRight, y, gridPaint);
                    var labelText = value >= 1000 ? $"{value / 1000:N0}k" : value.ToString("N0");
                    var labelWidth = yLabelFont.MeasureText(labelText);
                    canvas.DrawText(labelText, chartLeft - labelWidth - 10, y + 6, SKTextAlign.Left, yLabelFont, yLabelPaint);
                }

                // ── Baseline ──
                using var basePaint = new SKPaint { Color = SKColor.Parse("#D0C4E0"), StrokeWidth = 1.5f, IsAntialias = true };
                canvas.DrawLine(chartLeft, chartBottom, width - chartRight, chartBottom, basePaint);

                // ── Bar positions (centered) ──
                int totalBarsWidth = barWidth * 2 + barGap;
                int barsStartX = chartLeft + (chartContentWidth - totalBarsWidth) / 2;
                int bar1X = barsStartX;
                int bar2X = barsStartX + barWidth + barGap;

                // ── Helper: Draw a bar with gradient + shadow + rounded top ──
                void DrawBar(int x, float barH, SKColor colorTop, SKColor colorBot, SKColor shadow)
                {
                    if (barH < 4) barH = 4; // minimum visible height
                    var rect = new SKRect(x, chartBottom - barH, x + barWidth, chartBottom);
                    var rr = new SKRoundRect(rect, 12, 12);

                    // Shadow
                    using var shadowPaint = new SKPaint
                    {
                        Color = shadow,
                        IsAntialias = true,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6)
                    };
                    var shadowRect = new SKRect(rect.Left + 3, rect.Top + 4, rect.Right + 3, rect.Bottom + 2);
                    canvas.DrawRoundRect(new SKRoundRect(shadowRect, 12, 12), shadowPaint);

                    // Gradient fill
                    using var barPaint = new SKPaint { IsAntialias = true };
                    barPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(x, rect.Top), new SKPoint(x, chartBottom),
                        [colorTop, colorBot],
                        SKShaderTileMode.Clamp);
                    canvas.DrawRoundRect(rr, barPaint);

                    // Highlight strip (glossy effect on left)
                    using var glossPaint = new SKPaint { IsAntialias = true };
                    glossPaint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(x, rect.Top), new SKPoint(x + barWidth * 0.35f, rect.Top),
                        [new SKColor(255, 255, 255, 50), new SKColor(255, 255, 255, 0)],
                        SKShaderTileMode.Clamp);
                    canvas.DrawRoundRect(rr, glossPaint);
                }

                // ── Bar 1: Paid ──
                float bar1H = maxValue > 0 ? (float)paidRevenue / maxValue * chartHeight : 0;
                DrawBar(bar1X, bar1H, SKColor.Parse("#9C27B0"), SKColor.Parse("#6A1B9A"), new SKColor(106, 27, 154, 40));

                // ── Bar 2: Course ──
                float bar2H = maxValue > 0 ? (float)courseRevenue / maxValue * chartHeight : 0;
                DrawBar(bar2X, bar2H, SKColor.Parse("#42A5F5"), SKColor.Parse("#1565C0"), new SKColor(21, 101, 192, 40));

                // ── Value badges on top of bars ──
                void DrawValueBadge(int x, float barH, string text, SKColor bgColor)
                {
                    using var badgeFont = new SKFont(SKTypeface.Default, 21) { Embolden = true };
                    float textWidth = badgeFont.MeasureText(text);
                    float badgeW = textWidth + 20;
                    float badgeH = 28;
                    float badgeX = x + (barWidth - badgeW) / 2;
                    float badgeY = chartBottom - Math.Max(barH, 4) - badgeH - 8;
                    if (badgeY < chartTop - 10) badgeY = chartTop - 10;

                    // Badge background
                    var badgeRect = new SKRect(badgeX, badgeY, badgeX + badgeW, badgeY + badgeH);
                    using var badgeBgPaint = new SKPaint { Color = bgColor, IsAntialias = true };
                    canvas.DrawRoundRect(new SKRoundRect(badgeRect, 8, 8), badgeBgPaint);

                    // Badge text
                    using var badgeTextPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                    canvas.DrawText(text, badgeX + 10, badgeY + 20, SKTextAlign.Left, badgeFont, badgeTextPaint);
                }

                DrawValueBadge(bar1X, bar1H, $"{paidRevenue:N0}", SKColor.Parse("#7B1FA2"));
                DrawValueBadge(bar2X, bar2H, $"{courseRevenue:N0}", SKColor.Parse("#1565C0"));

                // ── X-axis labels (below bars) ──
                using var xLabelPaint = new SKPaint { Color = SKColor.Parse("#555555"), IsAntialias = true };
                using var xLabelFont = new SKFont(SKTypeface.Default, 20) { Embolden = true };

                // Paid label with colored dot
                using var paidDotPaint = new SKPaint { Color = SKColor.Parse("#9C27B0"), IsAntialias = true };
                float paidLabelCenterX = bar1X + barWidth / 2f;
                canvas.DrawCircle(paidLabelCenterX - 28, chartBottom + 24, 5, paidDotPaint);
                canvas.DrawText("Paid", paidLabelCenterX - 18, chartBottom + 30, SKTextAlign.Left, xLabelFont, xLabelPaint);

                // Course label with colored dot
                using var courseDotPaint = new SKPaint { Color = SKColor.Parse("#1565C0"), IsAntialias = true };
                float courseLabelCenterX = bar2X + barWidth / 2f;
                canvas.DrawCircle(courseLabelCenterX - 38, chartBottom + 24, 5, courseDotPaint);
                canvas.DrawText("Course", courseLabelCenterX - 28, chartBottom + 30, SKTextAlign.Left, xLabelFont, xLabelPaint);

                // ── Total label at top center ──
                var total = paidRevenue + courseRevenue;
                using var totalFont = new SKFont(SKTypeface.Default, 16);
                using var totalPaint = new SKPaint { Color = SKColor.Parse("#888888"), IsAntialias = true };
                var totalText = $"Total: {total:N0}";
                var totalWidth = totalFont.MeasureText(totalText);
                canvas.DrawText(totalText, (width - totalWidth) / 2, 24, SKTextAlign.Left, totalFont, totalPaint);

                // Encode to PNG
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            });

            // Set image source on UI thread
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            imageControl.Source = bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RenderRevenueChart Error: {ex.Message}");
        }
    }

    // ====================================================================
    // Build Detail Cards
    // ====================================================================

    private Border BuildPaidDetailCard(ReservationSummary res, UsageSummary usage)
    {
        var rows = new List<(string Label, string Value, string? ValueColor)>
        {
            ("จองทั้งหมด", $"{res.Total} รายการ", null),
            ("เสร็จสิ้น (completed)", $"{res.Completed}", "#2E7D32"),
            ("กำลังใช้งาน (in_use)", $"{res.InUse}", "#E65100"),
            ("รอใช้งาน (booked)", $"{res.Booked}", "#1565C0"),
            ("ยกเลิก (cancelled)", $"{res.Cancelled}", "#C62828"),
        };

        var revenueRows = new List<(string Label, string Value, string? ValueColor)>
        {
            ("รายได้รวม", $"฿{usage.TotalRevenue:N0}", "#D32F2F"),
            ("เวลาใช้เฉลี่ย/ครั้ง", $"{usage.AvgDuration:0.0} ชม.", null),
            ("รายได้เฉลี่ย/ครั้ง", usage.Count > 0 ? $"฿{usage.AvgPrice:N0}" : "—", "#D32F2F"),
        };

        return BuildSectionCard("\uE8CB", "เช่าสนาม (Paid)", "#4A148C", rows, revenueRows);
    }

    private Border BuildCourseDetailCard(ReservationSummary res, UsageSummary usage, CourseRegistrationRevenue regRevenue)
    {
        var rows = new List<(string Label, string Value, string? ValueColor)>
        {
            ("จองทั้งหมด", $"{res.Total} รายการ", null),
            ("เสร็จสิ้น (completed)", $"{res.Completed}", "#2E7D32"),
            ("กำลังใช้งาน (in_use)", $"{res.InUse}", "#E65100"),
            ("รอใช้งาน (booked)", $"{res.Booked}", "#1565C0"),
            ("ยกเลิก (cancelled)", $"{res.Cancelled}", "#C62828"),
        };

        var extraRows = new List<(string Label, string Value, string? ValueColor)>
        {
            ("เวลาใช้เฉลี่ย/ครั้ง", usage.Count > 0 ? $"{usage.AvgDuration:0.0} ชม." : "—", null),
            ("สมัครคอร์ส", $"{regRevenue.Count} รายการ", "#1565C0"),
            ("รายได้จากค่าสมัคร", $"฿{regRevenue.TotalRevenue:N0}", "#D32F2F"),
            ("ค่าสมัครเฉลี่ย/คน", regRevenue.Count > 0 ? $"฿{regRevenue.TotalRevenue / regRevenue.Count:N0}" : "—", "#D32F2F"),
        };

        return BuildSectionCard("\uE82D", "คอร์สเรียน (Course)", "#7B1FA2", rows, extraRows);
    }

    private Border BuildSectionCard(string glyph, string title, string accentColor,
        List<(string Label, string Value, string? ValueColor)> rows,
        List<(string Label, string Value, string? ValueColor)>? extraRows = null)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(UIHelper.ParseColor("#E8E8E8")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 16, 18, 16)
        };

        var stack = new StackPanel { Spacing = 10 };

        // Header
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(new FontIcon
        {
            Glyph = glyph, FontSize = 18,
            Foreground = new SolidColorBrush(UIHelper.ParseColor(accentColor))
        });
        header.Children.Add(new TextBlock
        {
            Text = title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#333333"))
        });
        stack.Children.Add(header);

        // Separator
        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(UIHelper.ParseColor("#F0F0F0")),
            Height = 1, Margin = new Thickness(0, 2, 0, 2)
        });

        // Stat rows
        foreach (var (label, value, color) in rows)
            stack.Children.Add(BuildStatRow(label, value, color));

        // Extra separator + rows
        if (extraRows is { Count: > 0 })
        {
            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(UIHelper.ParseColor("#F0F0F0")),
                Height = 1, Margin = new Thickness(0, 4, 0, 4)
            });
            foreach (var (label, value, color) in extraRows)
                stack.Children.Add(BuildStatRow(label, value, color));
        }

        card.Child = stack;
        return card;
    }

    private static Grid BuildStatRow(string label, string value, string? valueColor)
    {
        var grid = new Grid { Padding = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelTxt = new TextBlock
        {
            Text = label, FontSize = 13,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#666666"))
        };
        Grid.SetColumn(labelTxt, 0);
        grid.Children.Add(labelTxt);

        var valueTxt = new TextBlock
        {
            Text = value, FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(UIHelper.ParseColor(valueColor ?? "#333333"))
        };
        Grid.SetColumn(valueTxt, 1);
        grid.Children.Add(valueTxt);

        return grid;
    }

    // ====================================================================
    // Court Ranking Card (with bar chart)
    // ====================================================================

    private Border BuildCourtRankingCard(List<CourtUsageRank> ranking)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(UIHelper.ParseColor("#E8E8E8")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 16, 18, 16)
        };

        var stack = new StackPanel { Spacing = 10 };

        // Header
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(new FontIcon
        {
            Glyph = "\uE707", FontSize = 18,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#00695C"))
        });
        header.Children.Add(new TextBlock
        {
            Text = "สถิติการใช้สนาม", FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(UIHelper.ParseColor("#333333"))
        });
        stack.Children.Add(header);

        stack.Children.Add(new Border
        {
            Background = new SolidColorBrush(UIHelper.ParseColor("#F0F0F0")),
            Height = 1, Margin = new Thickness(0, 2, 0, 2)
        });

        if (ranking.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "ยังไม่มีข้อมูลการใช้สนาม", FontSize = 13,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#999999")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });
        }
        else
        {
            var maxCount = ranking.Max(r => r.UsageCount);
            var barColors = new[] { "#4A148C", "#7B1FA2", "#9C27B0", "#BA68C8", "#CE93D8" };

            for (int i = 0; i < ranking.Count; i++)
            {
                var item = ranking[i];
                var barColor = barColors[Math.Min(i, barColors.Length - 1)];
                var barPercent = maxCount > 0 ? (double)item.UsageCount / maxCount : 0;

                var row = new StackPanel { Spacing = 4 };

                // Label row
                var labelGrid = new Grid();
                labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var courtLabel = new TextBlock
                {
                    Text = $"สนาม {item.CourtId}", FontSize = 13,
                    FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                    Foreground = new SolidColorBrush(UIHelper.ParseColor("#333333"))
                };
                Grid.SetColumn(courtLabel, 0);
                labelGrid.Children.Add(courtLabel);

                var countLabel = new TextBlock
                {
                    Text = $"{item.UsageCount} ครั้ง", FontSize = 13,
                    Foreground = new SolidColorBrush(UIHelper.ParseColor("#666666"))
                };
                Grid.SetColumn(countLabel, 1);
                labelGrid.Children.Add(countLabel);
                row.Children.Add(labelGrid);

                // Bar
                var barBg = new Border
                {
                    Background = new SolidColorBrush(UIHelper.ParseColor("#F5F5F5")),
                    CornerRadius = new CornerRadius(4),
                    Height = 10
                };
                var barFill = new Border
                {
                    Background = new SolidColorBrush(UIHelper.ParseColor(barColor)),
                    CornerRadius = new CornerRadius(4),
                    Height = 10,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                // Use star width for proportional bar
                var barGrid = new Grid { Height = 10 };
                barGrid.Children.Add(barBg);
                barGrid.Children.Add(barFill);

                // Set width after layout
                barFill.Loaded += (s, e) =>
                {
                    if (barBg.ActualWidth > 0)
                        barFill.Width = barBg.ActualWidth * barPercent;
                    else
                        barFill.Width = 100 * barPercent;
                };

                row.Children.Add(barGrid);
                stack.Children.Add(row);
            }
        }

        card.Child = stack;
        return card;
    }

    // ====================================================================
    // System Stats Card + Backup
    // ====================================================================

    private Border BuildSystemStatsCard(SystemStats stats)
    {
        var dbSizeText = BackupService.GetDatabaseSizeText(_db.GetDatabasePath());

        var rows = new List<(string Label, string Value, string? ValueColor)>
        {
            ("🎾 สนาม", $"{stats.TotalCourts} (เปิดใช้ {stats.ActiveCourts})", null),
            ("👤 ผู้ฝึกสอน", $"{stats.Trainers} คน", null),
            ("👥 ผู้เรียน", $"{stats.Trainees} คน", null),
            ("📚 คอร์ส", $"{stats.Courses} คอร์ส", null),
            ("📝 สมัครคอร์ส", $"{stats.Registrations} รายการ", null),
            ("💾 ขนาดฐานข้อมูล", dbSizeText, "#4A148C"),
        };

        var card = BuildSectionCard("\uE9D9", "ข้อมูลระบบ", "#37474F", rows);

        // เพิ่มปุ่ม Backup/Import ใต้ stat rows
        if (card.Child is StackPanel stack)
        {
            // Separator
            stack.Children.Add(new Border
            {
                Background = new SolidColorBrush(UIHelper.ParseColor("#F0F0F0")),
                Height = 1, Margin = new Thickness(0, 6, 0, 6)
            });

            // Buttons row
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Export button
            var exportBtn = new Button
            {
                Background = new SolidColorBrush(UIHelper.ParseColor("#4A148C")),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(8),
                Height = 40,
                Padding = new Thickness(16, 0, 16, 0),
                BorderThickness = new Thickness(0)
            };
            var exportContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            exportContent.Children.Add(new FontIcon
            {
                Glyph = "\uE78C", FontSize = 14,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
            });
            exportContent.Children.Add(new TextBlock
            {
                Text = "สำรองข้อมูล", FontSize = 13, VerticalAlignment = VerticalAlignment.Center
            });
            exportBtn.Content = exportContent;
            exportBtn.Click += BtnExportBackup_Click;
            btnRow.Children.Add(exportBtn);

            // Import button
            var importBtn = new Button
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#4A148C")),
                CornerRadius = new CornerRadius(8),
                Height = 40,
                Padding = new Thickness(16, 0, 16, 0),
                BorderBrush = new SolidColorBrush(UIHelper.ParseColor("#4A148C")),
                BorderThickness = new Thickness(1)
            };
            var importContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            importContent.Children.Add(new FontIcon
            {
                Glyph = "\uE896", FontSize = 14,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#4A148C"))
            });
            importContent.Children.Add(new TextBlock
            {
                Text = "นำเข้าข้อมูล", FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(UIHelper.ParseColor("#4A148C"))
            });
            importBtn.Content = importContent;
            importBtn.Click += BtnImportBackup_Click;
            btnRow.Children.Add(importBtn);

            stack.Children.Add(btnRow);
        }

        return card;
    }

    // ====================================================================
    // Backup Export
    // ====================================================================

    private async void BtnExportBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn) btn.IsEnabled = false;

            var dbPath = _db.GetDatabasePath();
            var backupPath = await BackupService.ExportBackupAsync(dbPath);

            _notify?.ShowSuccess("สำรองข้อมูลเรียบร้อย");

            // เปิด Share Sheet ให้ user ส่งต่อได้
            BackupService.ShareFileOnAndroid(backupPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Backup Export Error: {ex.Message}");
            _notify?.ShowError($"ไม่สามารถสำรองข้อมูลได้: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn) btn.IsEnabled = true;
        }
    }

    // ====================================================================
    // Backup Import
    // ====================================================================

    private async void BtnImportBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn) btn.IsEnabled = false;

            // 1. เลือกไฟล์
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".db");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;

#if !__ANDROID__
            if (App.MainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
#endif

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            // 2. Copy ไฟล์ที่เลือกไป temp เพื่อ validate
            var tempPath = Path.Combine(Path.GetTempPath(), $"import_{Guid.NewGuid()}.db");
            using (var sourceStream = await file.OpenStreamForReadAsync())
            using (var destStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            // 3. Validate
            var (isValid, error) = await BackupService.ValidateBackupFileAsync(tempPath);
            if (!isValid)
            {
                _notify?.ShowError(error ?? "ไฟล์ไม่ถูกต้อง");
                File.Delete(tempPath);
                return;
            }

            // 4. Confirm
            var confirmed = _notify != null
                ? await _notify.ShowConfirmAsync(
                    "⚠️ ยืนยันนำเข้าข้อมูล",
                    "ข้อมูลปัจจุบันจะถูกแทนที่ด้วยไฟล์ที่เลือก\n\n" +
                    "📦 ระบบจะสำรองข้อมูลเดิมไว้ใน Downloads อัตโนมัติ\n\n" +
                    "ต้องการดำเนินการ?",
                    this.XamlRoot!)
                : false;

            if (!confirmed)
            {
                File.Delete(tempPath);
                return;
            }

            // 5. Import (auto-backup ก่อน)
            var dbPath = _db.GetDatabasePath();
            var autoBackupPath = await BackupService.ImportBackupAsync(tempPath, dbPath);

            // 6. Re-initialize database
            await _db.InitializeAsync();

            // 7. Cleanup temp
            File.Delete(tempPath);

            _notify?.ShowSuccess("นำเข้าข้อมูลเรียบร้อย — โหลดข้อมูลใหม่...");

            // 8. Reload dashboard
            _connectionString = $"Data Source={_db.GetDatabasePath()};Pooling=false";
            await LoadDashboardAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Backup Import Error: {ex.Message}");
            _notify?.ShowError($"ไม่สามารถนำเข้าข้อมูลได้: {ex.Message}");
        }
        finally
        {
            if (sender is Button btn) btn.IsEnabled = true;
        }
    }

    // ====================================================================
    // Export PDF
    // ====================================================================

    private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_lastPaidRes == null || _lastCourseRes == null ||
            _lastPaidUsage == null || _lastCourseUsage == null ||
            _lastCourtRanking == null || _lastSystemStats == null ||
            _lastCourseRegRevenue == null)
        {
            _notify?.ShowWarning("กรุณารอให้โหลดข้อมูลเสร็จก่อน");
            return;
        }

        try
        {
            BtnExportPdf.IsEnabled = false;

            var periodLabel = _selectedPeriod switch
            {
                "today" => "วันนี้",
                "7" => "ย้อนหลัง 7 วัน",
                "30" => "ย้อนหลัง 30 วัน",
                "custom" when _customStartDate.HasValue && _customEndDate.HasValue
                    => $"{_customStartDate.Value:dd/MM/yyyy} — {_customEndDate.Value:dd/MM/yyyy}",
                _ => "ทั้งหมด"
            };

            var pdfTotalRevenue = _lastPaidUsage.TotalRevenue + _lastCourseRegRevenue.TotalRevenue;
            var pdfTotalRevenueCount = _lastPaidUsage.Count + _lastCourseRegRevenue.Count;
            var totalBookings = _lastPaidRes.Total + _lastCourseRes.Total;
            var totalUsage = _lastPaidUsage.Count + _lastCourseUsage.Count;
            var totalCancelled = _lastPaidRes.Cancelled + _lastCourseRes.Cancelled;
            var avgDuration = totalUsage > 0
                ? (_lastPaidUsage.TotalDuration + _lastCourseUsage.TotalDuration) / totalUsage
                : 0;
            var cancelPercent = totalBookings > 0
                ? (double)totalCancelled / totalBookings * 100
                : 0;

            var reportData = new PdfExportService.ReportData(
                Summary: new PdfExportService.SummaryData(
                    periodLabel,
                    pdfTotalRevenue, pdfTotalRevenueCount,
                    totalBookings, _lastPaidRes.Total, _lastCourseRes.Total,
                    totalUsage, avgDuration,
                    totalCancelled, cancelPercent),
                PaidDetail: new PdfExportService.ReservationDetail(
                    _lastPaidRes.Total, _lastPaidRes.Booked, _lastPaidRes.InUse,
                    _lastPaidRes.Completed, _lastPaidRes.Cancelled,
                    _lastPaidUsage.TotalRevenue, _lastPaidUsage.AvgDuration, _lastPaidUsage.AvgPrice),
                CourseDetail: new PdfExportService.ReservationDetail(
                    _lastCourseRes.Total, _lastCourseRes.Booked, _lastCourseRes.InUse,
                    _lastCourseRes.Completed, _lastCourseRes.Cancelled,
                    _lastCourseRegRevenue.TotalRevenue, _lastCourseUsage.AvgDuration,
                    _lastCourseRegRevenue.Count > 0 ? (double)_lastCourseRegRevenue.TotalRevenue / _lastCourseRegRevenue.Count : 0),
                CourtRanking: _lastCourtRanking
                    .Select(r => new PdfExportService.CourtRank(r.CourtId, r.UsageCount))
                    .ToList(),
                System: new PdfExportService.SystemInfo(
                    _lastSystemStats.TotalCourts, _lastSystemStats.ActiveCourts,
                    _lastSystemStats.Trainers, _lastSystemStats.Trainees,
                    _lastSystemStats.Courses, _lastSystemStats.Registrations));

            var pdfService = new PdfExportService();
            var filePath = await pdfService.GeneratePdfAsync(reportData);

            _notify?.ShowSuccess($"บันทึก PDF เรียบร้อย");
            System.Diagnostics.Debug.WriteLine($"✅ PDF saved: {filePath}");

            // Try open PDF on Android
            await TryOpenPdfAsync(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ PDF Export Error: {ex.Message}");
            _notify?.ShowError($"ไม่สามารถสร้าง PDF ได้: {ex.Message}");
        }
        finally
        {
            BtnExportPdf.IsEnabled = true;
        }
    }

    private static async Task TryOpenPdfAsync(string filePath)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var file = new Java.IO.File(filePath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                context.PackageName + ".fileprovider",
                file);

            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(uri, "application/pdf");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Cannot open PDF: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }
}
