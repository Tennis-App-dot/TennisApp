using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Dialogs;

/// <summary>
/// Dialog สำหรับเพิ่ม/แก้ไข การจองสนาม
/// รองรับทั้งการจองแบบเช่า (Paid) และการจองสำหรับคอร์ส (Course)
/// </summary>
public sealed partial class ReservationFormDialog : ContentDialog
{
    // ========================================================================
    // Properties
    // ========================================================================

    /// <summary>
    /// วันที่ต้องการใช้สนาม (สำหรับ DatePicker binding)
    /// </summary>
    public DateTimeOffset ReserveDateOffset { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// เวลาที่เข้าใช้สนาม
    /// </summary>
    public TimeSpan ReserveTime { get; set; } = new TimeSpan(8, 0, 0); // Default 08:00

    /// <summary>
    /// ระยะเวลาใช้สนาม (ชั่วโมง)
    /// </summary>
    public double Duration { get; set; } = 1.0; // Default 1 hour

    /// <summary>
    /// รหัสสนามที่เลือก
    /// </summary>
    public string SelectedCourtId { get; set; } = string.Empty;

    /// <summary>
    /// ประเภทการจอง ("Paid" หรือ "Course")
    /// </summary>
    public string ReservationType { get; set; } = "Paid";

    /// <summary>
    /// รหัสคอร์สที่เลือก (สำหรับการจองแบบคอร์ส)
    /// </summary>
    public string SelectedCourseId { get; set; } = string.Empty;

    /// <summary>
    /// ชื่อผู้จอง
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// เบอร์โทรศัพท์
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// แสดงเวลาสิ้นสุดที่คาดการณ์ (อ่านอย่างเดียว)
    /// </summary>
    public string EstimatedEndTime
    {
        get
        {
            var endTime = ReserveTime.Add(TimeSpan.FromHours(Duration));
            return endTime.ToString(@"hh\:mm");
        }
    }

    /// <summary>
    /// ผลลัพธ์การจอง (Paid หรือ Course)
    /// </summary>
    public object? Result { get; private set; }

    /// <summary>
    /// บันทึกสำเร็จหรือไม่
    /// </summary>
    public bool WasSaved { get; private set; }

    // ========================================================================
    // Internal Data
    // ========================================================================

    private readonly List<CourtItem> _availableCourts;
    private readonly List<CourseItem> _availableCourses;
    private readonly bool _isEditMode;

    // ========================================================================
    // Constructor (Add Mode)
    // ========================================================================

    /// <summary>
    /// สร้าง Dialog สำหรับเพิ่มการจองใหม่
    /// </summary>
    public ReservationFormDialog(List<CourtItem> courts, List<CourseItem> courses)
    {
        this.InitializeComponent();

        _availableCourts = courts ?? new List<CourtItem>();
        _availableCourses = courses ?? new List<CourseItem>();
        _isEditMode = false;

        InitializeComboBoxes();
        SetDefaultValues();

        System.Diagnostics.Debug.WriteLine("📅 ReservationFormDialog - Add Mode");
    }

    // ========================================================================
    // Constructor (Edit Mode)
    // ========================================================================

    /// <summary>
    /// สร้าง Dialog สำหรับแก้ไขการจองแบบเช่า
    /// </summary>
    public ReservationFormDialog(PaidCourtReservationItem reservation, List<CourtItem> courts)
    {
        this.InitializeComponent();

        _availableCourts = courts ?? new List<CourtItem>();
        _availableCourses = new List<CourseItem>();
        _isEditMode = true;

        InitializeComboBoxes();
        LoadPaidReservationData(reservation);

        System.Diagnostics.Debug.WriteLine($"📅 ReservationFormDialog - Edit Mode (Paid): {reservation.ReserveId}");
    }

    /// <summary>
    /// สร้าง Dialog สำหรับแก้ไขการจองแบบคอร์ส
    /// </summary>
    public ReservationFormDialog(CourseCourtReservationItem reservation, List<CourtItem> courts, List<CourseItem> courses)
    {
        this.InitializeComponent();

        _availableCourts = courts ?? new List<CourtItem>();
        _availableCourses = courses ?? new List<CourseItem>();
        _isEditMode = true;

        InitializeComboBoxes();
        LoadCourseReservationData(reservation);

        System.Diagnostics.Debug.WriteLine($"📅 ReservationFormDialog - Edit Mode (Course): {reservation.ReserveId}");
    }

    // ========================================================================
    // Initialization
    // ========================================================================

    /// <summary>
    /// กำหนดค่าเริ่มต้น ComboBoxes
    /// </summary>
    private void InitializeComboBoxes()
    {
        // ComboBox สนาม
        CourtComboBox.Items.Clear();
        foreach (var court in _availableCourts)
        {
            var item = new ComboBoxItem
            {
                Content = court.DisplayName,
                Tag = court.CourtID
            };
            CourtComboBox.Items.Add(item);
        }

        // ComboBox คอร์ส
        CourseComboBox.Items.Clear();
        foreach (var course in _availableCourses)
        {
            var item = new ComboBoxItem
            {
                Content = course.ClassTitle,
                Tag = course.ClassId
            };
            CourseComboBox.Items.Add(item);
        }

        // ตั้งค่า default selections
        if (CourtComboBox.Items.Count > 0)
            CourtComboBox.SelectedIndex = 0;

        if (CourseComboBox.Items.Count > 0)
            CourseComboBox.SelectedIndex = 0;

        // ตั้งค่า Reservation Type
        ReservationTypeComboBox.SelectedIndex = 0; // Default: Paid

        // ตั้งค่า Duration
        DurationComboBox.SelectedIndex = 0; // Default: 1.0 hr
    }

    /// <summary>
    /// กำหนดค่าเริ่มต้นสำหรับ Add Mode
    /// </summary>
    private void SetDefaultValues()
    {
        ReserveDateOffset = DateTimeOffset.Now;
        ReserveTime = new TimeSpan(8, 0, 0);
        Duration = 1.0;
        ReservationType = "Paid";
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;

        // Update UI
        UpdateEstimatedEndTime();
    }

    /// <summary>
    /// โหลดข้อมูลจากการจองแบบเช่า (Edit Mode)
    /// </summary>
    private void LoadPaidReservationData(PaidCourtReservationItem reservation)
    {
        ReserveDateOffset = new DateTimeOffset(reservation.ReserveDate);
        ReserveTime = reservation.ReserveTime;
        Duration = reservation.Duration;
        SelectedCourtId = reservation.CourtId;
        ReservationType = "Paid";
        CustomerName = reservation.ReserveName;
        CustomerPhone = reservation.ReservePhone;

        // Select court in ComboBox
        var courtItem = CourtComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == SelectedCourtId);
        if (courtItem != null)
            CourtComboBox.SelectedItem = courtItem;

        // Select duration
        var durationItem = DurationComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == Duration.ToString("F1"));
        if (durationItem != null)
            DurationComboBox.SelectedItem = durationItem;

        // Select reservation type
        ReservationTypeComboBox.SelectedIndex = 0; // Paid

        UpdateEstimatedEndTime();
    }

    /// <summary>
    /// โหลดข้อมูลจากการจองแบบคอร์ส (Edit Mode)
    /// </summary>
    private void LoadCourseReservationData(CourseCourtReservationItem reservation)
    {
        ReserveDateOffset = new DateTimeOffset(reservation.ReserveDate);
        ReserveTime = reservation.ReserveTime;
        Duration = reservation.Duration;
        SelectedCourtId = reservation.CourtId;
        SelectedCourseId = reservation.ClassId;
        ReservationType = "Course";
        CustomerName = reservation.ReserveName;
        CustomerPhone = reservation.ReservePhone;

        // Select court in ComboBox
        var courtItem = CourtComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == SelectedCourtId);
        if (courtItem != null)
            CourtComboBox.SelectedItem = courtItem;

        // Select duration
        var durationItem = DurationComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == Duration.ToString("F1"));
        if (durationItem != null)
            DurationComboBox.SelectedItem = durationItem;

        // Select course
        var courseItem = CourseComboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == SelectedCourseId);
        if (courseItem != null)
            CourseComboBox.SelectedItem = courseItem;

        // Select reservation type
        ReservationTypeComboBox.SelectedIndex = 1; // Course

        // Show course panel
        CourseSelectionPanel.Visibility = Visibility.Visible;

        UpdateEstimatedEndTime();
    }

    // ========================================================================
    // Event Handlers
    // ========================================================================

    /// <summary>
    /// เมื่อเปลี่ยนประเภทการจอง (Paid/Course)
    /// </summary>
    private void ReservationTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReservationTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            ReservationType = selectedItem.Tag?.ToString() ?? "Paid";

            // แสดง/ซ่อน Course Selection
            if (ReservationType == "Course")
            {
                CourseSelectionPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CourseSelectionPanel.Visibility = Visibility.Collapsed;
            }

            System.Diagnostics.Debug.WriteLine($"🔄 Reservation Type changed: {ReservationType}");
        }
    }

    /// <summary>
    /// เมื่อเปลี่ยนสนาม
    /// </summary>
    private void CourtComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CourtComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedCourtId = selectedItem.Tag?.ToString() ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"🎾 Court changed: {SelectedCourtId}");
        }
    }

    /// <summary>
    /// เมื่อเปลี่ยนคอร์ส
    /// </summary>
    private void CourseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CourseComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedCourseId = selectedItem.Tag?.ToString() ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"📚 Course changed: {SelectedCourseId}");

            // Auto-fill customer name and phone from course (if needed)
            // TODO: Implement auto-fill logic from Trainer data
        }
    }

    /// <summary>
    /// เมื่อเปลี่ยนระยะเวลา
    /// </summary>
    private void DurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DurationComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            if (double.TryParse(selectedItem.Tag?.ToString(), out var duration))
            {
                Duration = duration;
                UpdateEstimatedEndTime();
                System.Diagnostics.Debug.WriteLine($"⏱️ Duration changed: {Duration} hrs");
            }
        }
    }

    /// <summary>
    /// อัปเดตเวลาสิ้นสุดที่คาดการณ์
    /// </summary>
    private void UpdateEstimatedEndTime()
    {
        if (EstimatedEndTimeTextBox != null)
        {
            EstimatedEndTimeTextBox.Text = EstimatedEndTime;
        }
    }

    // ========================================================================
    // Button Handlers
    // ========================================================================

    /// <summary>
    /// บันทึกการจอง
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("💾 SaveButton_Click - เริ่มบันทึกการจอง");

        // Validate input
        if (!ValidateInput())
        {
            System.Diagnostics.Debug.WriteLine("❌ Validation failed");
            return;
        }

        try
        {
            // สร้าง Reservation Object ตามประเภท
            if (ReservationType == "Paid")
            {
                Result = CreatePaidReservation();
                System.Diagnostics.Debug.WriteLine($"✅ Created PaidCourtReservation");
            }
            else // Course
            {
                Result = CreateCourseReservation();
                System.Diagnostics.Debug.WriteLine($"✅ Created CourseCourtReservation");
            }

            WasSaved = true;
            Hide();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error saving reservation: {ex.Message}");
        }
    }

    /// <summary>
    /// ยกเลิก
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🚫 CancelButton_Click");
        WasSaved = false;
        Hide();
    }

    // ========================================================================
    // Validation
    // ========================================================================

    /// <summary>
    /// ตรวจสอบข้อมูลที่กรอก
    /// </summary>
    private bool ValidateInput()
    {
        // ตรวจสอบสนาม
        if (string.IsNullOrEmpty(SelectedCourtId))
        {
            ShowErrorMessage("กรุณาเลือกสนาม");
            return false;
        }

        // ตรวจสอบชื่อผู้จอง
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            ShowErrorMessage("กรุณากรอกชื่อผู้จอง");
            return false;
        }

        // ตรวจสอบคอร์ส (ถ้าเป็นการจองแบบคอร์ส)
        if (ReservationType == "Course" && string.IsNullOrEmpty(SelectedCourseId))
        {
            ShowErrorMessage("กรุณาเลือกคอร์สเรียน");
            return false;
        }

        // ตรวจสอบเบอร์โทร (ถ้ากรอก)
        if (!string.IsNullOrWhiteSpace(CustomerPhone))
        {
            if (CustomerPhone.Length != 10 || !CustomerPhone.All(char.IsDigit))
            {
                ShowErrorMessage("เบอร์โทรศัพท์ไม่ถูกต้อง (ต้องเป็นตัวเลข 10 หลัก)");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// แสดงข้อความ error
    /// </summary>
    private async void ShowErrorMessage(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "ข้อมูลไม่ครบถ้วน",
            Content = message,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    // ========================================================================
    // Create Reservation Objects
    // ========================================================================

    /// <summary>
    /// สร้าง PaidCourtReservationItem
    /// </summary>
    private PaidCourtReservationItem CreatePaidReservation()
    {
        var reserveDate = ReserveDateOffset.Date;
        var requestDate = DateTime.Now;

        // สร้าง Reserve ID (รูปแบบ YYYYMMDDXX)
        var reserveId = ReservationIdGenerator.GeneratePaidReservationId(requestDate);

        return new PaidCourtReservationItem
        {
            ReserveId = reserveId,
            CourtId = SelectedCourtId,
            RequestDate = requestDate,
            ReserveDate = reserveDate,
            ReserveTime = ReserveTime,
            Duration = Duration,
            ReserveName = CustomerName.Trim(),
            ReservePhone = CustomerPhone.Trim()
        };
    }

    /// <summary>
    /// สร้าง CourseCourtReservationItem
    /// </summary>
    private CourseCourtReservationItem CreateCourseReservation()
    {
        var reserveDate = ReserveDateOffset.Date;
        var requestDate = DateTime.Now;

        // สร้าง Reserve ID (รูปแบบ YYYYMMDDXX)
        var reserveId = ReservationIdGenerator.GenerateCourseReservationId(requestDate);

        return new CourseCourtReservationItem
        {
            ReserveId = reserveId,
            CourtId = SelectedCourtId,
            ClassId = SelectedCourseId,
            RequestDate = requestDate,
            ReserveDate = reserveDate,
            ReserveTime = ReserveTime,
            Duration = Duration,
            ReserveName = CustomerName.Trim(),
            ReservePhone = CustomerPhone.Trim()
        };
    }
}
