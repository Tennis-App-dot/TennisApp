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

            // Load trainee info
            _trainee = await databaseService.Trainees.GetTraineeByIdAsync(traineeId);

            if (_trainee == null)
            {
                await ShowErrorDialog("ไม่พบข้อมูลผู้เรียน");
                return;
            }

            // Display trainee info
            DisplayTraineeInfo(_trainee);

            // Load registration history
            _registrations = await databaseService.Registrations.GetRegistrationsByTraineeIdAsync(traineeId);

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
            TxtTraineeId.Text = $"รหัสประจำตัวผู้เรียน: {trainee.TraineeId}";
            TxtTraineeName.Text = $"ชื่อ-นามสกุล: {trainee.FullName}";
            TxtTraineeNickname.Text = string.IsNullOrWhiteSpace(trainee.Nickname)
                ? "ชื่อเล่น: -"
                : $"ชื่อเล่น: {trainee.Nickname}";

            if (trainee.BirthDate.HasValue)
            {
                TxtBirthDate.Text = $"วันเกิด: {trainee.BirthDate.Value:dd/MM/yyyy}";
                TxtAge.Text = trainee.Age.HasValue ? $"อายุ: {trainee.Age.Value} ปี" : "อายุ: -";
            }
            else
            {
                TxtBirthDate.Text = "วันเกิด: -";
                TxtAge.Text = "อายุ: -";
            }

            TxtPhone.Text = string.IsNullOrWhiteSpace(trainee.Phone)
                ? "เบอร์ติดต่อ: -"
                : $"เบอร์ติดต่อ: {trainee.Phone}";

            // Load profile image
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
