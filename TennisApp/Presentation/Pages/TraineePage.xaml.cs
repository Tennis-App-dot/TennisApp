using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Presentation.ViewModels;
using TennisApp.Models;
using System.Linq;

namespace TennisApp.Presentation.Pages;

public sealed partial class TraineePage : Page
{
    public TraineePageViewModel ViewModel { get; }

    public TraineePage()
    {
        InitializeComponent();
        ViewModel = new TraineePageViewModel();
        
        // Wire up detail card events
        DetailCard.CloseRequested += (s, e) => HideDetailCard();
        DetailCard.EditRequested += DetailCard_EditRequested;
        DetailCard.DeleteRequested += DetailCard_DeleteRequested;
        DetailCard.HistoryRequested += DetailCard_HistoryRequested;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // โหลดข้อมูลทุกครั้งที่เข้าหน้านี้ (เพื่อแสดงข้อมูลใหม่หลังจาก Add/Edit)
        System.Diagnostics.Debug.WriteLine("TraineePage: OnNavigatedTo - Loading trainees...");
        await ViewModel.LoadTraineesAsync();
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

    private void BtnAddTrainee_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("TraineePage: BtnAddTrainee_Click");
        Frame.Navigate(typeof(TraineeFormPage));
    }

    private void TraineeRow_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is TraineeItem trainee)
        {
            System.Diagnostics.Debug.WriteLine($"📋 Row tapped: {trainee.TraineeId}");
            ShowDetailCard(trainee);
            e.Handled = true;
        }
    }

    private void Button_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Stop event propagation to prevent row tap from triggering
        e.Handled = true;
    }

    private void ShowDetailCard(TraineeItem trainee)
    {
        DetailCard.SetTrainee(trainee);
        DetailOverlay.Visibility = Visibility.Visible;
    }

    private void HideDetailCard()
    {
        DetailOverlay.Visibility = Visibility.Collapsed;
    }

    private void DetailOverlay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Close when tapping outside the card
        if (e.OriginalSource == DetailOverlay)
        {
            HideDetailCard();
        }
    }

    private void DetailCard_EditRequested(object? sender, string traineeId)
    {
        System.Diagnostics.Debug.WriteLine($"TraineePage: Edit from detail card: {traineeId}");
        HideDetailCard();
        Frame.Navigate(typeof(TraineeEditPage), traineeId);
    }

    private async void DetailCard_DeleteRequested(object? sender, string traineeId)
    {
        System.Diagnostics.Debug.WriteLine($"TraineePage: Delete from detail card: {traineeId}");
        HideDetailCard();
        
        await ShowDeleteConfirmationAndDelete(traineeId);
    }

    private void DetailCard_HistoryRequested(object? sender, string traineeId)
    {
        System.Diagnostics.Debug.WriteLine($"TraineePage: History from detail card: {traineeId}");
        HideDetailCard();
        Frame.Navigate(typeof(TraineeHistoryPage), traineeId);
    }

    private void BtnEditTrainee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string traineeId)
        {
            System.Diagnostics.Debug.WriteLine($"TraineePage: Editing trainee: {traineeId}");
            Frame.Navigate(typeof(TraineeEditPage), traineeId);
        }
    }

    private async void BtnDeleteTrainee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string traineeId)
        {
            await ShowDeleteConfirmationAndDelete(traineeId);
        }
    }

    private async System.Threading.Tasks.Task ShowDeleteConfirmationAndDelete(string traineeId)
    {
        System.Diagnostics.Debug.WriteLine($"TraineePage: Delete trainee requested: {traineeId}");

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
            Text = $"คุณต้องการลบผู้เรียนรหัส {traineeId} ใช่หรือไม่?",
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
            var success = await ViewModel.DeleteTraineeAsync(traineeId);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("✅ Trainee deleted successfully");
                await ShowSuccessDialog("ลบข้อมูลผู้เรียนเรียบร้อยแล้ว");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to delete trainee");
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
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans ไทย"),
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
