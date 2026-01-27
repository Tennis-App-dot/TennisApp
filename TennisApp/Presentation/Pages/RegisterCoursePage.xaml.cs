using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Presentation.ViewModels;
using TennisApp.Presentation.Dialogs;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TennisApp.Presentation.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RegisterCoursePage : Page
{
    public RegisterCoursePageViewModel ViewModel { get; }

    public RegisterCoursePage()
    {
        InitializeComponent();
        ViewModel = new RegisterCoursePageViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        System.Diagnostics.Debug.WriteLine("RegisterCoursePage: OnNavigatedTo - Loading trainees...");
        await ViewModel.LoadTraineesAsync();
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SearchTraineesAsync();
    }

    private async void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchTraineesAsync();
            e.Handled = true;
        }
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string traineeId)
        {
            System.Diagnostics.Debug.WriteLine($"RegisterCoursePage: Opening registration dialog for trainee: {traineeId}");
            
            // Get trainee from the list
            var trainee = ViewModel.Trainees.FirstOrDefault(t => t.TraineeId == traineeId);
            if (trainee == null)
            {
                await ShowErrorDialog("ไม่พบข้อมูลผู้เรียน");
                return;
            }

            // Show registration dialog
            var dialog = new CourseRegistrationDialog(trainee, ViewModel);
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                System.Diagnostics.Debug.WriteLine("✅ Course registration completed successfully");
            }
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
}
