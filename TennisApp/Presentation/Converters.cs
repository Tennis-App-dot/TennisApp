using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace TennisApp.Presentation;

public sealed class StatusTextConverter : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, string language)
        => value?.ToString() == "1" ? "พร้อมใช้งาน" : "กำลังปิดปรับปรุง";
    public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        => (value?.ToString() == "พร้อมใช้งาน") ? "1" : "0";
}

public sealed class StatusBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Colors.ForestGreen);
    private static readonly SolidColorBrush Red = new(Colors.IndianRed);

    public object Convert(object value, System.Type targetType, object parameter, string language)
        => value?.ToString() == "1" ? Green : Red;

    public object ConvertBack(object value, System.Type targetType, object parameter, string language) => "1";
}
