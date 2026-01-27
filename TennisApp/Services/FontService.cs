using Microsoft.UI.Xaml;

namespace TennisApp.Services;

/// <summary>
/// Service for managing application fonts and typography
/// </summary>
public class FontService
{
    // Font Family Constants
    public const string ThaiFontRegular = "ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai";
    public const string ThaiFontBold = "ms-appx:///Assets/Fonts/NotoSansThai-Bold.ttf#Noto Sans Thai";
    public const string ThaiFontLight = "ms-appx:///Assets/Fonts/NotoSansThai-Light.ttf#Noto Sans Thai";
    public const string ThaiFontFallback = "ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai, Leelawadee UI, Tahoma, Arial Unicode MS";

    /// <summary>
    /// Get Thai font family by weight
    /// </summary>
    public static string GetThaiFontFamily(FontWeight weight = FontWeight.Regular)
    {
        return weight switch
        {
            FontWeight.Bold => ThaiFontBold,
            FontWeight.Light => ThaiFontLight,
            _ => ThaiFontRegular
        };
    }

    /// <summary>
    /// Get Thai font with system fallbacks
    /// </summary>
    public static string GetThaiFontWithFallback()
    {
        return ThaiFontFallback;
    }

    /// <summary>
    /// Apply Thai font to a FrameworkElement
    /// </summary>
    public static void ApplyThaiFont(FrameworkElement element, FontWeight weight = FontWeight.Regular)
    {
        try
        {
            if (element is Control control)
            {
                control.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(GetThaiFontFamily(weight));
                System.Diagnostics.Debug.WriteLine($"Applied Thai font to {element.GetType().Name}: {weight}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply Thai font to {element.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply Thai font to multiple elements
    /// </summary>
    public static void ApplyThaiFontToElements(IEnumerable<FrameworkElement> elements, FontWeight weight = FontWeight.Regular)
    {
        foreach (var element in elements)
        {
            ApplyThaiFont(element, weight);
        }
    }

    /// <summary>
    /// Validate font availability
    /// </summary>
    public static async Task<bool> ValidateFontsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Validating Thai font availability...");

            var fonts = new[] { ThaiFontRegular, ThaiFontBold, ThaiFontLight };
            var results = new List<bool>();

            foreach (var font in fonts)
            {
                try
                {
                    var fontFamily = new Microsoft.UI.Xaml.Media.FontFamily(font);
                    results.Add(true);
                    System.Diagnostics.Debug.WriteLine($"Font available: {font}");
                }
                catch
                {
                    results.Add(false);
                    System.Diagnostics.Debug.WriteLine($"Font unavailable: {font}");
                }
            }

            var allAvailable = results.All(r => r);
            System.Diagnostics.Debug.WriteLine($"Font validation result: {(allAvailable ? "All fonts available" : "Some fonts missing")}");

            return allAvailable;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Font validation error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get font resource key by type
    /// </summary>
    public static string GetFontResourceKey(FontResourceType type)
    {
        return type switch
        {
            FontResourceType.Primary => "ThaiFontFamily",
            FontResourceType.Bold => "ThaiFontFamilyBold",
            FontResourceType.Light => "ThaiFontFamilyLight",
            FontResourceType.WithFallback => "AppThaiFont",
            _ => "ThaiFontFamily"
        };
    }

    public enum FontWeight
    {
        Regular,
        Bold,
        Light
    }

    public enum FontResourceType
    {
        Primary,
        Bold,
        Light,
        WithFallback
    }
}
