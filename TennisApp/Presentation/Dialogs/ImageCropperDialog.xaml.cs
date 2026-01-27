using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using SkiaSharp;

namespace TennisApp.Presentation.Dialogs;

public enum CropShape
{
    Circle,
    Rectangle
}

public sealed partial class ImageCropperDialog : ContentDialog
{
    private byte[] _originalImageData;
    private double _imageOffsetX;
    private double _imageOffsetY;
    private readonly int _cropWidth;
    private readonly int _cropHeight;
    private readonly CropShape _cropShape;
    private double _imageWidth;
    private double _imageHeight;

    public byte[]? CroppedImageData { get; private set; }

    // Constructor for circular crop (backward compatibility)
    public ImageCropperDialog(byte[] imageData) 
        : this(imageData, CropShape.Circle, 400, 400)
    {
    }

    // Constructor with crop shape and size options
    public ImageCropperDialog(byte[] imageData, CropShape cropShape, int cropWidth, int cropHeight)
    {
        _originalImageData = imageData;
        _cropShape = cropShape;
        _cropWidth = cropWidth;
        _cropHeight = cropHeight;
        
        this.InitializeComponent();
        
        // Update UI based on crop shape
        UpdateCropShapeUI();
        
        _ = LoadImageAsync();
    }

    private void UpdateCropShapeUI()
    {
        // Set dimensions
        CropCanvas.Width = _cropWidth;
        CropCanvas.Height = _cropHeight;
        CropperGrid.Width = _cropWidth;
        CropperGrid.Height = _cropHeight;
        
        // Show/hide appropriate border and overlay
        if (_cropShape == CropShape.Circle)
        {
            // Circle mode: Show overlay with cutout + white border
            CircleOverlayContainer.Visibility = Visibility.Visible;
            CircleOverlayContainer.Width = _cropWidth;
            CircleOverlayContainer.Height = _cropHeight;
            
            CircleCutout.Width = _cropWidth;
            CircleCutout.Height = _cropHeight;
            
            CircleBorder.Visibility = Visibility.Visible;
            CircleBorder.Width = _cropWidth;
            CircleBorder.Height = _cropHeight;
            
            RectangleBorderCanvas.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Rectangle mode: Hide overlay, show only white border
            CircleOverlayContainer.Visibility = Visibility.Collapsed;
            CircleBorder.Visibility = Visibility.Collapsed;
            
            RectangleBorderCanvas.Visibility = Visibility.Visible;
            RectangleBorderCanvas.Width = _cropWidth;
            RectangleBorderCanvas.Height = _cropHeight;
            RectangleBorder.Width = _cropWidth;
            RectangleBorder.Height = _cropHeight;
        }
    }

    private async Task LoadImageAsync()
    {
        try
        {
            // Load image using SkiaSharp to get dimensions
            using var skBitmap = SKBitmap.Decode(_originalImageData);
            if (skBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Failed to decode image");
                return;
            }

            var originalWidth = skBitmap.Width;
            var originalHeight = skBitmap.Height;

            // Calculate scaling to fit in crop area
            var scale = Math.Max(_cropWidth / (double)originalWidth, _cropHeight / (double)originalHeight);
            _imageWidth = originalWidth * scale;
            _imageHeight = originalHeight * scale;

            // Set image properties
            SourceImage.Width = _imageWidth;
            SourceImage.Height = _imageHeight;

            // Center the image
            _imageOffsetX = (_cropWidth - _imageWidth) / 2;
            _imageOffsetY = (_cropHeight - _imageHeight) / 2;
            ImageTransform.X = _imageOffsetX;
            ImageTransform.Y = _imageOffsetY;

            // Load bitmap for display
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(_originalImageData.AsBuffer());
            stream.Seek(0);
            
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            SourceImage.Source = bitmap;

            System.Diagnostics.Debug.WriteLine($"✅ Image loaded: {originalWidth}x{originalHeight} -> {_imageWidth}x{_imageHeight}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading image: {ex.Message}");
        }
    }

    private void SourceImage_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        // Update position based on drag
        _imageOffsetX += e.Delta.Translation.X;
        _imageOffsetY += e.Delta.Translation.Y;

        // Apply constraints to keep image within bounds
        var minX = _cropWidth - _imageWidth;
        var minY = _cropHeight - _imageHeight;

        _imageOffsetX = Math.Clamp(_imageOffsetX, minX, 0);
        _imageOffsetY = Math.Clamp(_imageOffsetY, minY, 0);

        ImageTransform.X = _imageOffsetX;
        ImageTransform.Y = _imageOffsetY;
    }

    private async void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            CroppedImageData = await CropImageAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Image cropped: {CroppedImageData?.Length ?? 0} bytes");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cropping image: {ex.Message}");
            CroppedImageData = null;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void SecondaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        CroppedImageData = null;
    }

    private async Task<byte[]> CropImageAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Load original image
                using var originalBitmap = SKBitmap.Decode(_originalImageData);
                if (originalBitmap == null)
                {
                    throw new Exception("Failed to decode original image");
                }

                var originalWidth = originalBitmap.Width;
                var originalHeight = originalBitmap.Height;

                // Calculate scale factor between display and original
                var displayScale = Math.Max(_cropWidth / (double)originalWidth, _cropHeight / (double)originalHeight);

                // Calculate crop bounds in original image coordinates
                var cropX = (int)Math.Max(0, -_imageOffsetX / displayScale);
                var cropY = (int)Math.Max(0, -_imageOffsetY / displayScale);
                var cropWidth = (int)(_cropWidth / displayScale);
                var cropHeight = (int)(_cropHeight / displayScale);

                // Ensure crop bounds are within image
                cropX = Math.Min(cropX, originalWidth - cropWidth);
                cropY = Math.Min(cropY, originalHeight - cropHeight);
                cropWidth = Math.Min(cropWidth, originalWidth - cropX);
                cropHeight = Math.Min(cropHeight, originalHeight - cropY);

                // Create crop rectangle
                var cropRect = new SKRectI(cropX, cropY, cropX + cropWidth, cropY + cropHeight);

                // Create cropped bitmap
                using var croppedBitmap = new SKBitmap(cropWidth, cropHeight);
                originalBitmap.ExtractSubset(croppedBitmap, cropRect);

                // Resize to target size if needed using new API
                var targetInfo = new SKImageInfo(_cropWidth, _cropHeight);
                var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                using var resizedBitmap = croppedBitmap.Resize(targetInfo, samplingOptions);
                if (resizedBitmap == null)
                {
                    throw new Exception("Failed to resize image");
                }

                // Encode to PNG
                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                
                return data.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SkiaSharp crop error: {ex.Message}");
                throw;
            }
        });
    }
}
