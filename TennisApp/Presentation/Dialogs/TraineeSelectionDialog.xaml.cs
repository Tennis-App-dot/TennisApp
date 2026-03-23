using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Dialogs;

public sealed partial class TraineeSelectionDialog : ContentDialog
{
    private readonly DatabaseService _database;
    private readonly string _classId;
    private readonly string _trainerId;
    private readonly List<string> _alreadySelectedIds;
    private List<SelectableTraineeItem> _allTrainees = new();
    private readonly ObservableCollection<SelectableTraineeItem> _filteredTrainees = new();

    /// <summary>
    /// Returns the trainees the user selected (confirmed).
    /// </summary>
    public List<TraineeItem> SelectedTrainees { get; private set; } = new();

    /// <param name="classId">Course ID to check existing registrations.</param>
    /// <param name="trainerId">Trainer ID for composite key check.</param>
    /// <param name="alreadySelectedIds">IDs already in the parent page list (to pre-check / disable).</param>
    public TraineeSelectionDialog(string classId, string trainerId, List<string> alreadySelectedIds)
    {
        InitializeComponent();

        _database = ((App)Application.Current).DatabaseService;
        _classId = classId;
        _trainerId = trainerId;
        _alreadySelectedIds = alreadySelectedIds ?? new List<string>();

        var titleTextBlock = new TextBlock
        {
            Text = "เลือกผู้เรียนที่ต้องการสมัคร",
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        this.Title = titleTextBlock;

        TraineeListView.ItemsSource = _filteredTrainees;

        _ = LoadTraineesAsync();
    }

    private async System.Threading.Tasks.Task LoadTraineesAsync()
    {
        try
        {
            LoadingRing.IsActive = true;
            NoResultsPanel.Visibility = Visibility.Collapsed;

            var trainees = await _database.Trainees.GetAllTraineesAsync();

            // Check which trainees are already registered for this specific course+trainer in DB
            var existingRegistrations = await _database.Registrations.GetRegistrationsByCompositeKeyAsync(_classId, _trainerId);
            var registeredIds = existingRegistrations.Select(r => r.TraineeId).ToHashSet();

            _allTrainees.Clear();
            foreach (var t in trainees)
            {
                bool isRegisteredInDb = registeredIds.Contains(t.TraineeId);
                bool isInParentList = _alreadySelectedIds.Contains(t.TraineeId);

                var item = new SelectableTraineeItem(t, isAlreadyRegistered: isRegisteredInDb);

                // Pre-select if already in parent list
                if (isInParentList && !isRegisteredInDb)
                {
                    item.IsSelected = true;
                }

                _allTrainees.Add(item);
            }

            ApplyFilter(string.Empty);
            UpdateSelectedCount();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading trainees: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void ApplyFilter(string keyword)
    {
        _filteredTrainees.Clear();

        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _allTrainees
            : _allTrainees.Where(t =>
                t.TraineeId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (t.Nickname?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Phone?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

        foreach (var item in filtered)
        {
            _filteredTrainees.Add(item);
        }

        NoResultsPanel.Visibility = _filteredTrainees.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateSelectedCount()
    {
        var count = _allTrainees.Count(t => t.IsSelected && !t.IsAlreadyRegistered);
        TxtSelectedCount.Text = $"เลือกแล้ว: {count} คน";
        this.PrimaryButtonText = count > 0 ? $"ยืนยันการเลือก ({count})" : "ยืนยันการเลือก";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(SearchBox.Text.Trim());
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilter(SearchBox.Text.Trim());
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSelectedCount();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedTrainees = _allTrainees
            .Where(t => t.IsSelected && !t.IsAlreadyRegistered)
            .Select(t => t.Trainee)
            .ToList();

        if (SelectedTrainees.Count == 0)
        {
            args.Cancel = true;
        }
    }
}
