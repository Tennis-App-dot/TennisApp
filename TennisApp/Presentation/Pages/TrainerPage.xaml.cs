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
using TennisApp.Models;
using TennisApp.Presentation.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TennisApp.Presentation.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TrainerPage : Page
{
    public TrainerPageViewModel ViewModel { get; }

    public TrainerPage()
    {
        InitializeComponent();
        ViewModel = new TrainerPageViewModel();
        
        // Wire up detail card events
        DetailCard.CloseRequested += (s, e) => HideDetailCard();
        DetailCard.EditRequested += DetailCard_EditRequested;
        DetailCard.DeleteRequested += DetailCard_DeleteRequested;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // โหลดข้อมูลทุกครั้งที่เข้าหน้านี้ (เพื่อแสดงข้อมูลใหม่หลังจาก Add/Edit)
        System.Diagnostics.Debug.WriteLine("TrainerPage: OnNavigatedTo - Loading trainers...");
        await ViewModel.LoadTrainersAsync();
    }

    private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            // กด Enter ให้ค้นหา
            ViewModel.SearchCommand?.Execute(null);
            e.Handled = true;
        }
    }

    private void BtnAddTrainer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("TrainerPage: BtnAddTrainer_Click");
        Frame.Navigate(typeof(TrainerFormPage));
    }

    private void TrainerRow_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is TrainerItem trainer)
        {
            System.Diagnostics.Debug.WriteLine($"📋 Row tapped: {trainer.TrainerId}");
            ShowDetailCard(trainer);
            e.Handled = true;
        }
    }

    private void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Stop event propagation to prevent row tap from triggering
        e.Handled = true;
    }

    private void ShowDetailCard(TrainerItem trainer)
    {
        DetailCard.SetTrainer(trainer);
        DetailOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void HideDetailCard()
    {
        DetailOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void DetailOverlay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Close when tapping outside the card
        if (e.OriginalSource == DetailOverlay)
        {
            HideDetailCard();
        }
    }

    private void DetailCard_EditRequested(object? sender, string trainerId)
    {
        System.Diagnostics.Debug.WriteLine($"TrainerPage: Edit from detail card: {trainerId}");
        HideDetailCard();
        Frame.Navigate(typeof(TrainerEditPage), trainerId);
    }

    private async void DetailCard_DeleteRequested(object? sender, string trainerId)
    {
        System.Diagnostics.Debug.WriteLine($"TrainerPage: Delete from detail card: {trainerId}");
        HideDetailCard();
        
        // Show confirmation dialog
        await ShowDeleteConfirmationAndDelete(trainerId);
    }

    private void BtnEditTrainer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string trainerId)
        {
            System.Diagnostics.Debug.WriteLine($"TrainerPage: Editing trainer: {trainerId}");
            Frame.Navigate(typeof(TrainerEditPage), trainerId);
        }
    }

    private async void BtnDeleteTrainer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string trainerId)
        {
            await ShowDeleteConfirmationAndDelete(trainerId);
        }
    }

    private async System.Threading.Tasks.Task ShowDeleteConfirmationAndDelete(string trainerId)
    {
        System.Diagnostics.Debug.WriteLine($"TrainerPage: Delete trainer requested: {trainerId}");

        // สร้าง TextBlock สำหรับ Title
        var titleTextBlock = new TextBlock
        {
            Text = "ยืนยันการลบ",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        // สร้าง TextBlock สำหรับ Content
        var contentTextBlock = new TextBlock
        {
            Text = $"คุณต้องการลบผู้ฝึกสอนรหัส {trainerId} ใช่หรือไม่?",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            TextWrapping = TextWrapping.Wrap
        };

        // แสดง confirmation dialog
        var dialog = new ContentDialog
        {
            Title = titleTextBlock,
            Content = contentTextBlock,
            PrimaryButtonText = "ลบ",
            CloseButtonText = "ยกเลิก",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // ลบข้อมูล
            var success = await ViewModel.DeleteTrainerAsync(trainerId);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("✅ Trainer deleted successfully");
                await ShowSuccessDialog("ลบข้อมูลผู้ฝึกสอนเรียบร้อยแล้ว");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to delete trainer");
                await ShowErrorDialog("เกิดข้อผิดพลาดในการลบข้อมูล");
            }
        }
    }

    private async System.Threading.Tasks.Task ShowSuccessDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "สำเร็จ",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
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

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "ข้อผิดพลาด",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
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

    private async void ShowNotImplementedDialog(string feature)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "กำลังพัฒนา",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };

        var contentTextBlock = new TextBlock
        {
            Text = $"ฟีเจอร์ \"{feature}\" กำลังอยู่ระหว่างการพัฒนา",
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
