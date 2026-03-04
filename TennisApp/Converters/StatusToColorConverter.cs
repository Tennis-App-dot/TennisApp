using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace TennisApp.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value?.ToString() ?? "booked";
        return status switch
        {
            "booked" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243)),    // #2196F3 Blue
            "in_use" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)),      // #FF9800 Orange
            "completed" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),   // #4CAF50 Green
            "cancelled" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),   // #F44336 Red
            _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 158, 158, 158))            // #9E9E9E Grey
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
