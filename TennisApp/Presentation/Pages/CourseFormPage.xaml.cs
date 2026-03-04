using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseFormPage : Page
{
    private readonly DatabaseService _database;

    public CourseFormPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;

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

        if (string.IsNullOrWhiteSpace(TxtCourseName.Text))
        {
            await ShowErrorDialog("กรุณากรอกชื่อคอร์ส");
            return;
        }

        if (CmbTrainer.SelectedItem == null)
        {
            await ShowErrorDialog("กรุณาเลือกผู้รับผิดชอบคอร์ส");
            return;
        }

        // Check if course already exists
        var courseId = TxtCourseId.Text.Trim().ToUpper();
        if (await _database.Courses.CourseExistsAsync(courseId))
        {
            await ShowErrorDialog($"รหัสคอร์ส {courseId} มีอยู่แล้ว");
            return;
        }

        // Parse tier pricing (0 if empty or invalid)
        int.TryParse(TxtRatePerTime.Text, out var ratePerTime);
        int.TryParse(TxtRate4.Text, out var rate4);
        int.TryParse(TxtRate8.Text, out var rate8);
        int.TryParse(TxtRate12.Text, out var rate12);
        int.TryParse(TxtRate16.Text, out var rate16);
        int.TryParse(TxtRateMonthly.Text, out var rateMonthly);

        var trainerId = CmbTrainer.SelectedItem is ComboBoxItem selectedTrainer
            ? selectedTrainer.Tag?.ToString()
            : null;

        // Create course object
        var course = new CourseItem
        {
            ClassId = courseId,
            ClassTitle = TxtCourseName.Text,
            ClassTime = 0,
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
