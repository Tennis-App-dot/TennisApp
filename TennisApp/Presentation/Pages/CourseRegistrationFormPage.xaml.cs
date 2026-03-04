using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Models;
using TennisApp.Presentation.Dialogs;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseRegistrationFormPage : Page
{
    private readonly DatabaseService _database;
    private List<CourseCardItem> _allCourseCards = new();
    private CourseCardItem? _selectedCard;
    private int _selectedPrice;
    private readonly List<TraineeItem> _selectedTrainees = new();
    private readonly ObservableCollection<SelectedTraineeDisplayItem> _displayTrainees = new();

    public CourseRegistrationFormPage()
    {
        InitializeComponent();
        _database = ((App)Application.Current).DatabaseService;
        SelectedTraineesListView.ItemsSource = _displayTrainees;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadCourseCardsAsync();
    }

    // ── Load course cards ─────────────────────────────────────
    private async System.Threading.Tasks.Task LoadCourseCardsAsync()
    {
        try
        {
            var courses = await _database.Courses.GetAllCoursesAsync();
            var allRegistrations = await _database.Registrations.GetAllRegistrationsAsync();

            var regCounts = allRegistrations
                .GroupBy(r => r.ClassId)
                .ToDictionary(g => g.Key, g => g.Count());

            _allCourseCards.Clear();
            foreach (var course in courses)
            {
                regCounts.TryGetValue(course.ClassId, out int count);
                _allCourseCards.Add(new CourseCardItem(course, count));
            }

            RenderCourseCards(_allCourseCards);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading course cards: {ex.Message}");
        }
    }

    private void RenderCourseCards(List<CourseCardItem> cards)
    {
        CourseCardsPanel.Children.Clear();

        if (cards.Count == 0)
        {
            TxtNoCourses.Visibility = Visibility.Visible;
            return;
        }

        TxtNoCourses.Visibility = Visibility.Collapsed;

        foreach (var card in cards)
        {
            CourseCardsPanel.Children.Add(CreateCourseCard(card));
        }
    }

    private Border CreateCourseCard(CourseCardItem card)
    {
        bool isSelected = _selectedCard != null && _selectedCard.ClassId == card.ClassId;

        var border = new Border
        {
            Width = 155,
            MinHeight = 170,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(isSelected ? 2.5 : 1),
            BorderBrush = isSelected
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 74, 20, 140))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 224, 224, 224)),
            Background = isSelected
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 243, 229, 245))
                : new SolidColorBrush(Colors.White),
            Padding = new Thickness(14),
            Tag = card.ClassId
        };

        border.Tapped += CourseCard_Tapped;

        var stack = new StackPanel { Spacing = 6 };

        stack.Children.Add(new TextBlock
        {
            Text = card.ClassId, FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 74, 20, 140))
        });
        stack.Children.Add(new TextBlock
        {
            Text = card.ClassTitle, FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 66, 66, 66))
        });
        stack.Children.Add(new TextBlock
        {
            Text = card.SessionsText, FontSize = 12,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158))
        });
        stack.Children.Add(new TextBlock
        {
            Text = card.DefaultPriceText, FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 47, 47)),
            Margin = new Thickness(0, 2, 0, 0)
        });
        stack.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 238, 238, 238)),
            Margin = new Thickness(0, 2, 0, 2)
        });

        var trainerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        trainerPanel.Children.Add(new FontIcon
        {
            Glyph = card.HasTrainer ? "\uE77B" : "\uE7BA", FontSize = 13,
            Foreground = card.HasTrainer
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 33, 150, 243))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0))
        });
        trainerPanel.Children.Add(new TextBlock
        {
            Text = card.TrainerName, FontSize = 12,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 97, 97, 97)),
            TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1, MaxWidth = 100
        });
        stack.Children.Add(trainerPanel);

        stack.Children.Add(new TextBlock
        {
            Text = card.RegistrationCountText, FontSize = 11,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158))
        });

        border.Child = stack;
        return border;
    }

    // ── Course card tap ───────────────────────────────────────
    private void CourseCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string classId)
        {
            var card = _allCourseCards.FirstOrDefault(c => c.ClassId == classId);
            if (card == null) return;

            if (_selectedCard != null) _selectedCard.IsSelected = false;
            _selectedCard = card;
            _selectedCard.IsSelected = true;

            TxtSelectedCourseId.Text = $"คอร์ส {card.ClassId}";
            TxtCourseName.Text = card.ClassTitle;
            TxtCourseTrainer.Text = card.TrainerName;
            TxtPackagePrice.Text = "";
            CourseDetailsPanel.Visibility = Visibility.Visible;

            PopulatePackageComboBox(card.Course);
            BtnAddTrainee.IsEnabled = false;

            _selectedPrice = 0;
            ClearTrainees();
            UpdateSummary();
            UpdateButtonStates();
            RenderCourseCards(GetFilteredCards());
        }
    }

    // ── Search ────────────────────────────────────────────────
    private void TxtCourseSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderCourseCards(GetFilteredCards());
    }

    private void BtnCourseSearch_Click(object sender, RoutedEventArgs e)
    {
        RenderCourseCards(GetFilteredCards());
    }

    private List<CourseCardItem> GetFilteredCards()
    {
        var keyword = TxtCourseSearch.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(keyword)) return _allCourseCards;

        return _allCourseCards.Where(c =>
            c.ClassId.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            c.ClassTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            c.TrainerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    // ── Package selection ─────────────────────────────────────
    private void PopulatePackageComboBox(CourseItem course)
    {
        CmbPackage.Items.Clear();
        var tiers = new (string Label, int Price)[]
        {
            ("ครั้งละ", course.ClassRatePerTime),
            ("4 ครั้ง", course.ClassRate4),
            ("8 ครั้ง", course.ClassRate8),
            ("12 ครั้ง", course.ClassRate12),
            ("16 ครั้ง", course.ClassRate16),
            ("รายเดือน", course.ClassRateMonthly),
        };
        foreach (var (label, price) in tiers)
        {
            if (price > 0)
            {
                CmbPackage.Items.Add(new ComboBoxItem
                {
                    Content = $"{label}  —  ฿{price:N0}",
                    Tag = price
                });
            }
        }
    }

    private void CmbPackage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPackage.SelectedItem is ComboBoxItem item && item.Tag is int price)
        {
            _selectedPrice = price;
            TxtPackagePrice.Text = $"ราคา: ฿{price:N0}";
            BtnAddTrainee.IsEnabled = true;
        }
        else
        {
            _selectedPrice = 0;
            if (TxtPackagePrice != null) TxtPackagePrice.Text = "";
            BtnAddTrainee.IsEnabled = false;
        }
        UpdateSummary();
        UpdateButtonStates();
    }

    // ── Trainee management ────────────────────────────────────
    private async void BtnAddTrainee_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) return;

        var alreadySelectedIds = _selectedTrainees.Select(t => t.TraineeId).ToList();
        var dialog = new TraineeSelectionDialog(_selectedCard.ClassId, alreadySelectedIds);
        dialog.XamlRoot = this.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.SelectedTrainees.Count > 0)
        {
            foreach (var trainee in dialog.SelectedTrainees)
            {
                if (!_selectedTrainees.Any(t => t.TraineeId == trainee.TraineeId))
                    _selectedTrainees.Add(trainee);
            }
            RefreshDisplayList();
            UpdateSummary();
            UpdateButtonStates();
        }
    }

    private void BtnRemoveTrainee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string traineeId)
        {
            _selectedTrainees.RemoveAll(t => t.TraineeId == traineeId);
            RefreshDisplayList();
            UpdateSummary();
            UpdateButtonStates();
        }
    }

    private void ClearTrainees()
    {
        _selectedTrainees.Clear();
        RefreshDisplayList();
    }

    private void RefreshDisplayList()
    {
        _displayTrainees.Clear();
        for (int i = 0; i < _selectedTrainees.Count; i++)
            _displayTrainees.Add(SelectedTraineeDisplayItem.FromTrainee(_selectedTrainees[i], i + 1));

        EmptyTraineePanel.Visibility = _displayTrainees.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SelectedTraineesListView.Visibility = _displayTrainees.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Summary ───────────────────────────────────────────────
    private void UpdateSummary()
    {
        int count = _selectedTrainees.Count;
        int total = _selectedPrice * count;
        TxtTraineeCount.Text = $"{count} คน";
        TxtPricePerPerson.Text = _selectedPrice > 0 ? $"฿{_selectedPrice:N0}" : "฿0";
        TxtTotalPrice.Text = total > 0 ? $"฿{total:N0}" : "฿0";
    }

    private void UpdateButtonStates()
    {
        BtnConfirm.IsEnabled = _selectedCard != null && _selectedPrice > 0 && _selectedTrainees.Count > 0;
    }

    // ── Confirm / Back ────────────────────────────────────────
    private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null || _selectedTrainees.Count == 0) return;

        int count = _selectedTrainees.Count;
        int total = _selectedPrice * count;

        bool confirmed = await ShowConfirmDialog(
            "ยืนยันการสมัคร",
            $"ยืนยันการสมัครคอร์ส {_selectedCard.ClassId} - {_selectedCard.ClassTitle}\n" +
            $"จำนวน {count} คน  รวม ฿{total:N0}?"
        );
        if (!confirmed) return;

        int successCount = 0;
        var failedNames = new List<string>();

        foreach (var trainee in _selectedTrainees)
        {
            try
            {
                var exists = await _database.Registrations.RegistrationExistsAsync(trainee.TraineeId, _selectedCard.ClassId);
                if (exists)
                {
                    failedNames.Add($"{trainee.FullName} (สมัครแล้ว)");
                    continue;
                }

                var record = new ClassRegisRecordItem
                {
                    TraineeId = trainee.TraineeId,
                    ClassId = _selectedCard.ClassId,
                    RegisDate = DateTime.Now
                };

                var success = await _database.Registrations.AddRegistrationAsync(record);
                if (success) successCount++;
                else failedNames.Add($"{trainee.FullName} (เกิดข้อผิดพลาด)");
            }
            catch (Exception ex)
            {
                failedNames.Add($"{trainee.FullName} ({ex.Message})");
            }
        }

        if (failedNames.Count == 0)
            await ShowMessageDialog("สำเร็จ", $"สมัครคอร์สเรียนสำเร็จ {successCount} คน");
        else
        {
            string msg = $"สมัครสำเร็จ {successCount}/{count} คน";
            if (failedNames.Count > 0)
                msg += "\n\nไม่สำเร็จ:\n" + string.Join("\n", failedNames.Select(n => $"• {n}"));
            await ShowMessageDialog("ผลการสมัคร", msg);
        }

        // Navigate back to registration list
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    // ── Dialogs ───────────────────────────────────────────────
    private async System.Threading.Tasks.Task ShowMessageDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            CloseButtonText = "ตกลง",
            XamlRoot = this.XamlRoot,
            FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
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
                FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            },
            Content = new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai"),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
            FontFamily = new FontFamily("ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai")
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
