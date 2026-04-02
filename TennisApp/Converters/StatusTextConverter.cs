using Microsoft.UI.Xaml.Data;

namespace TennisApp.Converters;

public sealed class StatusTextConverter : IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, string language)
        => value?.ToString() == "1" ? "พร้อมใช้งาน" : "กำลังปิดปรับปรุง";
    public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        => (value?.ToString() == "พร้อมใช้งาน") ? "1" : "0";
}
