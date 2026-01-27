using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace TennisApp.Converters;

public class AlternatingRowConverter : IValueConverter
{
    private static int _counter = 0;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var index = _counter++;
        if (_counter > 1000) _counter = 0; // Reset to avoid overflow

        return index % 2 == 0 
            ? new SolidColorBrush(Microsoft.UI.Colors.White)
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 249, 249, 249));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
