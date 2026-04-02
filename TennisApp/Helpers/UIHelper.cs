using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace TennisApp.Helpers;

/// <summary>
/// Helper class สำหรับจัดการ loading states และ error handling
/// </summary>
public static class UIHelper
{
    /// <summary>
    /// แสดง loading dialog
    /// </summary>
    public static async Task<ContentDialog> ShowLoadingAsync(string message = "กำลังโหลด...")
    {
        var dialog = new ContentDialog
        {
            Title = "กำลังประมวลผล",
            Content = new StackPanel
            {
                Children = 
                {
                    new ProgressRing { IsActive = true, Width = 40, Height = 40 },
                    new TextBlock { Text = message, Margin = new Microsoft.UI.Xaml.Thickness(0, 16, 0, 0) }
                }
            }
        };

        // ไม่รอ ShowAsync() เพื่อให้สามารถปิดได้ภายหลัง
        _ = dialog.ShowAsync();
        
        return dialog;
    }

    /// <summary>
    /// แสดง error message
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message, Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "ตกลง",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// แสดง success message
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message, Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "ตกลง",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// แสดง confirmation dialog
    /// </summary>
    public static async Task<bool> ShowConfirmationAsync(string title, string message, Microsoft.UI.Xaml.XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "ใช่",
            CloseButtonText = "ไม่",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Parse hex color string (e.g., "#4A148C" or "4A148C") to Windows.UI.Color
    /// </summary>
    public static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber));
        return Windows.UI.Color.FromArgb(255, 158, 158, 158);
    }
}
