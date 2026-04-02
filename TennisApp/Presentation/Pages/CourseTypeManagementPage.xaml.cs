using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TennisApp.Helpers;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Presentation.Pages;

public sealed partial class CourseTypeManagementPage : Page
{
    private readonly DatabaseService _db;
    private NotificationService? _notify;
    private List<CourseTypeItem> _types = [];
    private Dictionary<string, List<CoursePackageItem>> _packagesByType = [];
    private string _searchText = "";

    public CourseTypeManagementPage()
    {
        InitializeComponent();
        _db = ((App)Application.Current).DatabaseService;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _notify = NotificationService.GetFromPage(this);
        await LoadDataAsync();
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    // ========================================================================
    // Search
    // ========================================================================

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchTextBox.Text?.Trim().ToLower() ?? "";
        RenderFilteredList();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        _searchText = SearchTextBox.Text?.Trim().ToLower() ?? "";
        RenderFilteredList();
    }

    // ========================================================================
    // Load
    // ========================================================================

    private async System.Threading.Tasks.Task LoadDataAsync()
    {
        try
        {
            _db.EnsureInitialized();
            _types = await _db.CourseTypes.GetAllTypesAsync();
            var allPackages = await _db.CourseTypes.GetAllPackagesAsync();
            _packagesByType = allPackages.GroupBy(p => p.TypeCode).ToDictionary(g => g.Key, g => g.ToList());

            RenderFilteredList();
        }
        catch (Exception ex)
        {
            _notify?.ShowError($"โหลดข้อมูลล้มเหลว: {ex.Message}");
        }
    }

    private void RenderFilteredList()
    {
        CardContainer.Children.Clear();

        var filtered = string.IsNullOrEmpty(_searchText)
            ? _types
            : _types.Where(t =>
                t.TypeCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                t.TypeName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                t.TypeNameThai.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0)
        {
            EmptyPanel.Visibility = Visibility.Visible;
            CardScrollViewer.Visibility = Visibility.Collapsed;
            EmptyStateText.Text = string.IsNullOrEmpty(_searchText)
                ? "ยังไม่มีประเภทคอร์ส"
                : $"ไม่พบผลลัพธ์สำหรับ \"{_searchText}\"";
            return;
        }

        EmptyPanel.Visibility = Visibility.Collapsed;
        CardScrollViewer.Visibility = Visibility.Visible;

        foreach (var type in filtered)
        {
            _packagesByType.TryGetValue(type.TypeCode, out var packages);
            CardContainer.Children.Add(BuildTypeCard(type, packages ?? []));
        }
    }

    // ========================================================================
    // Build UI Card
    // ========================================================================

    private Border BuildTypeCard(CourseTypeItem type, List<CoursePackageItem> packages)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Colors.White),
            CornerRadius = new CornerRadius(14),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 229, 231, 235)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18)
        };

        var root = new StackPanel { Spacing = 10 };

        // ── Header ──
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var badge = new Border
        {
            Width = 48, Height = 48,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 237, 231, 246)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = type.TypeCode,
                FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 108, 43, 217)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var titleInfo = new StackPanel { Spacing = 2, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        titleInfo.Children.Add(new TextBlock
        {
            Text = type.DisplayName,
            FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 26, 26, 46))
        });
        titleInfo.Children.Add(new TextBlock
        {
            Text = $"{packages.Count} แพ็กเกจ",
            FontSize = 12, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 156, 163, 175))
        });

        var leftStack = new StackPanel { Orientation = Orientation.Horizontal };
        leftStack.Children.Add(badge);
        leftStack.Children.Add(titleInfo);
        Grid.SetColumn(leftStack, 0);
        header.Children.Add(leftStack);

        // Action buttons
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

        var btnAddPkg = CreateIconButton("\uE710", ColorHelper.FromArgb(255, 232, 245, 233), type.TypeCode);
        btnAddPkg.Click += BtnAddPackage_Click;
        ToolTipService.SetToolTip(btnAddPkg, "เพิ่มแพ็กเกจ");
        btnPanel.Children.Add(btnAddPkg);

        var btnEdit = CreateIconButton("\uE70F", ColorHelper.FromArgb(255, 227, 242, 253), type.TypeCode);
        btnEdit.Click += BtnEditType_Click;
        ToolTipService.SetToolTip(btnEdit, "แก้ไข");
        btnPanel.Children.Add(btnEdit);

        var btnDel = new Button
        {
            Content = new FontIcon { Glyph = "\uE74D", FontSize = 14, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 38, 38)) },
            Tag = type.TypeCode,
            Width = 38, Height = 38, Padding = new Thickness(0),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 254, 226, 226)),
            BorderThickness = new Thickness(0)
        };
        ToolTipService.SetToolTip(btnDel, "ลบ");
        btnDel.Click += BtnDeleteType_Click;
        btnPanel.Children.Add(btnDel);

        Grid.SetColumn(btnPanel, 1);
        header.Children.Add(btnPanel);
        root.Children.Add(header);

        // ── Separator ──
        root.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 243, 244, 246)),
            Margin = new Thickness(0, 4, 0, 2)
        });

        // ── Package list ──
        if (packages.Count > 0)
        {
            foreach (var pkg in packages.OrderBy(p => p.Sessions))
            {
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var sessionBlock = new TextBlock
                {
                    Text = pkg.SessionsDisplay,
                    FontSize = 14, VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(sessionBlock, 0);
                row.Children.Add(sessionBlock);

                var priceBlock = new TextBlock
                {
                    Text = pkg.PriceDisplay,
                    FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 211, 47, 47)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(priceBlock, 1);
                row.Children.Add(priceBlock);

                var btnDelPkg = new Button
                {
                    Content = new FontIcon { Glyph = "\uE711", FontSize = 10, Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 200, 200, 200)) },
                    Tag = $"{type.TypeCode}|{pkg.Sessions}",
                    Width = 28, Height = 28, Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0)
                };
                btnDelPkg.Click += BtnDeletePackage_Click;
                Grid.SetColumn(btnDelPkg, 2);
                row.Children.Add(btnDelPkg);

                root.Children.Add(row);
            }
        }
        else
        {
            root.Children.Add(new TextBlock
            {
                Text = "ยังไม่มีแพ็กเกจ — กดปุ่ม + เพื่อเพิ่ม",
                FontSize = 13,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180)),
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        card.Child = root;
        return card;
    }

    private static Button CreateIconButton(string glyph, Windows.UI.Color bgColor, string tag)
    {
        return new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 14 },
            Tag = tag,
            Width = 38, Height = 38,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(bgColor),
            BorderThickness = new Thickness(0)
        };
    }

    // ========================================================================
    // CourseType — Add (Popup)
    // ========================================================================

    private async void BtnAddType_Click(object sender, RoutedEventArgs e)
    {
        await ShowTypeDialogAsync(null);
    }

    // ========================================================================
    // CourseType — Edit (Popup)
    // ========================================================================

    private async void BtnEditType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string code) return;
        var type = _types.FirstOrDefault(t => t.TypeCode == code);
        if (type == null) return;
        await ShowTypeDialogAsync(type);
    }

    // ========================================================================
    // CourseType — Dialog (Add / Edit)
    // ========================================================================

    private async System.Threading.Tasks.Task ShowTypeDialogAsync(CourseTypeItem? existing)
    {
        bool isEdit = existing != null;

        var txtCode = new TextBox
        {
            PlaceholderText = "เช่น T4, JR",
            MaxLength = 2,
            Text = existing?.TypeCode ?? "",
            IsEnabled = !isEdit,
            Height = 38, CornerRadius = new CornerRadius(6)
        };
        // แปลงเป็นตัวพิมพ์ใหญ่อัตโนมัติ (แทน CharacterCasing.Upper ที่ Uno ไม่รองรับ)
        txtCode.TextChanging += (s, args) =>
        {
            if (s is TextBox tb)
            {
                var upper = tb.Text?.ToUpperInvariant() ?? "";
                if (tb.Text != upper)
                {
                    var pos = tb.SelectionStart;
                    tb.Text = upper;
                    tb.SelectionStart = pos;
                }
            }
        };

        var txtName = new TextBox
        {
            PlaceholderText = "เช่น Junior Elite",
            Text = existing?.TypeName ?? "",
            Height = 38, CornerRadius = new CornerRadius(6)
        };

        var txtNameThai = new TextBox
        {
            PlaceholderText = "เช่น จูเนียร์อีลิท",
            Text = existing?.TypeNameThai ?? "",
            Height = 38, CornerRadius = new CornerRadius(6)
        };

        var form = new StackPanel { Spacing = 14, MinWidth = 320 };
        form.Children.Add(MakeField("รหัสประเภท (2 ตัวอักษร)", txtCode));
        form.Children.Add(MakeField("ชื่อภาษาอังกฤษ", txtName));
        form.Children.Add(MakeField("ชื่อภาษาไทย (ถ้ามี)", txtNameThai));

        var dialog = new ContentDialog
        {
            Title = isEdit ? $"แก้ไขประเภท {existing!.TypeCode}" : "เพิ่มประเภทคอร์สใหม่",
            Content = form,
            PrimaryButtonText = "บันทึก",
            CloseButtonText = "ยกเลิก",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var code = txtCode.Text?.Trim().ToUpperInvariant() ?? "";
        var name = txtName.Text?.Trim() ?? "";
        var nameThai = txtNameThai.Text?.Trim() ?? "";

        if (code.Length != 2) { _notify?.ShowWarning("รหัสประเภทต้อง 2 ตัวอักษร"); return; }
        if (string.IsNullOrWhiteSpace(name)) { _notify?.ShowWarning("กรุณากรอกชื่อภาษาอังกฤษ"); return; }

        var item = new CourseTypeItem { TypeCode = code, TypeName = name, TypeNameThai = nameThai };
        bool success;

        if (isEdit)
        {
            success = await _db.CourseTypes.UpdateTypeAsync(item);
        }
        else
        {
            if (await _db.CourseTypes.TypeExistsAsync(code))
            {
                _notify?.ShowWarning($"รหัส \"{code}\" มีอยู่แล้ว");
                return;
            }
            success = await _db.CourseTypes.AddTypeAsync(item);
        }

        if (success)
        {
            _notify?.ShowSuccess(isEdit ? "แก้ไขเรียบร้อย" : "เพิ่มประเภทคอร์สเรียบร้อย");
            await RefreshAndReloadCache();
        }
        else
        {
            _notify?.ShowError("ไม่สามารถบันทึกได้");
        }
    }

    // ========================================================================
    // CourseType — Delete
    // ========================================================================

    private async void BtnDeleteType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string code) return;
        var type = _types.FirstOrDefault(t => t.TypeCode == code);
        if (type == null) return;

        if (_notify == null) return;
        var confirmed = await _notify.ShowDeleteConfirmAsync($"ประเภท {type.DisplayName} (รวมทุกแพ็กเกจ)", this.XamlRoot!);
        if (!confirmed) return;

        if (await _db.CourseTypes.DeleteTypeAsync(code))
        {
            _notify.ShowSuccess("ลบเรียบร้อย");
            await RefreshAndReloadCache();
        }
        else
        {
            _notify.ShowError("ไม่สามารถลบได้");
        }
    }

    // ========================================================================
    // Package — Add (Popup)
    // ========================================================================

    private async void BtnAddPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string typeCode) return;

        var cmbSessions = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 38, CornerRadius = new CornerRadius(6)
        };
        foreach (var (content, tag) in new[]
        {
            ("ครั้งละ (1)", "1"), ("4 ครั้ง", "4"), ("8 ครั้ง", "8"),
            ("12 ครั้ง", "12"), ("16 ครั้ง", "16"), ("รายเดือน (0)", "0")
        })
        {
            cmbSessions.Items.Add(new ComboBoxItem { Content = content, Tag = tag });
        }

        var txtPrice = new TextBox
        {
            PlaceholderText = "0",
            Height = 38, CornerRadius = new CornerRadius(6),
            InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } }
        };

        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = MakeField("จำนวนครั้ง", cmbSessions);
        Grid.SetColumn(left, 0);
        row.Children.Add(left);

        var right = MakeField("ราคา (บาท)", txtPrice);
        Grid.SetColumn(right, 1);
        row.Children.Add(right);

        var form = new StackPanel { Spacing = 14, MinWidth = 360 };
        form.Children.Add(row);

        var dialog = new ContentDialog
        {
            Title = $"เพิ่มแพ็กเกจให้ {typeCode}",
            Content = form,
            PrimaryButtonText = "เพิ่มแพ็กเกจ",
            CloseButtonText = "ยกเลิก",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        if (cmbSessions.SelectedItem is not ComboBoxItem sessItem || sessItem.Tag is not string sessTag)
        { _notify?.ShowWarning("กรุณาเลือกจำนวนครั้ง"); return; }
        if (!int.TryParse(sessTag, out var sessions))
        { _notify?.ShowWarning("จำนวนครั้งไม่ถูกต้อง"); return; }
        if (!int.TryParse(txtPrice.Text?.Trim(), out var price) || price <= 0)
        { _notify?.ShowWarning("กรุณากรอกราคาที่ถูกต้อง"); return; }

        var pkg = new CoursePackageItem { TypeCode = typeCode, Sessions = sessions, Price = price };

        if (await _db.CourseTypes.AddPackageAsync(pkg))
        {
            _notify?.ShowSuccess($"เพิ่มแพ็กเกจ {pkg.SessionsDisplay} ฿{price:N0} เรียบร้อย");
            await RefreshAndReloadCache();
        }
        else
        {
            _notify?.ShowError("ไม่สามารถเพิ่มแพ็กเกจได้ (อาจซ้ำ)");
        }
    }

    // ========================================================================
    // Package — Delete
    // ========================================================================

    private async void BtnDeletePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;
        var parts = tag.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var sessions)) return;
        var typeCode = parts[0];

        if (_notify == null) return;
        var sessionText = CoursePricingHelper.GetSessionDisplayText(sessions);
        var confirmed = await _notify.ShowConfirmAsync("ลบแพ็กเกจ", $"ลบแพ็กเกจ {sessionText} ของ {typeCode}?", this.XamlRoot!);
        if (!confirmed) return;

        if (await _db.CourseTypes.DeletePackageAsync(typeCode, sessions))
        {
            _notify.ShowSuccess("ลบแพ็กเกจเรียบร้อย");
            await RefreshAndReloadCache();
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static StackPanel MakeField(string label, UIElement control)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 13,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 107, 114, 128))
        });
        stack.Children.Add(control);
        return stack;
    }

    private async System.Threading.Tasks.Task RefreshAndReloadCache()
    {
        CoursePricingHelper.InvalidateCache();
        await CoursePricingHelper.LoadFromDatabaseAsync(_db);
        await LoadDataAsync();
    }
}
