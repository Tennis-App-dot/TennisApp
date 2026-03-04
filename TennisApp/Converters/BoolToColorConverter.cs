using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace TennisApp.Converters;

/// <summary>
/// Converter ที่แปลง Boolean เป็นสี
/// true = สีปกติ (Black)
/// false = สีแดง (Orange) สำหรับ "รอจัดสรรสนาม"
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isAssigned)
        {
            // ถ้าสนามถูกจัดสรรแล้ว → สีดำ
            // ถ้ายังไม่ได้จัดสรร (false) → สีส้ม/แดง
            return isAssigned 
                ? new SolidColorBrush(Colors.Black) 
                : new SolidColorBrush(Colors.DarkOrange);
        }
        
        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
