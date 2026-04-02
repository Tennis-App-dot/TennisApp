using Android.Content;
using Android.Graphics;
using System;

namespace TennisApp.Platforms.Android;

/// <summary>
/// Helper class for validating fonts on Android platform
/// </summary>
public static class FontHelper
{
    private static readonly string[] FontAssetPaths = new[]
    {
        "Assets/Fonts",
        "Fonts",
    };

    /// <summary>
    /// Validate all Thai fonts are available on Android
    /// </summary>
    public static void TestFonts(Context context)
    {
        System.Diagnostics.Debug.WriteLine("Validating Thai fonts on Android...");

        var fontFiles = new[] { "NotoSansThai-Regular.ttf", "NotoSansThai-Bold.ttf", "NotoSansThai-Light.ttf" };

        foreach (var fontFile in fontFiles)
        {
            bool found = false;
            foreach (var basePath in FontAssetPaths)
            {
                try
                {
                    var assetPath = $"{basePath}/{fontFile}";
                    var typeface = Typeface.CreateFromAsset(context.Assets, assetPath);
                    if (typeface != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"   ✅ {fontFile}: Available (path: {assetPath})");
                        found = true;
                        break;
                    }
                }
                catch
                {
                    // Try next path
                }
            }

            if (!found)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ {fontFile}: Not found in any asset path");
            }
        }

        System.Diagnostics.Debug.WriteLine("Font validation complete");
    }
}
