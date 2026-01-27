# Thai Font Fix for System Dialogs

## Problem
The `ShowConfirm` and `ShowMessage` methods in CourtPage.xaml.cs were creating ContentDialog instances without explicitly applying Thai fonts, causing Thai text to not display properly.

## Root Cause
When creating ContentDialog programmatically in C#, the dialog doesn't automatically inherit the global font styles from App.xaml.cs. The ContentDialog needs explicit font assignment.

## Solution Applied

### Updated ShowMessage Method
```csharp
private async Task ShowMessage(string title, string content)
{
    var dlg = new ContentDialog
    {
        Title = title,
        Content = content,
        CloseButtonText = "ตกลง",
        XamlRoot = this.XamlRoot
    };

    // Apply Thai font from application resources
    if (Application.Current?.Resources != null)
    {
        if (Application.Current.Resources.TryGetValue("ThaiFontFamily", out var fontResource))
        {
            dlg.FontFamily = fontResource as Microsoft.UI.Xaml.Media.FontFamily;
        }
    }

    await dlg.ShowAsync();
}
```

### Updated ShowConfirm Method
```csharp
private async Task<bool> ShowConfirm(string title, string content)
{
    var dlg = new ContentDialog
    {
        Title = title,
        Content = content,
        PrimaryButtonText = "ใช่",
        CloseButtonText = "ไม่",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.XamlRoot
    };

    // Apply Thai font from application resources
    if (Application.Current?.Resources != null)
    {
        if (Application.Current.Resources.TryGetValue("ThaiFontFamily", out var fontResource))
        {
            dlg.FontFamily = fontResource as Microsoft.UI.Xaml.Media.FontFamily;
        }
    }

    var result = await dlg.ShowAsync();
    return result == ContentDialogResult.Primary;
}
```

## How It Works

1. After creating the ContentDialog, the code retrieves the "ThaiFontFamily" resource from Application.Current.Resources
2. This resource was configured in App.xaml.cs during application startup
3. The font is then explicitly assigned to the dialog's FontFamily property
4. This ensures all text in the dialog (title, content, buttons) uses the Thai font

## Testing

Build Status: SUCCESS

To verify:
1. Run the app
2. Click any button that shows a dialog (Add, Edit, Delete)
3. Thai text should display correctly in all dialogs
4. Dialog buttons should show Thai text properly

## Related Files
- TennisApp\Presentation\Pages\CourtPage.xaml.cs (updated)
- TennisApp\App.xaml.cs (font configuration source)
- TennisApp\Presentation\Dialogs\CourtFormDialog.xaml (uses similar approach)

## Note
This same pattern should be applied to any other places where ContentDialog is created programmatically in C# rather than defined in XAML.
