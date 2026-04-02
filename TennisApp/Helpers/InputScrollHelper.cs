using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace TennisApp.Helpers;

/// <summary>
/// Helper ที่ทำให้ ScrollViewer เลื่อนอัตโนมัติเมื่อ TextBox ได้รับ focus
/// ขยาย KeyboardSpacer (พื้นที่ว่างด้านล่าง) ตาม keyboard height จริง
/// แล้วเลื่อน ScrollViewer ให้ TextBox อยู่เหนือ keyboard
/// </summary>
public static class InputScrollHelper
{
    public static void Attach(Page page)
    {
        page.GotFocus += Page_GotFocus;
        page.LostFocus += Page_LostFocus;
    }

    public static void Detach(Page page)
    {
        page.GotFocus -= Page_GotFocus;
        page.LostFocus -= Page_LostFocus;
    }

    private static async void Page_GotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox textBox) return;

        // รอให้ keyboard ขึ้นเต็มที่ + WindowInsets อัพเดท
        await System.Threading.Tasks.Task.Delay(400);

        try
        {
            var scrollViewer = FindParent<ScrollViewer>(textBox);
            if (scrollViewer == null) return;

            var keyboardHeightDp = GetKeyboardHeightDp();
            if (keyboardHeightDp <= 0) return;

            // ขยาย spacer ให้เท่ากับ keyboard height → สร้างพื้นที่สีเทาใต้ card
            var spacer = FindChildByName(scrollViewer, "KeyboardSpacer") as FrameworkElement;
            if (spacer != null)
            {
                spacer.Height = keyboardHeightDp;
            }

            // รอ layout อัพเดทหลัง spacer เปลี่ยนขนาด
            await System.Threading.Tasks.Task.Delay(50);

            var page = FindParent<Page>(textBox);
            if (page == null) return;

            var transformToPage = textBox.TransformToVisual(page);
            var posInPage = transformToPage.TransformPoint(new Windows.Foundation.Point(0, 0));

            var textBoxBottomInPage = posInPage.Y + textBox.ActualHeight;
            var pageHeight = page.ActualHeight;
            var keyboardTop = pageHeight - keyboardHeightDp;

            // ถ้า TextBox อยู่เหนือ keyboard (มี padding 40dp) → ไม่ต้องเลื่อน
            if (textBoxBottomInPage + 40 <= keyboardTop) return;

            var scrollNeeded = textBoxBottomInPage + 60 - keyboardTop;
            var targetOffset = scrollViewer.VerticalOffset + scrollNeeded;
            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

            scrollViewer.ChangeView(null, targetOffset, null, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InputScrollHelper: {ex.Message}");
        }
    }

    private static async void Page_LostFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox) return;

        // รอเล็กน้อยเพื่อเช็คว่า focus ย้ายไป TextBox อื่นหรือไม่
        await System.Threading.Tasks.Task.Delay(200);

        try
        {
            var keyboardHeightDp = GetKeyboardHeightDp();
            // keyboard ยังเปิดอยู่ (focus ย้ายไป TextBox อื่น) → ไม่ต้อง reset
            if (keyboardHeightDp > 0) return;

            if (sender is not Page page) return;
            var scrollViewer = FindChild<ScrollViewer>(page);
            if (scrollViewer == null) return;

            var spacer = FindChildByName(scrollViewer, "KeyboardSpacer") as FrameworkElement;
            if (spacer != null)
            {
                spacer.Height = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InputScrollHelper LostFocus: {ex.Message}");
        }
    }

    private static double GetKeyboardHeightDp()
    {
#if ANDROID
        var heightPx = TennisApp.Droid.MainActivity.CurrentKeyboardHeight;
        var density = TennisApp.Droid.MainActivity.ScreenDensity;
        return density > 0 ? heightPx / density : 0;
#else
        return 0;
#endif
    }

    private static T? FindParent<T>(DependencyObject child) where T : class
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T found) return found;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : class
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private static DependencyObject? FindChildByName(DependencyObject parent, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name) return fe;
            var result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
