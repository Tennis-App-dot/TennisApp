using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class CourseRegistrationDialog : ContentDialog
{
    private readonly TraineeItem _trainee;
    private readonly RegisterCoursePageViewModel _viewModel;
    private CourseItem? _selectedCourse;

    public CourseRegistrationDialog(TraineeItem trainee, RegisterCoursePageViewModel viewModel)
    {
        InitializeComponent();
        
        _trainee = trainee;
        _viewModel = viewModel;
        
        // Set dialog title with Thai font
        var titleTextBlock = new TextBlock
        {
            Text = $"สมัครคอร์สเรียน {trainee.TraineeId}",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        this.Title = titleTextBlock;
        
        LoadTraineeInfo();
        _ = LoadCoursesAsync();
    }

    private void LoadTraineeInfo()
    {
        try
        {
            // Display trainee info
            TxtTraineeId.Text = $"รหัสประจำตัวผู้เรียน: {_trainee.TraineeId}";
            TxtTraineeName.Text = $"ชื่อ-นามสกุล: {_trainee.FullName}";
            TxtTraineeNickname.Text = string.IsNullOrWhiteSpace(_trainee.Nickname)
                ? "ชื่อเล่น: -"
                : $"ชื่อเล่น: {_trainee.Nickname}";
            TxtTraineePhone.Text = string.IsNullOrWhiteSpace(_trainee.Phone) 
                ? "เบอร์โทรศัพท์: -" 
                : $"เบอร์โทรศัพท์: {_trainee.Phone}";

            // Load profile image
            if (_trainee.ImageData != null && _trainee.ImageData.Length > 0)
            {
                _ = LoadImageFromBytesAsync(_trainee.ImageData);
                PlaceholderIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                PlaceholderIcon.Visibility = Visibility.Visible;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Loaded trainee info: {_trainee.FullName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainee info: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task LoadImageFromBytesAsync(byte[] imageData)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageData.AsBuffer());
            stream.Seek(0);
            
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            
            ProfileImage.Source = bitmap;
            System.Diagnostics.Debug.WriteLine("✅ Profile image loaded");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading profile image: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task LoadCoursesAsync()
    {
        try
        {
            var courses = await _viewModel.GetAvailableCoursesAsync();
            
            CmbCourse.Items.Clear();
            foreach (var course in courses)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{course.ClassId} - {course.ClassTitle}",
                    Tag = course.ClassId
                };
                CmbCourse.Items.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Loaded {courses.Count} courses");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading courses: {ex.Message}");
        }
    }

    private async void CmbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbCourse.SelectedItem is ComboBoxItem item && item.Tag is string classId)
        {
            try
            {
                _selectedCourse = await _viewModel.GetCourseByIdAsync(classId);
                
                if (_selectedCourse != null)
                {
                    // Display course details
                    TxtCourseName.Text = _selectedCourse.ClassTitle;
                    TxtCourseSessions.Text = $"{_selectedCourse.ClassTime} ครั้ง";
                    TxtCourseDuration.Text = $"{_selectedCourse.ClassDuration} ชั่วโมง/ครั้ง";
                    TxtCourseFee.Text = $"{_selectedCourse.ClassRate:N0} บาท";
                    TxtCourseTrainer.Text = string.IsNullOrWhiteSpace(_selectedCourse.TrainerName) 
                        ? "-" 
                        : _selectedCourse.TrainerName;

                    CourseDetailsPanel.Visibility = Visibility.Visible;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Selected course: {_selectedCourse.ClassTitle}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading course details: {ex.Message}");
            }
        }
        else
        {
            CourseDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate course selection
        if (_selectedCourse == null)
        {
            args.Cancel = true;
            await ShowErrorDialog("กรุณาเลือกคอร์ส");
            return;
        }

        // Get deferral to perform async work
        var deferral = args.GetDeferral();

        try
        {
            // Register trainee to course
            var success = await _viewModel.RegisterToCourseAsync(_trainee.TraineeId, _selectedCourse.ClassId);

            if (success)
            {
                await ShowSuccessDialog($"สมัครคอร์ส {_selectedCourse.ClassId} สำเร็จ");
                System.Diagnostics.Debug.WriteLine($"✅ Registration successful: {_trainee.TraineeId} -> {_selectedCourse.ClassId}");
            }
            else
            {
                args.Cancel = true;
                await ShowErrorDialog("ไม่สามารถสมัครคอร์สได้\nผู้เรียนอาจจะสมัครคอร์สนี้แล้ว");
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            System.Diagnostics.Debug.WriteLine($"❌ Registration error: {ex.Message}");
            await ShowErrorDialog($"เกิดข้อผิดพลาด: {ex.Message}");
        }
        finally
        {
            deferral.Complete();
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
