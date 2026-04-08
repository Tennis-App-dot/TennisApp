using TennisApp.Services;
using Windows.Globalization;

namespace TennisApp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public DatabaseService DatabaseService { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        // ✅ Force Light theme — ทุกหน้า UI ออกแบบสำหรับ Light mode
        this.RequestedTheme = ApplicationTheme.Light;

#if ANDROID
        ApplicationLanguages.PrimaryLanguageOverride = "th-TH";
#endif

        // Initialize SQLite for Database Integration
        SQLitePCL.Batteries_V2.Init();

        // ✅ สร้าง DatabaseService แบบ lightweight (ไม่สร้าง DAO / ไม่ block main thread)
        DatabaseService = new DatabaseService();

        ConfigureFonts();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging((context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(context.HostingEnvironment.IsDevelopment()
                            ? LogLevel.Information
                            : LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(DatabaseService);
                    services.AddSingleton<FontService>();

                    // ✅ Register ViewModels — DI จะ inject DatabaseService ให้อัตโนมัติ
                    services.AddTransient<TennisApp.Presentation.ViewModels.CourtPageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.TrainerPageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.TraineePageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.CoursePageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.BookingPageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.RegisterCoursePageViewModel>();
                    services.AddTransient<TennisApp.Presentation.ViewModels.CourtUsageLogPageViewModel>();
                })
            );

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        System.Diagnostics.Debug.WriteLine("🚀 Creating Shell...");
        var shell = new Presentation.Shell();
        System.Diagnostics.Debug.WriteLine("✅ Shell created successfully");

        MainWindow.Content = shell;
        System.Diagnostics.Debug.WriteLine("📱 Shell set as MainWindow.Content");

        MainWindow.Activate();
        System.Diagnostics.Debug.WriteLine("🎯 MainWindow.Activate() complete");

        // ✅ Initialize Database on background thread (ไม่ block UI)
        _ = InitializeDatabaseAsync();
    }

    private void ConfigureFonts()
    {
        try
        {
            if (Application.Current?.Resources == null)
            {
                System.Diagnostics.Debug.WriteLine("Application.Resources not available");
                return;
            }

            var resources = Application.Current.Resources;

            // Font paths
            var regularFont = "ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai";
            var boldFont = "ms-appx:///Assets/Fonts/NotoSansThai-Bold.ttf#Noto Sans Thai";
            var lightFont = "ms-appx:///Assets/Fonts/NotoSansThai-Light.ttf#Noto Sans Thai";
            var fallbackFont = "ms-appx:///Assets/Fonts/NotoSansThai-Regular.ttf#Noto Sans Thai, Leelawadee UI, Tahoma, Arial Unicode MS";

            // Create FontFamily objects
            var thaiFontFamily = new FontFamily(regularFont);
            var thaiFontFamilyBold = new FontFamily(boldFont);
            var thaiFontFamilyLight = new FontFamily(lightFont);
            var appThaiFontFamily = new FontFamily(fallbackFont);

            // Add font resources
            resources["ThaiFontFamily"] = thaiFontFamily;
            resources["ThaiFontFamilyBold"] = thaiFontFamilyBold;
            resources["ThaiFontFamilyLight"] = thaiFontFamilyLight;
            resources["AppThaiFont"] = appThaiFontFamily;

            // System theme font overrides
            resources["DefaultFontFamily"] = appThaiFontFamily;
            resources["ContentControlThemeFontFamily"] = appThaiFontFamily;
            resources["ControlContentThemeFontFamily"] = appThaiFontFamily;
            resources["ContentDialogThemeFontFamily"] = appThaiFontFamily;
            resources["TextControlThemeFontFamily"] = appThaiFontFamily;

            // Create global control styles
            CreateControlStyles(resources, thaiFontFamily);

            // Create Tennis App specific styles
            CreateAppSpecificStyles(resources, thaiFontFamily, thaiFontFamilyBold);

            System.Diagnostics.Debug.WriteLine("Font configuration completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Font configuration error: {ex.Message}");
        }
    }

    private static void CreateControlStyles(ResourceDictionary resources, FontFamily fontFamily)
    {
        var controlTypes = new[]
        {
            (typeof(TextBlock), "BaseTextBlockStyle", 14.0),
            (typeof(Button), "BaseButtonStyle", 14.0)
        };

        foreach (var (type, baseName, fontSize) in controlTypes)
        {
            var baseStyle = new Style(type);
            baseStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, fontFamily));
            baseStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, fontSize));
            resources[baseName] = baseStyle;

            var defaultStyle = new Style(type) { BasedOn = baseStyle };
            resources[type] = defaultStyle;
        }

        // Additional control styles — ensure readable text on any device
        var additionalControls = new[]
        {
            typeof(TextBox), typeof(ComboBox), typeof(RadioButton),
            typeof(CheckBox), typeof(ListView), typeof(DatePicker)
        };

        foreach (var type in additionalControls)
        {
            var style = new Style(type);
            style.Setters.Add(new Setter(Control.FontFamilyProperty, fontFamily));
            if (type != typeof(ListView))
            {
                style.Setters.Add(new Setter(Control.FontSizeProperty, 14.0));
            }
            // ✅ Ensure text is dark on light backgrounds for all input controls
            style.Setters.Add(new Setter(Control.ForegroundProperty,
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 28, 27, 31))));
            resources[type] = style;
        }

        // NavigationView style
        var navStyle = new Style(typeof(Microsoft.UI.Xaml.Controls.NavigationView));
        navStyle.Setters.Add(new Setter(Microsoft.UI.Xaml.Controls.NavigationView.FontFamilyProperty, fontFamily));
        resources[typeof(Microsoft.UI.Xaml.Controls.NavigationView)] = navStyle;

        // ContentDialog style
        var dialogStyle = new Style(typeof(ContentDialog));
        dialogStyle.Setters.Add(new Setter(ContentDialog.FontFamilyProperty, fontFamily));
        resources[typeof(ContentDialog)] = dialogStyle;
    }

    private static void CreateAppSpecificStyles(ResourceDictionary resources, FontFamily regularFont, FontFamily boldFont)
    {
        var baseTextBlockStyle = (Style)resources["BaseTextBlockStyle"];
        var baseButtonStyle = (Style)resources["BaseButtonStyle"];

        var appStyles = new[]
        {
            ("PageTitleStyle", boldFont, 28.0, Microsoft.UI.Text.FontWeights.SemiBold),
            ("SectionHeaderStyle", boldFont, 18.0, Microsoft.UI.Text.FontWeights.Medium),
            ("CourtNameStyle", regularFont, 22.0, Microsoft.UI.Text.FontWeights.SemiBold),
            ("MenuItemTextStyle", regularFont, 16.0, Microsoft.UI.Text.FontWeights.Normal)
        };

        foreach (var (name, font, size, weight) in appStyles)
        {
            var style = new Style(typeof(TextBlock)) { BasedOn = baseTextBlockStyle };
            style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, font));
            style.Setters.Add(new Setter(TextBlock.FontSizeProperty, size));
            style.Setters.Add(new Setter(TextBlock.FontWeightProperty, weight));
            if (name == "MenuItemTextStyle")
            {
                style.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Black)));
            }
            resources[name] = style;
        }

        // Action Button Style
        var actionButtonStyle = new Style(typeof(Button)) { BasedOn = baseButtonStyle };
        actionButtonStyle.Setters.Add(new Setter(Button.FontFamilyProperty, regularFont));
        actionButtonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 16.0));
        actionButtonStyle.Setters.Add(new Setter(Button.FontWeightProperty, Microsoft.UI.Text.FontWeights.SemiBold));
        actionButtonStyle.Setters.Add(new Setter(Button.HeightProperty, 45.0));
        actionButtonStyle.Setters.Add(new Setter(Button.CornerRadiusProperty, new CornerRadius(6)));
        resources["ActionButtonStyle"] = actionButtonStyle;
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Starting Database Initialization (async)...");
            // ✅ สร้างตารางทั้งหมดบน background thread
            await DatabaseService.InitializeAsync();

            // ✅ โหลดข้อมูลประเภทคอร์ส + ราคาจาก DB เข้า cache
            await Helpers.CoursePricingHelper.LoadFromDatabaseAsync(DatabaseService);

            // ✅ Seed ข้อมูลตัวอย่างถ้า database ว่าง (สำหรับดู UI)
            // 💡 เปลี่ยนเป็น ResetAndSeedAsync เพื่อ force reset ข้อมูลใหม่
            // 💡 เปลี่ยนกลับเป็น SeedIfEmptyAsync เมื่อดู UI เสร็จแล้ว
            await Services.SeedDataService.SeedIfEmptyAsync(DatabaseService);

            System.Diagnostics.Debug.WriteLine($"Database Path: {DatabaseService.GetDatabasePath()}");
            System.Diagnostics.Debug.WriteLine($"Database Ready: {DatabaseService.IsDatabaseReady()}");
            System.Diagnostics.Debug.WriteLine("✅ Database initialization complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Database error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }
    }
}
