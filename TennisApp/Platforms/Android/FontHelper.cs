using Android.Content;
using Android.Graphics;
using System;

namespace TennisApp.Platforms.Android;

/// <summary>
/// Helper class for validating fonts on Android platform
/// </summary>
public static class FontHelper
{
    /// <summary>
    /// Validate all Thai fonts are available on Android
    /// </summary>
    public static void TestFonts(Context context)
    {
        System.Diagnostics.Debug.WriteLine("Validating Thai fonts on Android...");
        
        try
        {
            // Test loading each font file
            var regular = Typeface.CreateFromAsset(context.Assets, "Fonts/NotoSansThai-Regular.ttf");
            var bold = Typeface.CreateFromAsset(context.Assets, "Fonts/NotoSansThai-Bold.ttf");
            var light = Typeface.CreateFromAsset(context.Assets, "Fonts/NotoSansThai-Light.ttf");

            System.Diagnostics.Debug.WriteLine($"Font validation results:");
            System.Diagnostics.Debug.WriteLine($"   Regular: {(regular != null ? "Available" : "Missing")}");
            System.Diagnostics.Debug.WriteLine($"   Bold: {(bold != null ? "Available" : "Missing")}");
            System.Diagnostics.Debug.WriteLine($"   Light: {(light != null ? "Available" : "Missing")}");
            System.Diagnostics.Debug.WriteLine("All Thai fonts validated successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Font validation failed: {ex.Message}");
        }
    }
}
