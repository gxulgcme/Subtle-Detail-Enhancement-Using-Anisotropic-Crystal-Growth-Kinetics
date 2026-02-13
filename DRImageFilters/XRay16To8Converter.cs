using System;
using System.Diagnostics;
using OpenCvSharp;
namespace DRImageFilters
{
    /// <summary>
    /// Optimized 16-bit X-ray image to 8-bit converter (single image version)
    /// Utilizes adaptive window level/width, LUT optimization, and intelligent contrast enhancement
    /// </summary>
    public static class XRay16To8Converter
    {
        private const int LUT_SIZE = 65536;

        /// <summary>
        /// Converts a 16-bit X-ray image to an 8-bit image
        /// </summary>
        /// <param name="input16Bit">16-bit single-channel grayscale image (CV_16UC1)</param>
        /// <param name="parameters">Conversion parameters, defaults to optimal practice configuration</param>
        /// <returns>8-bit single-channel grayscale image (CV_8UC1)</returns>
        public static Mat Convert(Mat input16Bit, ConversionParameters parameters = null)
        {
            if (input16Bit == null)
                throw new ArgumentNullException(nameof(input16Bit));

            ValidateInputImage(input16Bit);

            var param = parameters ?? ConversionParameters.Optimal;

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                // 1. Calculate adaptive window level and width
                (int windowLevel, int windowWidth) = CalculateAdaptiveWindow(input16Bit, param);

                // 2. Create optimized LUT
                using (Mat lut = CreateOptimizedLUT(windowLevel, windowWidth, param))
                {
                    // 3. Apply LUT conversion
                    Mat output8Bit = ApplyLUT(input16Bit, lut);

                    // 4. Apply post-processing enhancement
                    if (param.ApplyPostProcessing)
                    {
                        ApplyPostProcessing(output8Bit, param);
                    }

                    sw.Stop();

                    if (param.EnableLogging)
                    {
                        Console.WriteLine($"[XRay16To8] Conversion completed: " +
                            $"Dimensions={input16Bit.Width}x{input16Bit.Height}, " +
                            $"Window Level={windowLevel}, Window Width={windowWidth}, " +
                            $"Elapsed Time={sw.ElapsedMilliseconds}ms");
                    }

                    return output8Bit;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Image conversion failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates adaptive window level and width (core algorithm)
        /// </summary>
        private static (int WindowLevel, int WindowWidth) CalculateAdaptiveWindow(
            Mat image16Bit, ConversionParameters param)
        {
            // Obtain basic statistical information
            image16Bit.MinMaxLoc(out double globalMin, out double globalMax);

            // Utilize block statistics for improved robustness (targeting uneven exposure)
            if (param.UseBlockStatistics)
            {
                return CalculateWindowWithBlocks(image16Bit, globalMin, globalMax, param);
            }

            // Standard adaptive algorithm
            return CalculateStandardWindow(image16Bit, globalMin, globalMax, param);
        }

        /// <summary>
        /// Standard adaptive window level and width calculation
        /// </summary>
        private static (int WindowLevel, int WindowWidth) CalculateStandardWindow(
            Mat image16Bit, double globalMin, double globalMax, ConversionParameters param)
        {
            // Calculate histogram (utilizing finer resolution than standard)
            const int HIST_BINS = 4096; // 4K bins for 16-bit range
            using (Mat hist = new Mat())
            {
                Rangef[] ranges = { new Rangef(0, 65536) };
                Cv2.CalcHist(
                    new Mat[] { image16Bit },
                    new int[] { 0 },
                    null,
                    hist,
                    1,
                    new int[] { HIST_BINS },
                    ranges,
                    true,
                    false
                );

                // Calculate effective pixel range (excluding extremes)
                long totalPixels = image16Bit.Width * image16Bit.Height;
                long lowerCount = (long)(totalPixels * param.LowerPercentile / 100.0);
                long upperCount = (long)(totalPixels * param.UpperPercentile / 100.0);

                long cumulative = 0;
                int lowBin = 0;
                int highBin = HIST_BINS - 1;

                // Locate lower bound quantile
                for (int i = 0; i < HIST_BINS; i++)
                {
                    cumulative += (long)hist.Get<float>(i);
                    if (cumulative >= lowerCount)
                    {
                        lowBin = Math.Max(0, i - 1); // Slightly wider range
                        break;
                    }
                }

                // Locate upper bound quantile
                cumulative = 0;
                for (int i = HIST_BINS - 1; i >= 0; i--)
                {
                    cumulative += (long)hist.Get<float>(i);
                    if (cumulative >= (totalPixels - upperCount))
                    {
                        highBin = Math.Min(HIST_BINS - 1, i + 1); // Slightly wider range
                        break;
                    }
                }

                // Convert bin indices to actual grayscale values
                int lowValue = (int)(lowBin * 65535.0 / (HIST_BINS - 1));
                int highValue = (int)(highBin * 65535.0 / (HIST_BINS - 1));

                // Apply intelligent adjustment
                (lowValue, highValue) = AdjustWindowBounds(
                    lowValue, highValue, globalMin, globalMax, param);

                // Calculate window level and width
                int windowLevel = (lowValue + highValue) / 2;
                int windowWidth = Math.Max(1, highValue - lowValue);

                // Apply window width constraints
                windowWidth = ApplyWindowWidthConstraints(windowWidth, param);

                return (windowLevel, windowWidth);
            }
        }

        /// <summary>
        /// Block statistics adaptive window level and width calculation (targeting uneven exposure)
        /// </summary>
        private static (int WindowLevel, int WindowWidth) CalculateWindowWithBlocks(
            Mat image16Bit, double globalMin, double globalMax, ConversionParameters param)
        {
            const int BLOCK_SIZE = 256;
            int blocksX = (image16Bit.Width + BLOCK_SIZE - 1) / BLOCK_SIZE;
            int blocksY = (image16Bit.Height + BLOCK_SIZE - 1) / BLOCK_SIZE;

            int totalBlocks = blocksX * blocksY;
            double[] blockMins = new double[totalBlocks];
            double[] blockMaxs = new double[totalBlocks];
            double[] blockMeans = new double[totalBlocks];

            // Calculate statistical information for each block
            int blockIndex = 0;
            for (int y = 0; y < blocksY; y++)
            {
                int blockY = y * BLOCK_SIZE;
                int blockHeight = Math.Min(BLOCK_SIZE, image16Bit.Height - blockY);

                for (int x = 0; x < blocksX; x++)
                {
                    int blockX = x * BLOCK_SIZE;
                    int blockWidth = Math.Min(BLOCK_SIZE, image16Bit.Width - blockX);

                    using (Mat block = image16Bit[new Rect(blockX, blockY, blockWidth, blockHeight)])
                    {
                        block.MinMaxLoc(out double blockMin, out double blockMax);
                        Scalar mean = block.Mean();

                        blockMins[blockIndex] = blockMin;
                        blockMaxs[blockIndex] = blockMax;
                        blockMeans[blockIndex] = mean.Val0;
                        blockIndex++;
                    }
                }
            }

            // Calculate median values across all blocks
            Array.Sort(blockMins);
            Array.Sort(blockMaxs);
            Array.Sort(blockMeans);

            double medianMin = blockMins[totalBlocks / 2];
            double medianMax = blockMaxs[totalBlocks / 2];
            double medianMean = blockMeans[totalBlocks / 2];

            // Calculate window based on median values and global range
            double lowerBound = Math.Max(medianMin, globalMin + (medianMean - globalMin) * 0.3);
            double upperBound = Math.Min(medianMax, globalMax - (globalMax - medianMean) * 0.3);

            int lowValue = (int)lowerBound;
            int highValue = (int)upperBound;

            // Apply intelligent adjustment
            (lowValue, highValue) = AdjustWindowBounds(
                lowValue, highValue, globalMin, globalMax, param);

            // Calculate window level and width
            int windowLevel = (lowValue + highValue) / 2;
            int windowWidth = Math.Max(1, highValue - lowValue);

            // Apply window width constraints
            windowWidth = ApplyWindowWidthConstraints(windowWidth, param);

            return (windowLevel, windowWidth);
        }

        /// <summary>
        /// Intelligent adjustment of window boundaries
        /// </summary>
        private static (int LowValue, int HighValue) AdjustWindowBounds(
            int lowValue, int highValue, double globalMin, double globalMax, ConversionParameters param)
        {
            // Ensure minimum span
            int minSpan = (int)((globalMax - globalMin) * 0.05); // At least 5% of global range
            if (highValue - lowValue < minSpan)
            {
                int center = (lowValue + highValue) / 2;
                lowValue = Math.Max((int)globalMin, center - minSpan / 2);
                highValue = Math.Min((int)globalMax, center + minSpan / 2);
            }

            // Apply safety margin
            int safetyMargin = (int)((highValue - lowValue) * 0.05);
            lowValue = Math.Max(0, lowValue - safetyMargin);
            highValue = Math.Min(65535, highValue + safetyMargin);

            return (lowValue, highValue);
        }

        /// <summary>
        /// Applies window width constraints
        /// </summary>
        private static int ApplyWindowWidthConstraints(int windowWidth, ConversionParameters param)
        {
            // Apply minimum window width
            windowWidth = Math.Max(windowWidth, param.MinWindowWidth);

            // Apply maximum window width
            if (param.MaxWindowWidth > 0)
            {
                windowWidth = Math.Min(windowWidth, param.MaxWindowWidth);
            }

            return windowWidth;
        }

        /// <summary>
        /// Creates optimized LUT (utilizing interpolation to reduce quantization error)
        /// </summary>
        private static Mat CreateOptimizedLUT(int windowLevel, int windowWidth, ConversionParameters param)
        {
            Mat lut = new Mat(1, LUT_SIZE, MatType.CV_8UC1);

            // Calculate window boundaries
            int lowerBound = windowLevel - windowWidth / 2;
            int upperBound = windowLevel + windowWidth / 2;

            // Ensure boundary validity
            lowerBound = Math.Max(0, lowerBound);
            upperBound = Math.Min(65535, upperBound);

            if (upperBound <= lowerBound)
            {
                upperBound = Math.Min(65535, lowerBound + 1);
            }

            unsafe
            {
                byte* lutPtr = (byte*)lut.Data;
                double scale = 255.0 / (upperBound - lowerBound);

                // Create smooth LUT using linear interpolation
                for (int i = 0; i < LUT_SIZE; i++)
                {
                    if (i <= lowerBound)
                    {
                        lutPtr[i] = 0;
                    }
                    else if (i >= upperBound)
                    {
                        lutPtr[i] = 255;
                    }
                    else
                    {
                        // Add gamma correction factor
                        double normalized = (double)(i - lowerBound) / (upperBound - lowerBound);

                        if (Math.Abs(param.GammaCorrection - 1.0) > 0.01)
                        {
                            normalized = Math.Pow(normalized, param.GammaCorrection);
                        }

                        // Add contrast enhancement
                        if (param.ContrastEnhancement > 0)
                        {
                            normalized = ApplyContrastCurve(normalized, param.ContrastEnhancement);
                        }

                        lutPtr[i] = (byte)Math.Min(255, Math.Max(0, normalized * 255));
                    }
                }
            }

            return lut;
        }

        /// <summary>
        /// Applies contrast curve
        /// </summary>
        private static double ApplyContrastCurve(double value, double strength)
        {
            // S-curve for contrast enhancement
            return 1.0 / (1.0 + Math.Exp(-strength * (value - 0.5) * 10)) * 0.5 + 0.25;
        }

        /// <summary>
        /// Applies LUT conversion
        /// </summary>
        private static Mat ApplyLUT(Mat input16Bit, Mat lut)
        {
            Mat output8Bit = new Mat(input16Bit.Rows, input16Bit.Cols, MatType.CV_8UC1);
            Cv2.LUT(input16Bit, lut, output8Bit);
            return output8Bit;
        }

        /// <summary>
        /// Applies post-processing enhancement
        /// </summary>
        private static void ApplyPostProcessing(Mat image8Bit, ConversionParameters param)
        {
            // 1. Apply local contrast enhancement (CLAHE)
            if (param.UseCLAHE)
            {
                using (CLAHE clahe = Cv2.CreateCLAHE(
                    clipLimit: param.CLAHEClipLimit,
                    tileGridSize: param.CLAHETileGridSize))
                {
                    clahe.Apply(image8Bit, image8Bit);
                }
            }

            // 2. Apply sharpening (optional)
            if (param.SharpeningAmount > 0)
            {
                ApplySharpening(image8Bit, param.SharpeningAmount);
            }

            // 3. Apply noise suppression (optional)
            if (param.NoiseReduction > 0)
            {
                ApplyNoiseReduction(image8Bit, param.NoiseReduction);
            }
        }

        /// <summary>
        /// Applies sharpening
        /// </summary>
        private static void ApplySharpening(Mat image, double amount)
        {
            using (Mat blurred = new Mat())
            {
                // Apply slight Gaussian blur
                Cv2.GaussianBlur(image, blurred, new Size(0, 0), 1.0);

                // Calculate sharpened image: original + (original - blurred) * amount
                Cv2.AddWeighted(image, 1.0 + amount, blurred, -amount, 0, image);
            }
        }

        /// <summary>
        /// Applies noise suppression
        /// </summary>
        private static void ApplyNoiseReduction(Mat image, int strength)
        {
            int kernelSize = 3 + (strength - 1) * 2;
            Cv2.MedianBlur(image, image, kernelSize);
        }

        /// <summary>
        /// Validates input image
        /// </summary>
        private static void ValidateInputImage(Mat image)
        {
            if (image.Empty())
                throw new ArgumentException("Input image is empty", nameof(image));

            if (image.Type() != MatType.CV_16UC1)
                throw new ArgumentException(
                    $"Input image must be a 16-bit single-channel grayscale image (CV_16UC1), actual type: {image.Type()}",
                    nameof(image));

            if (image.Width <= 0 || image.Height <= 0)
                throw new ArgumentException($"Input image dimensions are invalid: {image.Width}x{image.Height}",
                    nameof(image));
        }

        /// <summary>
        /// Conversion parameter configuration
        /// </summary>
        public class ConversionParameters
        {
            // Window level/width calculation parameters
            public double LowerPercentile { get; set; } = 1.0;      // Lower quantile (1%)
            public double UpperPercentile { get; set; } = 99.0;     // Upper quantile (99%)
            public int MinWindowWidth { get; set; } = 1000;         // Minimum window width
            public int MaxWindowWidth { get; set; } = 0;            // Maximum window width (0=no limit)
            public bool UseBlockStatistics { get; set; } = true;    // Utilize block statistics

            // LUT parameters
            public double GammaCorrection { get; set; } = 1.0;      // Gamma correction (1.0=no correction)
            public double ContrastEnhancement { get; set; } = 0.2;  // Contrast enhancement intensity

            // Post-processing parameters
            public bool ApplyPostProcessing { get; set; } = true;   // Apply post-processing
            public bool UseCLAHE { get; set; } = true;              // Utilize CLAHE
            public double CLAHEClipLimit { get; set; } = 2.0;       // CLAHE contrast limit
            public Size CLAHETileGridSize { get; set; } = new Size(8, 8);
            public double SharpeningAmount { get; set; } = 0.3;     // Sharpening intensity
            public int NoiseReduction { get; set; } = 1;            // Noise reduction level (0-3)

            // Other parameters
            public bool EnableLogging { get; set; } = true;         // Enable logging

            /// <summary>
            /// Optimal practice configuration (optimized for industrial X-ray images)
            /// </summary>
            public static ConversionParameters Optimal => new ConversionParameters
            {
                LowerPercentile = 0.5,      // More aggressively exclude dark noise
                UpperPercentile = 99.5,     // More aggressively exclude bright saturation
                MinWindowWidth = 2000,      // Ensure sufficient dynamic range
                UseBlockStatistics = true,  // More robust for uneven exposure
                GammaCorrection = 1.1,      // Slight enhancement of dark detail
                ContrastEnhancement = 0.15, // Moderate contrast enhancement
                ApplyPostProcessing = true,
                UseCLAHE = true,
                CLAHEClipLimit = 2.5,       // Stronger local contrast
                CLAHETileGridSize = new Size(8, 8),
                SharpeningAmount = 0.2,     // Slight sharpening
                NoiseReduction = 1,         // Slight noise suppression
                EnableLogging = true
            };

            /// <summary>
            /// High-quality display configuration
            /// </summary>
            public static ConversionParameters HighQuality => new ConversionParameters
            {
                LowerPercentile = 0.2,
                UpperPercentile = 99.8,
                MinWindowWidth = 1000,
                UseBlockStatistics = true,
                GammaCorrection = 1.05,
                ContrastEnhancement = 0.1,
                ApplyPostProcessing = true,
                UseCLAHE = true,
                CLAHEClipLimit = 2.0,
                CLAHETileGridSize = new Size(12, 12),
                SharpeningAmount = 0.1,
                NoiseReduction = 2,
                EnableLogging = true
            };

            /// <summary>
            /// Fast conversion configuration (performance prioritized)
            /// </summary>
            public static ConversionParameters Fast => new ConversionParameters
            {
                LowerPercentile = 2.0,
                UpperPercentile = 98.0,
                MinWindowWidth = 5000,
                UseBlockStatistics = false, // Disable block statistics for speed
                GammaCorrection = 1.0,
                ContrastEnhancement = 0.0,
                ApplyPostProcessing = false, // Disable post-processing for speed
                EnableLogging = false
            };

            /// <summary>
            /// Custom configuration
            /// </summary>
            public static ConversionParameters Create(
                double lowerPercentile = 1.0,
                double upperPercentile = 99.0,
                bool useBlockStatistics = true,
                double gamma = 1.0,
                bool useCLAHE = true)
            {
                return new ConversionParameters
                {
                    LowerPercentile = lowerPercentile,
                    UpperPercentile = upperPercentile,
                    UseBlockStatistics = useBlockStatistics,
                    GammaCorrection = gamma,
                    UseCLAHE = useCLAHE
                };
            }
        }
    }

    /// <summary>
    /// Usage example
    /// </summary>
    public static class XRay16To8ConverterExampleUsage
    {
        public static void Demonstrate()
        {
            // Example 1: Acquire 16-bit X-ray image from data source
            Mat xrayImage16Bit = AcquireXRayImageFromDevice();

            try
            {
                // Method 1: Utilize optimal practice configuration
                Mat resultOptimal = XRay16To8Converter.Convert(
                    xrayImage16Bit,
                    XRay16To8Converter.ConversionParameters.Optimal);

                // Process resultOptimal...
                DisplayImage(resultOptimal);
                resultOptimal.Dispose();

                // Method 2: Utilize custom configuration
                var customParams = new XRay16To8Converter.ConversionParameters
                {
                    LowerPercentile = 0.5,
                    UpperPercentile = 99.5,
                    GammaCorrection = 1.2,
                    UseCLAHE = true,
                    CLAHEClipLimit = 3.0
                };

                Mat resultCustom = XRay16To8Converter.Convert(xrayImage16Bit, customParams);

                // Process resultCustom...
                DisplayImage(resultCustom);
                resultCustom.Dispose();

                // Method 3: Fast conversion (no post-processing)
                Mat resultFast = XRay16To8Converter.Convert(
                    xrayImage16Bit,
                    XRay16To8Converter.ConversionParameters.Fast);

                // Process resultFast...
                DisplayImage(resultFast);
                resultFast.Dispose();
            }
            finally
            {
                xrayImage16Bit.Dispose();
            }
        }

        private static Mat AcquireXRayImageFromDevice()
        {
            // Code for acquiring image from X-ray device should be here
            // Example: Create a simulated 16-bit X-ray image
            int width = 2048;
            int height = 1536;
            Mat image = new Mat(height, width, MatType.CV_16UC1);

            // Simulate X-ray image: bright center, dark edges
            unsafe
            {
                ushort* ptr = (ushort*)image.Data;
                int centerX = width / 2;
                int centerY = height / 2;
                double maxDist = Math.Sqrt(centerX * centerX + centerY * centerY);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dist = Math.Sqrt(
                            (x - centerX) * (x - centerX) +
                            (y - centerY) * (y - centerY));

                        double normalized = 1.0 - dist / maxDist;
                        normalized = Math.Max(0, normalized);

                        // Simulate typical X-ray grayscale range: 5000-45000
                        ushort value = (ushort)(5000 + normalized * 40000);

                        // Add some noise
                        value = (ushort)Math.Max(0, Math.Min(65535,
                            value + (new Random().Next(-100, 100))));

                        ptr[y * width + x] = value;
                    }
                }
            }

            return image;
        }

        private static void DisplayImage(Mat image)
        {
            // Code for displaying or processing image should be here
            Console.WriteLine($"Image display: {image.Width}x{image.Height}, Type: {image.Type()}");

            // Example: Calculate and display statistical information
            image.MinMaxLoc(out double minVal, out double maxVal);
            Scalar mean = image.Mean();
            Scalar stddev = new Scalar();
            Cv2.MeanStdDev(image, out Scalar mean2, out stddev);

            Console.WriteLine($"  Pixel range: [{minVal:F0}, {maxVal:F0}]");
            Console.WriteLine($"  Mean value: {mean.Val0:F1}, Standard deviation: {stddev.Val0:F1}");
        }
    }
}