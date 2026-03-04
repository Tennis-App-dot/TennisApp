using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Pages;

public sealed partial class TraineeHistoryPage : Page
{
    private TraineeItem? _trainee;
    private List<ClassRegisRecordItem> _registrations = new();

    public TraineeHistoryPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string traineeId)
        {
            await LoadTraineeHistoryAsync(traineeId);
        }
    }

    private async System.Threading.Tasks.Task LoadTraineeHistoryAsync(string traineeId)
    {
        try
        {
            LoadingRing.IsActive = true;

            var databaseService = new DatabaseService();

            _trainee = await databaseService.Trainees.GetTraineeByIdAsync(traineeId);

            if (_trainee == null)
            {
                await ShowErrorDialog("ไม่พบข้อมูลผู้เรียน");
                return;
            }

            DisplayTraineeInfo(_trainee);

            _registrations = await databaseService.Registrations.GetRegistrationsByTraineeIdAsync(traineeId);

            // Update summary badge
            TxtTotalCourses.Text = _registrations.Count.ToString();

            if (_registrations.Count == 0)
            {
                NoResultsPanel.Visibility = Visibility.Visible;
                RegistrationListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoResultsPanel.Visibility = Visibility.Collapsed;
                RegistrationListView.Visibility = Visibility.Visible;
                RegistrationListView.ItemsSource = _registrations;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Loaded {_registrations.Count} registrations for trainee: {traineeId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainee history: {ex.Message}");
            await ShowErrorDialog($"เกิดข้อผิดพลาด: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private async void DisplayTraineeInfo(TraineeItem trainee)
    {
        try
        {
            TxtTraineeName.Text = trainee.FullName;
            TxtTraineeId.Text = $"รหัส: {trainee.TraineeId}";
            TxtTraineeNickname.Text = string.IsNullOrWhiteSpace(trainee.Nickname)
                ? "" : $"ชื่อเล่น: {trainee.Nickname}";
            TxtPhone.Text = string.IsNullOrWhiteSpace(trainee.Phone)
                ? "เบอร์: -" : $"เบอร์: {trainee.Phone}";
            TxtBirthDate.Text = trainee.BirthDate.HasValue
                ? $"วันเกิด: {trainee.BirthDate.Value:dd/MM/yyyy}" : "วันเกิด: -";
            TxtAge.Text = trainee.Age.HasValue
                ? $"อายุ: {trainee.Age.Value} ปี" : "";

            if (trainee.ImageData != null && trainee.ImageData.Length > 0)
            {
                var bitmap = await ImageHelper.CreateBitmapFromBytesAsync(trainee.ImageData);
                if (bitmap != null)
                {
                    ProfileImage.Source = bitmap;
                    PlaceholderIcon.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                PlaceholderIcon.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error displaying trainee info: {ex.Message}");
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
