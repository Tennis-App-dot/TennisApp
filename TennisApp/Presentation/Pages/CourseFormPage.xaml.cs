using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Helpers;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseFormPage : Page
{
    private readonly DatabaseService _database;
    private NotificationService? _notify;

    private string? _selectedCourseType;
    private int _selectedSessions = -1;
    private string? _selectedTrainerId;
    private string? _selectedTrainerName;
    private bool _isDuplicate;

    public CourseFormPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
        this.Loaded += CourseFormPage_Loaded;
    }

    private async void CourseFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        _notify = NotificationService.GetFromPage(this);
        LoadCourseTypes();
        await LoadTrainersAsync();
    }

    private void LoadCourseTypes()
    {
        CmbCourseType.Items.Clear();
        foreach (var type in CoursePricingHelper.GetAllCourseTypes())
        {
            CmbCourseType.Items.Add(new ComboBoxItem
            {
                Content = CoursePricingHelper.GetCourseDisplayName(type),
                Tag = type
            });
        }
    }

    private async System.Threading.Tasks.Task LoadTrainersAsync()
    {
        try
        {
            var trainers = await _database.Trainers.GetAllTrainersAsync();
            CmbTrainer.Items.Clear();
            foreach (var trainer in trainers)
            {
                CmbTrainer.Items.Add(new ComboBoxItem
                {
                    Content = $"{trainer.FirstName} {trainer.LastName}",
                    Tag = trainer.TrainerId
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainers: {ex.Message}");
        }
    }

    private void CmbCourseType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbCourseType.SelectedItem is ComboBoxItem item && item.Tag is string courseType)
        {
            _selectedCourseType = courseType;
            _selectedSessions = -1;

            // Populate session ComboBox with valid sessions for this type
            CmbSessionCount.Items.Clear();
            var validSessions = CoursePricingHelper.GetValidSessions(courseType);
            foreach (var sessions in validSessions)
            {
                var price = CoursePricingHelper.GetPrice(courseType, sessions);
                var text = $"{CoursePricingHelper.GetSessionDisplayText(sessions)}  —  ฿{price:N0}";
                CmbSessionCount.Items.Add(new ComboBoxItem
                {
                    Content = text,
                    Tag = sessions.ToString()
                });
            }

            CmbSessionCount.IsEnabled = true;
            CmbSessionCount.SelectedIndex = -1;
            UpdateSummary();
        }
    }

    private void CmbSessionCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSessionCount.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (int.TryParse(tag, out var sessions))
            {
                _selectedSessions = sessions;
                UpdateSummary();
            }
        }
    }

    private void CmbTrainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTrainer.SelectedItem is ComboBoxItem item)
        {
            _selectedTrainerId = item.Tag?.ToString();
            _selectedTrainerName = item.Content?.ToString();
            UpdateSummary();
        }
    }

    private async void UpdateSummary()
    {
        bool hasAll = _selectedCourseType != null && _selectedSessions >= 0 && !string.IsNullOrEmpty(_selectedTrainerId);

        if (!hasAll || _selectedCourseType == null || _selectedTrainerId == null)
        {
            SummarySection.Visibility = Visibility.Collapsed;
            BtnSave.IsEnabled = false;
            return;
        }

        var classId = CoursePricingHelper.GenerateClassId(_selectedCourseType, _selectedSessions);
        var price = CoursePricingHelper.GetPrice(_selectedCourseType, _selectedSessions);
        var title = CoursePricingHelper.GetCourseName(_selectedCourseType);
        var sessionsText = CoursePricingHelper.GetSessionDisplayText(_selectedSessions);

        TxtSummaryClassId.Text = classId;
        TxtSummaryTitle.Text = title;
        TxtSummarySessions.Text = sessionsText;
        TxtSummaryPrice.Text = $"฿{price:N0}";
        TxtSummaryTrainer.Text = _selectedTrainerName ?? "ไม่ระบุ";

        SummarySection.Visibility = Visibility.Visible;

        // Check duplicate
        try
        {
            _isDuplicate = await _database.Courses.CourseExistsAsync(classId, _selectedTrainerId);
            DuplicateWarning.Visibility = _isDuplicate ? Visibility.Visible : Visibility.Collapsed;
            BtnSave.IsEnabled = !_isDuplicate;
        }
        catch
        {
            _isDuplicate = false;
            DuplicateWarning.Visibility = Visibility.Collapsed;
            BtnSave.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCourseType == null || _selectedSessions < 0 || string.IsNullOrEmpty(_selectedTrainerId))
        {
            _notify?.ShowWarning("กรุณาเลือกข้อมูลให้ครบทุกช่อง");
            return;
        }

        if (_isDuplicate)
        {
            _notify?.ShowWarning("คอร์สนี้ + ผู้ฝึกสอนคนนี้มีอยู่แล้วในระบบ");
            return;
        }

        var course = CourseItem.Create(_selectedCourseType, _selectedSessions, _selectedTrainerId, _selectedTrainerName);

        var success = await _database.Courses.AddCourseAsync(course);

        if (success)
        {
            _notify?.ShowSuccess("บันทึกข้อมูลคอร์สเรียบร้อยแล้ว");
            if (Frame.CanGoBack) Frame.GoBack();
        }
        else
        {
            _notify?.ShowError("ไม่สามารถบันทึกข้อมูลได้");
        }
    }
}
