using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class RegisterCoursePage : Page
{
    private readonly DatabaseService _database;
    private List<ClassRegisRecordItem> _allRegistrations = new();
    private readonly ObservableCollection<ClassRegisRecordItem> _filteredRegistrations = new();

    public RegisterCoursePage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
        RegisListView.ItemsSource = _filteredRegistrations;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadRegistrationsAsync();
    }

    // ── Navigate to registration form ─────────────────────────
    private void BtnNewRegister_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CourseRegistrationFormPage));
    }

    // ── Load data ─────────────────────────────────────────────
    private async System.Threading.Tasks.Task LoadRegistrationsAsync()
    {
        try
        {
            LoadingRing.IsActive = true;
            EmptyPanel.Visibility = Visibility.Collapsed;
            RegisListView.Visibility = Visibility.Collapsed;

            _allRegistrations = await _database.Registrations.GetAllRegistrationsAsync();

            // Assign row numbers
            for (int i = 0; i < _allRegistrations.Count; i++)
            {
                _allRegistrations[i].RowNumber = i + 1;
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading registrations: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    // ── Filter ────────────────────────────────────────────────
    private void ApplyFilter()
    {
        var keyword = TxtSearch.Text?.Trim() ?? string.Empty;

        var filtered = string.IsNullOrEmpty(keyword)
            ? _allRegistrations
            : _allRegistrations.Where(r =>
                r.TraineeId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                r.TraineeName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                r.ClassId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                r.ClassName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                r.TrainerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        // Re-number filtered results
        for (int i = 0; i < filtered.Count; i++)
        {
            filtered[i].RowNumber = i + 1;
        }

        _filteredRegistrations.Clear();
        foreach (var item in filtered)
        {
            _filteredRegistrations.Add(item);
        }

        if (_filteredRegistrations.Count == 0)
        {
            EmptyPanel.Visibility = Visibility.Visible;
            RegisListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyPanel.Visibility = Visibility.Collapsed;
            RegisListView.Visibility = Visibility.Visible;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    // ── Delete ─────────────────────────────────────────────────
    private async void BtnDeleteRegis_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ClassRegisRecordItem record)
        {
            bool confirmed = await ShowConfirmDialog(
                "ลบรายการสมัคร",
                $"ต้องการลบรายการสมัครของ {record.TraineeName}\nคอร์ส {record.ClassId} - {record.ClassName} หรือไม่?"
            );
            if (!confirmed) return;

            try
            {
                var success = await _database.Registrations.DeleteRegistrationAsync(
                    record.TraineeId, record.ClassId, record.TrainerId);
                if (success)
                {
                    await LoadRegistrationsAsync();
                    await ShowMessageDialog("สำเร็จ", "ลบรายการสมัครเรียบร้อยแล้ว");
                }
                else
                {
                    await ShowMessageDialog("ข้อผิดพลาด", "ไม่สามารถลบรายการได้");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog("ข้อผิดพลาด", $"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }
    }

    // ── Dialogs ───────────────────────────────────────────────
    private async System.Threading.Tasks.Task ShowMessageDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = new TextBlock
            {
                Text = title,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = message,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };
        await dialog.ShowAsync();
    }

    private async System.Threading.Tasks.Task<bool> ShowConfirmDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = new TextBlock
            {
                Text = title,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = message,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
