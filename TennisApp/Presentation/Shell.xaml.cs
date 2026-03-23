using Microsoft.UI.Xaml.Controls;
using TennisApp.Presentation.Pages;
using TennisApp.Services;
using System.Collections.Generic;

namespace TennisApp.Presentation;

public sealed partial class Shell : UserControl
{
    private static readonly Dictionary<string, System.Type> PageMappings = new()
    {
        ["Court"] = typeof(CourtPage),
        ["Trainer"] = typeof(TrainerPage),
        ["Trainee"] = typeof(TraineePage),
        ["Course"] = typeof(CoursePage),
        ["RegisterCourse"] = typeof(RegisterCoursePage),
        ["PaidBooking"] = typeof(PaidBookingPage),
        ["CourseBooking"] = typeof(CourseBookingPage),
        ["UsageLog"] = typeof(CourtUsageLogPage),
        ["Reports"] = typeof(ReportsPage)
    };

    public NotificationService NotificationService { get; } = new();

    public Shell()
    {
        InitializeComponent();
        NotificationService.Initialize(GlobalInfoBar);
        System.Diagnostics.Debug.WriteLine("Shell constructor - initializing Shell + NotificationService");
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("NavView_Loaded - loading NavigationView");
        
        if (NavView.MenuItems is { Count: > 0 })
        {
            System.Diagnostics.Debug.WriteLine($"Found {NavView.MenuItems.Count} menu items");
            NavView.SelectedItem = NavView.MenuItems[0];
            System.Diagnostics.Debug.WriteLine("Selected first menu item, navigating to CourtPage");
            NavigateToTag("Court");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No menu items found in NavView.MenuItems");
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"NavView_ItemInvoked - menu clicked: {args.InvokedItem}");
        
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            System.Diagnostics.Debug.WriteLine($"Found Tag: {tag}");
            NavigateToTag(tag);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Tag or InvokedItemContainer not found");
        }
    }

    private void NavigateToTag(string tag)
    {
        System.Diagnostics.Debug.WriteLine($"NavigateToTag: {tag}");
        
        try
        {
            if (PageMappings.TryGetValue(tag, out var pageType))
            {
                System.Diagnostics.Debug.WriteLine($"Navigating to {pageType.Name}");
                ContentFrame.Navigate(pageType);
                System.Diagnostics.Debug.WriteLine($"✅ Navigation to {pageType.Name} successful");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Unknown Tag: {tag}");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
        }
    }
}
