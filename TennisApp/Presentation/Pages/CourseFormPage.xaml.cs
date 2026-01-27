using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseFormPage : Page
{
    private readonly DatabaseService _database;
    private bool _isAutoFilling = false; // Prevent recursive updates

    public CourseFormPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
        
        // Load trainers for ComboBox
        _ = LoadTrainersAsync();
    }

    private async System.Threading.Tasks.Task LoadTrainersAsync()
    {
        try
        {
            var trainers = await _database.Trainers.GetAllTrainersAsync();
            
            CmbTrainer.Items.Clear();
            foreach (var trainer in trainers)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{trainer.FirstName} {trainer.LastName}",
                    Tag = trainer.TrainerId
                };
                CmbTrainer.Items.Add(item);
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainers: {ex.Message}");
        }
    }

    /// <summary>
    /// Auto-fill session count based on course ID (course name removed from auto-fill)
    /// </summary>
    private void TxtCourseId_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isAutoFilling) return;

        string courseIdInput = TxtCourseId.Text.Trim().ToUpper();

        // Get references to XAML controls
        var txtCourseIdHint = this.FindName("TxtCourseIdHint") as TextBlock;
        var txtSessionsAutoIndicator = this.FindName("TxtSessionsAutoIndicator") as TextBlock;

        // Parse course ID
        var (isValid, courseType, courseName, sessionCount, errorMessage) = CourseIdParser.ParseCourseId(courseIdInput);

        if (isValid)
        {
            // Valid course ID - auto-fill session count only (NOT course name)
            _isAutoFilling = true;

            // Only auto-fill session count, let user type course name manually
            TxtSessions.Text = sessionCount.ToString();

            // Show success indicator
            if (txtCourseIdHint != null)
            {
                txtCourseIdHint.Text = $"{CourseIdParser.GetCourseTypeDescription(courseType)} - {sessionCount} ครั้ง";
                txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Green);
            }

            if (txtSessionsAutoIndicator != null)
            {
                txtSessionsAutoIndicator.Visibility = Visibility.Visible;
            }

            _isAutoFilling = false;

            System.Diagnostics.Debug.WriteLine($"✅ Auto-filled session count: {sessionCount} (course name: manual entry)");
        }
        else if (!string.IsNullOrEmpty(courseIdInput))
        {
            // Invalid course ID - show error
            if (txtCourseIdHint != null)
            {
                if (courseIdInput.Length == 4)
                {
                    // Only show detailed error when user has typed 4 characters
                    txtCourseIdHint.Text = errorMessage;
                    txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Red);
                }
                else
                {
                    // Still typing - show format hint
                    txtCourseIdHint.Text = "รูปแบบ: XX## (ประเภท 2 ตัว + ครั้ง 01-99)";
                    txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Gray);
                }
            }

            if (txtSessionsAutoIndicator != null)
            {
                txtSessionsAutoIndicator.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            // Empty input - reset to default hint
            if (txtCourseIdHint != null)
            {
                txtCourseIdHint.Text = "รูปแบบ: XX## (ประเภท 2 ตัว + ครั้ง 01-99)";
                txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Gray);
            }

            if (txtSessionsAutoIndicator != null)
            {
                txtSessionsAutoIndicator.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(TxtCourseId.Text))
        {
            await ShowErrorDialog("กรุณากรอกรหัสคอร์ส");
            return;
        }

        // Use CourseIdParser for validation
        var (isValid, _, _, _, errorMessage) = CourseIdParser.ParseCourseId(TxtCourseId.Text);
        
        if (!isValid)
        {
            await ShowErrorDialog($"รหัสคอร์สไม่ถูกต้อง\n{errorMessage}");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtCourseName.Text))
        {
            await ShowErrorDialog("กรุณากรอกชื่อคอร์ส");
            return;
        }

        // Validate session count
        if (string.IsNullOrWhiteSpace(TxtSessions.Text) || !int.TryParse(TxtSessions.Text, out var sessions))
        {
            await ShowErrorDialog("กรุณากรอกจำนวนครั้งที่ถูกต้อง");
            return;
        }

        if (!CourseIdParser.IsValidSessionCount(sessions))
        {
            await ShowErrorDialog("จำนวนครั้งต้องอยู่ระหว่าง 1-99");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtCourseFee.Text) || !int.TryParse(TxtCourseFee.Text, out var fee))
        {
            await ShowErrorDialog("กรุณากรอกค่าสมัครคอร์สที่ถูกต้อง");
            return;
        }

        // Check if course already exists
        if (await _database.Courses.CourseExistsAsync(TxtCourseId.Text.ToUpper()))
        {
            await ShowErrorDialog($"รหัสคอร์ส {TxtCourseId.Text.ToUpper()} มีอยู่แล้ว");
            return;
        }

        // Get selected values
        var duration = 1; // Fixed at 1 hour per session
        var trainerId = CmbTrainer.SelectedItem is ComboBoxItem selectedTrainer 
            ? selectedTrainer.Tag?.ToString() 
            : null;

        // Create course object
        var course = new CourseItem
        {
            ClassId = TxtCourseId.Text.ToUpper(),
            ClassTitle = TxtCourseName.Text,
            ClassTime = sessions,
            ClassDuration = duration,
            ClassRate = fee,
            TrainerId = trainerId ?? string.Empty
        };

        // Save to database
        var success = await _database.Courses.AddCourseAsync(course);

        if (success)
        {
            await ShowSuccessDialog("บันทึกข้อมูลคอร์สเรียบร้อยแล้ว");

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
        else
        {
            await ShowErrorDialog("ไม่สามารถบันทึกข้อมูลได้");
        }
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "ข้อผิดพลาด",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var contentTextBlock = new TextBlock
        {
            Text = message,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dialog = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task ShowSuccessDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "สำเร็จ",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var contentTextBlock = new TextBlock
        {
            Text = message,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dialog = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        await dialog.ShowAsync();
    }
}
