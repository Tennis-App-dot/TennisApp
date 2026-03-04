using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;

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

                // Load tier pricing
                TxtRatePerTime.Text = _currentCourse.ClassRatePerTime > 0 ? _currentCourse.ClassRatePerTime.ToString() : "";
                TxtRate4.Text = _currentCourse.ClassRate4 > 0 ? _currentCourse.ClassRate4.ToString() : "";
                TxtRate8.Text = _currentCourse.ClassRate8 > 0 ? _currentCourse.ClassRate8.ToString() : "";
                TxtRate12.Text = _currentCourse.ClassRate12 > 0 ? _currentCourse.ClassRate12.ToString() : "";
                TxtRate16.Text = _currentCourse.ClassRate16 > 0 ? _currentCourse.ClassRate16.ToString() : "";
                TxtRateMonthly.Text = _currentCourse.ClassRateMonthly > 0 ? _currentCourse.ClassRateMonthly.ToString() : "";

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
        if (string.IsNullOrWhiteSpace(TxtCourseName.Text))
        {
            await ShowErrorDialog("กรุณากรอกชื่อคอร์ส");
            return;
        }

        // Parse tier pricing
        int.TryParse(TxtRatePerTime.Text, out var ratePerTime);
        int.TryParse(TxtRate4.Text, out var rate4);
        int.TryParse(TxtRate8.Text, out var rate8);
        int.TryParse(TxtRate12.Text, out var rate12);
        int.TryParse(TxtRate16.Text, out var rate16);
        int.TryParse(TxtRateMonthly.Text, out var rateMonthly);

        var trainerId = CmbTrainer.SelectedItem is ComboBoxItem selectedTrainer
            ? selectedTrainer.Tag?.ToString()
            : null;

        var course = new CourseItem
        {
            ClassId = TxtCourseId.Text,
            ClassTitle = TxtCourseName.Text,
            ClassTime = _currentCourse?.ClassTime ?? 0,
            ClassDuration = 1,
            ClassRate = ratePerTime,
            ClassRatePerTime = ratePerTime,
            ClassRate4 = rate4,
            ClassRate8 = rate8,
            ClassRate12 = rate12,
            ClassRate16 = rate16,
            ClassRateMonthly = rateMonthly,
            TrainerId = trainerId ?? string.Empty
        };

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
