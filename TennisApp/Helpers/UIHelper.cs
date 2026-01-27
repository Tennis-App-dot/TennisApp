using System;
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
}
