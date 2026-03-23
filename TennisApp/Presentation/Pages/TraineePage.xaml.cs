using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Presentation.ViewModels;
using TennisApp.Models;
using TennisApp.Services;
using System.Linq;

namespace TennisApp.Presentation.Pages;

public sealed partial class TraineePage : Page
{
    public TraineePageViewModel ViewModel { get; }
    private NotificationService? _notify;

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
        _notify = NotificationService.GetFromPage(this);
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
        if (sender is FrameworkElement element && element.DataContext is TraineeItem trainee)
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
        HideDetailCard();
    }

    private void DetailCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Prevent closing when tapping on the card itself
        e.Handled = true;
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
        if (_notify == null) return;

        bool confirmed = await _notify.ShowDeleteConfirmAsync(
            $"ผู้เรียนรหัส {traineeId}",
            this.XamlRoot!);

        if (confirmed)
        {
            var success = await ViewModel.DeleteTraineeAsync(traineeId);

            if (success)
                _notify.ShowSuccess("ลบข้อมูลผู้เรียนเรียบร้อยแล้ว");
            else
                _notify.ShowError("เกิดข้อผิดพลาดในการลบข้อมูล");
        }
    }
}
