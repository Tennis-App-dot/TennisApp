using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using TennisApp.Helpers;

namespace TennisApp.Presentation.Converters;

/// <summary>
/// Converter สำหรับแปลง ImageData (byte[]) เป็น BitmapImage
/// </summary>
public class ImageDataToBitmapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is byte[] imageData && imageData.Length > 0)
        {
            // สร้าง BitmapImage จาก byte array (แบบ async)
            var task = ImageHelper.CreateBitmapFromBytesAsync(imageData);
            task.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    // อัปเดต UI บน UI thread
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        // Result จะถูกใช้โดย Binding
                    });
                }
            });

            // Return placeholder ก่อน
            return new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
        }

        // Default image
        return new BitmapImage(new Uri("ms-appx:///Assets/Courts/court1.jpg"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
