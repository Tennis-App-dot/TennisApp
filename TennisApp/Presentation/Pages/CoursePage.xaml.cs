using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Presentation.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TennisApp.Presentation.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CoursePage : Page
{
    public CoursePageViewModel ViewModel { get; }

    public CoursePage()
    {
        this.InitializeComponent();
        ViewModel = new CoursePageViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        System.Diagnostics.Debug.WriteLine("CoursePage: OnNavigatedTo - Loading courses...");
        await ViewModel.LoadCoursesAsync();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CourseFormPage));
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string classId)
        {
            System.Diagnostics.Debug.WriteLine($"CoursePage: Editing course: {classId}");
            Frame.Navigate(typeof(CourseEditPage), classId);
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string classId)
        {
            await ShowDeleteConfirmationAndDelete(classId);
        }
    }

    private void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Stop event propagation to prevent row tap from triggering
        e.Handled = true;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SearchKeyword = SearchTextBox.Text;
        await ViewModel.SearchCoursesAsync();
    }

    private async void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.SearchKeyword = SearchTextBox.Text;
            await ViewModel.SearchCoursesAsync();
            e.Handled = true;
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterComboBox.SelectedItem is ComboBoxItem item)
        {
            ViewModel.SelectedFilterField = item.Content?.ToString() ?? "ทั้งหมด";
        }
    }

    private async System.Threading.Tasks.Task ShowDeleteConfirmationAndDelete(string classId)
    {
        System.Diagnostics.Debug.WriteLine($"CoursePage: Delete course requested: {classId}");

        bool confirmed = await ShowConfirm(
            "ยืนยันการลบ",
            $"คุณต้องการลบคอร์ส {classId} ใช่หรือไม่?"
        );

        if (confirmed)
        {
            var success = await ViewModel.DeleteCourseAsync(classId);

            if (success)
            {
                await ShowMessage("สำเร็จ", "ลบคอร์สเรียบร้อยแล้ว");
            }
            else
            {
                await ShowMessage("ข้อผิดพลาด", "ไม่สามารถลบคอร์สได้");
            }
        }
    }

    /// <summary>
    /// Shows a message dialog with Thai font support
    /// </summary>
    private async System.Threading.Tasks.Task ShowMessage(string title, string content)
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
            Text = content,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dlg = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        await dlg.ShowAsync();
    }

    /// <summary>
    /// Shows a confirmation dialog with Thai font support
    /// </summary>
    private async System.Threading.Tasks.Task<bool> ShowConfirm(string title, string content)
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
            Text = content,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        var dlg = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        var result = await dlg.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
