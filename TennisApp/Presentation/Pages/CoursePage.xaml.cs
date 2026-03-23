using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CoursePage : Page
{
    public CoursePageViewModel ViewModel { get; }
    private NotificationService? _notify;

    public CoursePage()
    {
        this.InitializeComponent();
        ViewModel = new CoursePageViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _notify = NotificationService.GetFromPage(this);
        await ViewModel.LoadCoursesAsync();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CourseFormPage));
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string compositeKey)
        {
            System.Diagnostics.Debug.WriteLine($"CoursePage: Editing course: {compositeKey}");
            Frame.Navigate(typeof(CourseEditPage), compositeKey);
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string compositeKey)
        {
            var key = CourseKey.Parse(compositeKey);
            if (key == null) return;

            await ShowDeleteConfirmationAndDelete(key);
        }
    }

    private void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SearchCoursesAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchCoursesAsync();
            e.Handled = true;
        }
    }

    private async System.Threading.Tasks.Task ShowDeleteConfirmationAndDelete(CourseKey key)
    {
        if (_notify == null) return;

        bool confirmed = await _notify.ShowDeleteConfirmAsync(
            $"คอร์ส {key.ClassId}",
            this.XamlRoot!);

        if (confirmed)
        {
            var success = await ViewModel.DeleteCourseByKeyAsync(key.ToString());

            if (success)
                _notify.ShowSuccess("ลบคอร์สเรียบร้อยแล้ว");
            else
                _notify.ShowError("ไม่สามารถลบคอร์สได้");
        }
    }
}
