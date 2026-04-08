using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace TennisApp.Droid;

[Activity(
    MainLauncher = true,
    Theme = "@style/Theme.App.Starting",
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    public static new MainActivity? Current { get; private set; }
    public const int CameraRequestCode = 9001;

    /// <summary>
    /// ความสูงของ keyboard ปัจจุบัน (pixels) — 0 = keyboard ปิด
    /// </summary>
    public static int CurrentKeyboardHeight { get; private set; }

    /// <summary>
    /// ความหนาแน่นหน้าจอ (density) สำหรับแปลง px → dp
    /// </summary>
    public static float ScreenDensity { get; private set; } = 1f;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;

        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

#if DEBUG
        TennisApp.Platforms.Android.FontHelper.TestFonts(this);
#endif

        base.OnCreate(savedInstanceState);

        ScreenDensity = Resources?.DisplayMetrics?.Density ?? 1f;

        // ✅ ซ่อน Status Bar — แสดงแบบ full-screen
        if (Window != null)
        {
#pragma warning disable CA1422 // SetStatusBarColor is obsoleted on Android 35+
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
#pragma warning restore CA1422

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller != null)
            {
                controller.Hide(WindowInsetsCompat.Type.StatusBars());
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            }
        }

        // ✅ วัด keyboard height จริงผ่าน WindowInsets listener
        SetupKeyboardListener();
    }

    private void SetupKeyboardListener()
    {
        var rootView = FindViewById(Android.Resource.Id.Content);
        if (rootView == null) return;

        ViewCompat.SetOnApplyWindowInsetsListener(rootView, new KeyboardInsetsListener());
    }

    private class KeyboardInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
        {
            if (v == null || insets == null)
                return ViewCompat.OnApplyWindowInsets(v!, insets!) ?? insets!;

            var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            var navInsets = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars());

            // keyboard height = IME inset - navigation bar inset (เอาเฉพาะส่วน keyboard)
            var keyboardHeight = Math.Max(0, (imeInsets?.Bottom ?? 0) - (navInsets?.Bottom ?? 0));
            CurrentKeyboardHeight = keyboardHeight;

            System.Diagnostics.Debug.WriteLine(
                $"⌨️ Keyboard: {keyboardHeight}px ({keyboardHeight / ScreenDensity:F0}dp)");

            return ViewCompat.OnApplyWindowInsets(v, insets) ?? insets;
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == CameraRequestCode)
        {
            Services.ImagePickerService.OnCameraResult(resultCode == Result.Ok);
        }
    }
}
