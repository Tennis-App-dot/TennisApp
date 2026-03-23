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
                    Content = course.ComboBoxDisplayText,
                    Tag = course.CompositeKey
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
        if (CmbCourse.SelectedItem is ComboBoxItem item && item.Tag is string compositeKey)
        {
            try
            {
                var key = CourseKey.Parse(compositeKey);
                if (key != null)
                {
                    _selectedCourse = await _viewModel.GetCourseByKeyAsync(key.ClassId, key.TrainerId);
                }
                else
                {
                    _selectedCourse = null;
                }
                
                if (_selectedCourse != null)
                {
                    TxtCourseName.Text = _selectedCourse.ClassTitle;
                    TxtCourseSessions.Text = _selectedCourse.SessionCountText;
                    TxtCourseDuration.Text = $"{_selectedCourse.ClassDuration} ชั่วโมง/ครั้ง";
                    TxtCourseFee.Text = $"{_selectedCourse.ClassRate:N0} บาท";
                    TxtCourseTrainer.Text = _selectedCourse.TrainerDisplayName;

                    CourseDetailsPanel.Visibility = Visibility.Visible;
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
        if (_selectedCourse == null)
        {
            args.Cancel = true;
            var errorDialog = new ContentDialog
            {
                Title = "คำเตือน",
                Content = "กรุณาเลือกคอร์ส",
                CloseButtonText = "ตกลง",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
            return;
        }

        var deferral = args.GetDeferral();

        try
        {
            var success = await _viewModel.RegisterToCourseAsync(
                _trainee.TraineeId, _selectedCourse.ClassId, _selectedCourse.TrainerId);

            if (!success)
            {
                args.Cancel = true;
                var errorDialog = new ContentDialog
                {
                    Title = "ไม่สามารถสมัครได้",
                    Content = "ผู้เรียนอาจจะสมัครคอร์สนี้แล้ว",
                    CloseButtonText = "ตกลง",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            var errorDialog = new ContentDialog
            {
                Title = "เกิดข้อผิดพลาด",
                Content = ex.Message,
                CloseButtonText = "ตกลง",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
        finally
        {
            deferral.Complete();
        }
    }
}
