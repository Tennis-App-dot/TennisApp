using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace TennisApp.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

#if DEBUG
        // Validate Thai fonts are available
        TennisApp.Platforms.Android.FontHelper.TestFonts(this);
#endif

        base.OnCreate(savedInstanceState);
    }
}
