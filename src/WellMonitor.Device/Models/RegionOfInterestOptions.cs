using System.ComponentModel.DataAnnotations;

namespace WellMonitor.Device.Models
{
    /// <summary>
    /// Configuration options for Region of Interest (ROI) processing
    /// Enables focusing OCR processing on specific areas of camera images
    /// </summary>
    public class RegionOfInterestOptions
    {
        /// <summary>
        /// Enable ROI processing to focus on LED display area only
        /// </summary>
        public bool EnableRoi { get; set; } = true;

        /// <summary>
        /// ROI coordinates as percentages of image dimensions
        /// </summary>
        public RoiCoordinates RoiPercent { get; set; } = new();

        /// <summary>
        /// Enable automatic LED detection and ROI positioning
        /// </summary>
        public bool EnableAutoDetection { get; set; } = false;

        /// <summary>
        /// Brightness threshold for LED detection (0-255)
        /// </summary>
        [Range(0, 255)]
        public int LedBrightnessThreshold { get; set; } = 180;

        /// <summary>
        /// Margin pixels to expand around detected LED area
        /// </summary>
        [Range(0, 100)]
        public int ExpansionMargin { get; set; } = 10;

        /// <summary>
        /// Validate ROI configuration
        /// </summary>
        public bool IsValid()
        {
            return RoiPercent != null && RoiPercent.IsValid();
        }
    }

    /// <summary>
    /// ROI coordinates as percentages (0.0 to 1.0) of image dimensions
    /// </summary>
    public class RoiCoordinates
    {
        /// <summary>
        /// X coordinate as percentage from left edge (0.0 = left, 1.0 = right)
        /// </summary>
        [Range(0.0, 1.0)]
        public double X { get; set; } = 0.25;      // 25% from left

        /// <summary>
        /// Y coordinate as percentage from top edge (0.0 = top, 1.0 = bottom)
        /// </summary>
        [Range(0.0, 1.0)]
        public double Y { get; set; } = 0.40;      // 40% from top

        /// <summary>
        /// Width as percentage of image width (0.1 = 10%, 1.0 = 100%)
        /// </summary>
        [Range(0.1, 1.0)]
        public double Width { get; set; } = 0.50;  // 50% width

        /// <summary>
        /// Height as percentage of image height (0.1 = 10%, 1.0 = 100%)
        /// </summary>
        [Range(0.1, 1.0)]
        public double Height { get; set; } = 0.20; // 20% height

        /// <summary>
        /// Validate that coordinates are within valid ranges
        /// </summary>
        public bool IsValid()
        {
            return X >= 0.0 && X <= 1.0 &&
                   Y >= 0.0 && Y <= 1.0 &&
                   Width >= 0.1 && Width <= 1.0 &&
                   Height >= 0.1 && Height <= 1.0 &&
                   (X + Width) <= 1.0 &&
                   (Y + Height) <= 1.0;
        }

        /// <summary>
        /// Calculate pixel coordinates for given image dimensions
        /// </summary>
        public (int x, int y, int width, int height) ToPixelCoordinates(int imageWidth, int imageHeight)
        {
            var x = (int)(imageWidth * X);
            var y = (int)(imageHeight * Y);
            var width = (int)(imageWidth * Width);
            var height = (int)(imageHeight * Height);

            // Ensure coordinates don't exceed image bounds
            x = Math.Max(0, Math.Min(x, imageWidth - 1));
            y = Math.Max(0, Math.Min(y, imageHeight - 1));
            width = Math.Max(1, Math.Min(width, imageWidth - x));
            height = Math.Max(1, Math.Min(height, imageHeight - y));

            return (x, y, width, height);
        }

        /// <summary>
        /// Create ROI coordinates from pixel values
        /// </summary>
        public static RoiCoordinates FromPixelCoordinates(int x, int y, int width, int height, int imageWidth, int imageHeight)
        {
            return new RoiCoordinates
            {
                X = (double)x / imageWidth,
                Y = (double)y / imageHeight,
                Width = (double)width / imageWidth,
                Height = (double)height / imageHeight
            };
        }

        public override string ToString()
        {
            return $"ROI(X:{X:P1}, Y:{Y:P1}, W:{Width:P1}, H:{Height:P1})";
        }
    }
}
