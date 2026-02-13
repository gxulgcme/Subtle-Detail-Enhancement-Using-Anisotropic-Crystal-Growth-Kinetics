using OpenCvSharp;

namespace DRImageFilters
{
    public class ImageMetricsCalculator
    {
        /// <summary>
        /// Calculate PSNR (Peak Signal-to-Noise Ratio) for 16-bit grayscale images
        /// </summary>
        /// <param name="originalImage">Original image</param>
        /// <param name="processedImage">Processed image</param>
        /// <returns>PSNR value</returns>
        public static double ComputePSNR(Mat originalImage, Mat processedImage)
        {
            // Verify input images
            if (originalImage.Size() != processedImage.Size())
                throw new ArgumentException("Image sizes do not match");

            if (originalImage.Type() != MatType.CV_16UC1 || processedImage.Type() != MatType.CV_16UC1)
                throw new ArgumentException("Images must be 16-bit grayscale images");

            int width = originalImage.Width;
            int height = originalImage.Height;
            long totalPixels = width * height;

            // Calculate MSE (Mean Squared Error)
            double mse = 0.0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double originalVal = originalImage.At<ushort>(y, x);
                    double processedVal = processedImage.At<ushort>(y, x);
                    double diff = originalVal - processedVal;
                    mse += diff * diff;
                }
            }
            mse /= totalPixels;

            // Avoid division by zero error
            if (mse == 0)
                return double.PositiveInfinity;

            // Calculate PSNR
            double maxPixelValue = 65535; // Maximum value for 16-bit images
            double psnr = 10 * Math.Log10((maxPixelValue * maxPixelValue) / mse);

            return psnr;
        }

        /// <summary>
        /// Calculate SSIM (Structural Similarity Index) for 16-bit grayscale images
        /// </summary>
        /// <param name="originalImage">Original image</param>
        /// <param name="processedImage">Processed image</param>
        /// <returns>SSIM value</returns>
        public static double ComputeSSIM(Mat originalImage, Mat processedImage)
        {
            // Verify input images
            if (originalImage.Size() != processedImage.Size())
                throw new ArgumentException("Image sizes do not match");

            if (originalImage.Type() != MatType.CV_16UC1 || processedImage.Type() != MatType.CV_16UC1)
                throw new ArgumentException("Images must be 16-bit grayscale images");

            int width = originalImage.Width;
            int height = originalImage.Height;
            long totalPixels = width * height;

            // Calculate means
            double meanOriginal = 0, meanProcessed = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    meanOriginal += originalImage.At<ushort>(y, x);
                    meanProcessed += processedImage.At<ushort>(y, x);
                }
            }
            meanOriginal /= totalPixels;
            meanProcessed /= totalPixels;

            // Calculate variances and covariance
            double varOriginal = 0, varProcessed = 0, covariance = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double origVal = originalImage.At<ushort>(y, x);
                    double procVal = processedImage.At<ushort>(y, x);

                    varOriginal += (origVal - meanOriginal) * (origVal - meanOriginal);
                    varProcessed += (procVal - meanProcessed) * (procVal - meanProcessed);
                    covariance += (origVal - meanOriginal) * (procVal - meanProcessed);
                }
            }

            varOriginal /= (totalPixels - 1);
            varProcessed /= (totalPixels - 1);
            covariance /= (totalPixels - 1);

            // SSIM constants
            double maxPixelValue = 65535;
            double c1 = Math.Pow(0.01 * maxPixelValue, 2);
            double c2 = Math.Pow(0.03 * maxPixelValue, 2);

            // Calculate SSIM
            double numerator = (2 * meanOriginal * meanProcessed + c1) * (2 * covariance + c2);
            double denominator = (meanOriginal * meanOriginal + meanProcessed * meanProcessed + c1)
                               * (varOriginal + varProcessed + c2);

            return numerator / denominator;
        }

        /// <summary>
        /// Calculate Spatial Frequency (SF) for 16-bit grayscale images
        /// </summary>
        /// <param name="image">Input image</param>
        /// <returns>Spatial frequency value</returns>
        public static double ComputeSpatialFrequency(Mat image)
        {
            if (image.Type() != MatType.CV_16UC1)
                throw new ArgumentException("Image must be 16-bit grayscale image");

            int width = image.Width;
            int height = image.Height;
            long totalPixels = width * height;

            // Calculate horizontal gradient
            double horizontalGradient = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    double diff = image.At<ushort>(y, x) - image.At<ushort>(y, x + 1);
                    horizontalGradient += diff * diff;
                }
            }
            horizontalGradient = Math.Sqrt(horizontalGradient / totalPixels);

            // Calculate vertical gradient
            double verticalGradient = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double diff = image.At<ushort>(y, x) - image.At<ushort>(y + 1, x);
                    verticalGradient += diff * diff;
                }
            }
            verticalGradient = Math.Sqrt(verticalGradient / totalPixels);

            // Calculate spatial frequency
            double spatialFrequency = Math.Sqrt(horizontalGradient * horizontalGradient
                                              + verticalGradient * verticalGradient);

            return spatialFrequency;
        }

        /// <summary>
        /// Calculate all image quality metrics
        /// </summary>
        /// <param name="originalImage">Original image</param>
        /// <param name="processedImage">Processed image</param>
        /// <returns>Tuple containing all metrics</returns>
        public static (double psnr, double ssim, double sfOriginal, double sfProcessed)
            ComputeAllMetrics(Mat originalImage, Mat processedImage)
        {
            double psnr = ComputePSNR(originalImage, processedImage);
            double ssim = ComputeSSIM(originalImage, processedImage);
            double sfOriginal = ComputeSpatialFrequency(originalImage);
            double sfProcessed = ComputeSpatialFrequency(processedImage);

            return (psnr, ssim, sfOriginal, sfProcessed);
        }
    }
}