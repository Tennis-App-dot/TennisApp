using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Helpers;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseEditPage : Page
{
    private readonly DatabaseService _database;
    private CourseItem? _currentCourse;
    private string _oldTrainerId = string.Empty;
    private NotificationService? _notify;
    private bool _isDuplicate;

    public CourseEditPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _notify = NotificationService.GetFromPage(this);

        if (e.Parameter is string compositeKey)
        {
            var key = CourseKey.Parse(compositeKey);
            if (key != null)
            {
                await LoadCourseDataAsync(key.ClassId, key.TrainerId);
            }
        }
    }

    private async System.Threading.Tasks.Task LoadCourseDataAsync(string classId, string trainerId)
    {
        try
        {
            _currentCourse = await _database.Courses.GetCourseByKeyAsync(classId, trainerId);

            if (_currentCourse == null)
            {
                _notify?.ShowError("ไม่พบข้อมูลคอร์ส");
                if (Frame.CanGoBack) Frame.GoBack();
                return;
            }

            _oldTrainerId = _currentCourse.TrainerId;

            // Display read-only info
            TxtClassId.Text = _currentCourse.ClassId;
            TxtTitle.Text = _currentCourse.ClassTitle;
            TxtSessions.Text = _currentCourse.SessionCountText;
            TxtPrice.Text = $"฿{_currentCourse.ClassRate:N0}";
            TxtCurrentTrainer.Text = _currentCourse.TrainerDisplayName;

            await LoadTrainersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading course: {ex.Message}");
            _notify?.ShowError("ไม่สามารถโหลดข้อมูลคอร์สได้");
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
                // Skip current trainer
                if (trainer.TrainerId == _oldTrainerId) continue;

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

    private async void CmbTrainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentCourse == null) return;
        if (CmbTrainer.SelectedItem is not ComboBoxItem item) return;

        var newTrainerId = item.Tag?.ToString();
        if (string.IsNullOrEmpty(newTrainerId)) return;

        // Check if this combination already exists
        try
        {
            _isDuplicate = await _database.Courses.CourseExistsAsync(_currentCourse.ClassId, newTrainerId);
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
        if (_currentCourse == null) return;

        if (CmbTrainer.SelectedItem is not ComboBoxItem selectedItem)
        {
            _notify?.ShowWarning("กรุณาเลือกผู้ฝึกสอนใหม่");
            return;
        }

        var newTrainerId = selectedItem.Tag?.ToString();
        if (string.IsNullOrEmpty(newTrainerId))
        {
            _notify?.ShowWarning("กรุณาเลือกผู้ฝึกสอนใหม่");
            return;
        }

        if (_isDuplicate)
        {
            _notify?.ShowWarning("คอร์สนี้ + ผู้ฝึกสอนคนใหม่มีอยู่แล้วในระบบ");
            return;
        }

        var success = await _database.Courses.UpdateCourseTrainerAsync(
            _currentCourse.ClassId, _oldTrainerId, newTrainerId);

        if (success)
        {
            _notify?.ShowSuccess("เปลี่ยนผู้ฝึกสอนเรียบร้อยแล้ว");
            if (Frame.CanGoBack) Frame.GoBack();
        }
        else
        {
            _notify?.ShowError("ไม่สามารถบันทึกข้อมูลได้");
        }
    }
}
