using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TennisApp.Services;

/// <summary>
/// Centralized notification service for the entire app.
/// - Success/Warning/Error → InfoBar (auto-dismiss 3s)
/// - Confirm → ContentDialog (blocks until user responds)
/// - Critical Error → ContentDialog (blocks)
/// </summary>
public class NotificationService
{
    private InfoBar? _infoBar;
    private DispatcherTimer? _autoDismissTimer;

    /// <summary>
    /// Initialize with the InfoBar control from Shell.xaml
    /// </summary>
    public void Initialize(InfoBar infoBar)
    {
        _infoBar = infoBar;

        _autoDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _autoDismissTimer.Tick += (s, e) =>
        {
            _autoDismissTimer.Stop();
            if (_infoBar != null)
            {
                _infoBar.IsOpen = false;
            }
        };
    }

    // ========================================================================
    // InfoBar Notifications (non-blocking, auto-dismiss)
    // ========================================================================

    /// <summary>
    /// Show success notification (green, auto-dismiss 3s)
    /// </summary>
    public void ShowSuccess(string message, string title = "สำเร็จ")
    {
        ShowInfoBar(title, message, InfoBarSeverity.Success);
    }

    /// <summary>
    /// Show warning notification (yellow, auto-dismiss 3s)
    /// </summary>
    public void ShowWarning(string message, string title = "คำเตือน")
    {
        ShowInfoBar(title, message, InfoBarSeverity.Warning);
    }

    /// <summary>
    /// Show error notification (red, auto-dismiss 3s)
    /// </summary>
    public void ShowError(string message, string title = "เกิดข้อผิดพลาด")
    {
        ShowInfoBar(title, message, InfoBarSeverity.Error);
    }

    /// <summary>
    /// Show informational notification (blue, auto-dismiss 3s)
    /// </summary>
    public void ShowInfo(string message, string title = "ข้อมูล")
    {
        ShowInfoBar(title, message, InfoBarSeverity.Informational);
    }

    private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        if (_infoBar == null) return;

        _autoDismissTimer?.Stop();

        _infoBar.Title = title;
        _infoBar.Message = message;
        _infoBar.Severity = severity;
        _infoBar.IsOpen = true;

        _autoDismissTimer?.Start();
    }

    // ========================================================================
    // ContentDialog (blocking — for confirm + critical error)
    // ========================================================================

    /// <summary>
    /// Show confirmation dialog. Returns true if user confirms.
    /// </summary>
    public async Task<bool> ShowConfirmAsync(string title, string content, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Show delete confirmation dialog (Android-style).
    /// มีไอคอนเตือนสีแดง + ปุ่มลบสีแดง + ปุ่มยกเลิกสีเทา
    /// </summary>
    public async Task<bool> ShowDeleteConfirmAsync(string itemName, XamlRoot xamlRoot)
    {
        var contentPanel = new StackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // ไอคอนเตือน (วงกลมแดง + ไอคอนถังขยะ)
        var iconBorder = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(32),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 254, 226, 226)), // #FEE2E2
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = "\uE74D",
                FontSize = 28,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38)), // #DC2626
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        contentPanel.Children.Add(iconBorder);

        // หัวข้อ
        var titleText = new TextBlock
        {
            Text = "ยืนยันการลบ",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 46)), // #1A1A2E
            HorizontalAlignment = HorizontalAlignment.Center
        };
        contentPanel.Children.Add(titleText);

        // ข้อความอธิบาย
        var messagePanel = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        messagePanel.Children.Add(new TextBlock
        {
            Text = $"ต้องการลบ {itemName} ใช่หรือไม่?",
            FontSize = 15,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 107, 114, 128)), // #6B7280
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        messagePanel.Children.Add(new TextBlock
        {
            Text = "การดำเนินการนี้ไม่สามารถย้อนกลับได้",
            FontSize = 13,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 156, 163, 175)), // #9CA3AF
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });
        contentPanel.Children.Add(messagePanel);

        // ปุ่ม (เรียงแนวตั้ง — แบบ Android)
        bool confirmed = false;

        var deleteButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 48,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 220, 38, 38)), // #DC2626
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderThickness = new Thickness(0),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new FontIcon { Glyph = "\uE74D", FontSize = 16 },
                    new TextBlock
                    {
                        Text = "ลบ",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    }
                }
            }
        };

        var cancelButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 48,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 249, 250, 251)), // #F9FAFB
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 107, 114, 128)), // #6B7280
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 231, 235)), // #E5E7EB
            BorderThickness = new Thickness(1),
            Content = new TextBlock
            {
                Text = "ยกเลิก",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            }
        };

        var buttonPanel = new StackPanel { Spacing = 10, Margin = new Thickness(0, 8, 0, 0) };
        buttonPanel.Children.Add(deleteButton);
        buttonPanel.Children.Add(cancelButton);
        contentPanel.Children.Add(buttonPanel);

        var dialog = new ContentDialog
        {
            Content = contentPanel,
            XamlRoot = xamlRoot,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 229, 231, 235)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24, 20, 24, 20)
        };

        // ซ่อน default buttons ของ ContentDialog
        // ใช้ custom buttons แทน
        deleteButton.Click += (s, e) => { confirmed = true; dialog.Hide(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Hide(); };

        await dialog.ShowAsync();
        return confirmed;
    }

    /// <summary>
    /// Show critical error dialog that blocks the user.
    /// </summary>
    public async Task ShowCriticalErrorAsync(string title, string content, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "ตกลง",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// Show confirmation dialog (static — ใช้ได้แม้ไม่มี NotificationService instance)
    /// </summary>
    public static async Task<bool> ConfirmAsync(string title, string content, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Get the singleton instance from any Page via Shell
    /// </summary>
    public static NotificationService? GetFromPage(Microsoft.UI.Xaml.Controls.Page page)
    {
        try
        {
            // วิธีที่ 1: Walk up the visual tree (Windows layout)
            if (page.Frame?.Parent is NavigationView navView &&
                navView.Parent is Grid grid &&
                grid.Parent is Presentation.Shell shell)
            {
                return shell.NotificationService;
            }

            // วิธีที่ 2: Search visual tree recursively (Android layout อาจต่างกัน)
            var element = page.Frame?.Parent as FrameworkElement;
            while (element != null)
            {
                if (element is Presentation.Shell foundShell)
                {
                    return foundShell.NotificationService;
                }
                element = element.Parent as FrameworkElement;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ NotificationService.GetFromPage failed: {ex.Message}");
        }

        System.Diagnostics.Debug.WriteLine("⚠️ NotificationService.GetFromPage: ไม่พบ Shell — _notify จะเป็น null");
        return null;
    }
}
