using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseEditPage : Page
{
    private readonly DatabaseService _database;
    private string _classId = string.Empty;
    private CourseItem? _currentCourse;

    public CourseEditPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string classId)
        {
            _classId = classId;
            await LoadCourseDataAsync(classId);
            await LoadTrainersAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadCourseDataAsync(string classId)
    {
        try
        {
            _currentCourse = await _database.Courses.GetCourseByIdAsync(classId);

            if (_currentCourse != null)
            {
                TxtCourseId.Text = _currentCourse.ClassId;
                TxtCourseName.Text = _currentCourse.ClassTitle;
                TxtCourseFee.Text = _currentCourse.ClassRate.ToString();
                TxtSessions.Text = _currentCourse.ClassTime.ToString();

                // Parse and show course type information with validation
                var txtCourseIdHint = this.FindName("TxtCourseIdHint") as TextBlock;
                var (isValid, courseType, courseName, sessionCountFromId, _) = CourseIdParser.ParseCourseId(classId);
                
                if (isValid && txtCourseIdHint != null)
                {
                    // Check if session count matches the course ID
                    if (_currentCourse.ClassTime != sessionCountFromId)
                    {
                        txtCourseIdHint.Text = $"{CourseIdParser.GetCourseTypeDescription(courseType)} - รหัสระบุ {sessionCountFromId} ครั้ง แต่ฐานข้อมูล {_currentCourse.ClassTime} ครั้ง";
                        txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.Colors.Orange);
                    }
                    else
                    {
                        txtCourseIdHint.Text = $"{CourseIdParser.GetCourseTypeDescription(courseType)} - {sessionCountFromId} ครั้ง";
                        txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.Colors.DodgerBlue);
                    }
                }
                else if (txtCourseIdHint != null)
                {
                    txtCourseIdHint.Text = "รหัสคอร์สไม่ถูกต้องตามรูปแบบ";
                    txtCourseIdHint.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Red);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded course data: {classId}");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading course: {ex.Message}");
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
                var item = new ComboBoxItem
                {
                    Content = $"{trainer.FirstName} {trainer.LastName}",
                    Tag = trainer.TrainerId
                };
                CmbTrainer.Items.Add(item);

                // Select current trainer
                if (_currentCourse != null && trainer.TrainerId == _currentCourse.TrainerId)
                {
                    CmbTrainer.SelectedItem = item;
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainers: {ex.Message}");
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

        // Validate that session count matches course ID
        var (isValid, _, _, sessionCountFromId, _) = CourseIdParser.ParseCourseId(TxtCourseId.Text);
        if (isValid && sessions != sessionCountFromId)
        {
            var confirm = await ShowConfirmDialog(
                "จำนวนครั้งไม่ตรงกับรหัสคอร์ส",
                $"รหัสคอร์ส {TxtCourseId.Text} ระบุว่าควรเป็น {sessionCountFromId} ครั้ง\n" +
                $"แต่คุณกรอก {sessions} ครั้ง\n\n" +
                $"คุณแน่ใจหรือไม่ว่าต้องการบันทึก?"
            );

            if (!confirm)
            {
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(TxtCourseFee.Text) || !int.TryParse(TxtCourseFee.Text, out var fee))
        {
            await ShowErrorDialog("กรุณากรอกค่าสมัครคอร์สที่ถูกต้อง");
            return;
        }

        // Get selected values
        var duration = 1; // Fixed at 1 hour per session
        var trainerId = CmbTrainer.SelectedItem is ComboBoxItem selectedTrainer 
            ? selectedTrainer.Tag?.ToString() 
            : null;

        // Update course object
        var course = new CourseItem
        {
            ClassId = TxtCourseId.Text,
            ClassTitle = TxtCourseName.Text,
            ClassTime = sessions,
            ClassDuration = duration,
            ClassRate = fee,
            TrainerId = trainerId ?? string.Empty
        };

        // Update in database
        var success = await _database.Courses.UpdateCourseAsync(course);

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

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(string title, string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = title,
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
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
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
