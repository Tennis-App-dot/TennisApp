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
using TennisApp.Services;
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
    private NotificationService? _notify;

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
        _notify = NotificationService.GetFromPage(this);
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
        if (sender is FrameworkElement element && element.DataContext is TrainerItem trainer)
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
        HideDetailCard();
    }

    private void DetailCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Prevent closing when tapping on the card itself
        e.Handled = true;
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
        bool confirmed;
        if (_notify != null)
        {
            confirmed = await _notify.ShowDeleteConfirmAsync(
                $"ผู้ฝึกสอนรหัส {trainerId}",
                this.XamlRoot!);
        }
        else
        {
            confirmed = await NotificationService.ConfirmAsync(
                "ยืนยันการลบ",
                $"คุณต้องการลบผู้ฝึกสอนรหัส {trainerId} ใช่หรือไม่?",
                this.XamlRoot!);
        }

        if (confirmed)
        {
            var success = await ViewModel.DeleteTrainerAsync(trainerId);

            if (success)
                _notify?.ShowSuccess("ลบข้อมูลผู้ฝึกสอนเรียบร้อยแล้ว");
            else
                _notify?.ShowError("เกิดข้อผิดพลาดในการลบข้อมูล");
        }
    }
}
