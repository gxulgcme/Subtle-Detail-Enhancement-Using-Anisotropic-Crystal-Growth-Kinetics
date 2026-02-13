using System.Diagnostics;
using Dicom;
using Dicom.Imaging;
using MiniExcelLibs;

namespace DRImageFilters
{
    /// <summary>
    /// Data model for image quality assessment results
    /// </summary>
    public class ImageQualityMetrics
    {
        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public DateTime ProcessDate { get; set; }
        public double SSIM { get; set; }
        public double PSNR { get; set; }
        public double SF_Original { get; set; }
        public double SF_Enhanced { get; set; }
    }
    public class Program
    {
        /// <summary>
        /// Save results to Excel file
        /// </summary>
        /// <param name="results">List of quality assessment results</param>
        /// <param name="excelOutputPath">Excel output path</param>
        private static void SaveResultsToExcel(List<ImageQualityMetrics> results, string excelOutputPath)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(excelOutputPath));
            // Save data using MiniExcel
            MiniExcel.SaveAs(excelOutputPath, results, overwriteFile: true);
        }
        /// <summary>
        /// Process a single DICOM file and perform quality assessment
        /// </summary>
        /// <param name="inputFilePath">Input DICOM file path</param>
        /// <param name="outputDirectory">Output directory</param>
        /// <returns>Image quality metrics</returns>
        public static ImageQualityMetrics ProcessDicomFileWithEnhanceByCrystallographicEnhancer(
            string inputFilePath,
            string outputDirectory,
            float enhancementStrength = 3.25f,
        int growthLevels = 3)
        {
            // Create output directory
            Directory.CreateDirectory(outputDirectory);

            // Read original DICOM file
            DicomFile inputDicomFile = DicomFile.Open(inputFilePath);
            DicomImage inputDicomImage = new DicomImage(inputDicomFile.Dataset);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var enhancedResults = CrystallographicEnhancer.EnhanceByCrystallographicEnhancer(inputDicomImage,
                enhancementStrength,
        growthLevels);
            sw.Stop();
            // Calculate image quality metrics
            var metrics = ImageMetricsCalculator.ComputeAllMetrics(enhancedResults.Item1, enhancedResults.Item2);
            // Generate output file name
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string fileeExtension = Path.GetExtension(inputFilePath);
            string outputFilePath = Path.Combine(outputDirectory, $"{fileName}_enhanced{fileeExtension}");
            DicomFile outputDicomFile = new DicomFile(enhancedResults.Item3.Dataset);
            outputDicomFile.Save(outputFilePath);
            using (var image8bit = XRay16To8Converter.Convert(enhancedResults.Item1))
            {
                image8bit.SaveImage(outputFilePath+"_unenhanced_auto_win_level.jpg");
            }
            using (var image8bit = XRay16To8Converter.Convert(enhancedResults.Item2))
            {
                image8bit.SaveImage(outputFilePath + "_enhanced_auto_win_level.jpg");
            }
            // Clean up resources
            enhancedResults.Item1.Dispose();
            enhancedResults.Item2.Dispose();
            enhancedResults.Item3 = default(DicomImage);
            outputDicomFile = default(DicomFile);
            // Return statistical results
            return new ImageQualityMetrics
            {
                FileName = Path.GetFileName(inputFilePath),
                Width = inputDicomImage.Width,
                Height = inputDicomImage.Height,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                ProcessDate = DateTime.Now,
                SSIM = metrics.ssim,
                PSNR = metrics.psnr,
                SF_Original = metrics.sfOriginal,
                SF_Enhanced = metrics.sfProcessed
            };
        }
        /// <summary>
        /// Batch process DICOM file directory
        /// </summary>
        /// <param name="inputDirectory">Input directory</param>
        /// <param name="outputDirectory">Output directory</param>
        /// <param name="excelOutputPath">Excel output path</param>
        public static void BatchProcessDicomFilesWithEnhanceByCrystallographicEnhancer(string inputDirectory, string outputDirectory, string excelOutputPath,
            float enhancementStrength = 3.25f,
        int growthLevels = 3)
        {
            if (!Directory.Exists(inputDirectory))
                throw new DirectoryNotFoundException($"Input directory does not exist: {inputDirectory}");

            // Get all DICOM files
            string[] dicomFiles = Directory.GetFiles(inputDirectory, "*.dcm")
                .Concat(Directory.GetFiles(inputDirectory, "*.diconde"))
                .ToArray();

            if (dicomFiles.Length == 0)
                throw new FileNotFoundException($"No DICOM files found in directory {inputDirectory}");

            List<ImageQualityMetrics> results = new List<ImageQualityMetrics>();
            int processedCount = 0;

            Console.WriteLine($"Starting processing {dicomFiles.Length} DICOM files...");

            foreach (string dicomFile in dicomFiles)
            {
                try
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(dicomFile)}");

                    var metrics = ProcessDicomFileWithEnhanceByCrystallographicEnhancer(
                        dicomFile,
                        outputDirectory,
            enhancementStrength,
        growthLevels);
                    results.Add(metrics);

                    processedCount++;
                    Console.WriteLine($"Completed processing: {Path.GetFileName(dicomFile)} - PSNR: {metrics.PSNR:F2}, SSIM: {metrics.SSIM:F4}, SF_Enhanced: {metrics.SF_Enhanced:F4}, SF_Original: {metrics.SF_Original:F4}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process file: {Path.GetFileName(dicomFile)} - Error: {ex.Message}");
                }
            }

            // Save results to Excel
            if (results.Count > 0)
            {
                SaveResultsToExcel(results, excelOutputPath);
                Console.WriteLine($"Processing completed! Successfully processed {processedCount}/{dicomFiles.Length} files");
                Console.WriteLine($"Results saved to: {excelOutputPath}");
            }
            else
            {
                Console.WriteLine("No successfully processed files");
            }
        }
        static void Main(string[] args)
        {
            int growthLevels = 3;
            for (int i = 0; i < 5; i++)
            {
                float enhancementStrength = 5.0f + (float)i * 1.0f;
                try
                {
                    string testedDataType = "DR";
                    // Configure paths
                    string inputDirectory = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TestedDICOM", testedDataType);
                    string outputDirectory = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TestedDICOM", testedDataType + "-outputs" + enhancementStrength.ToString());
                    string excelOutputPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "TestedDICOM", testedDataType + "-outputs" + enhancementStrength.ToString(), $"quality_metrics_{testedDataType}.xlsx");
                    // Execute batch processing
                    BatchProcessDicomFilesWithEnhanceByCrystallographicEnhancer(
                        inputDirectory, 
                        outputDirectory,
                        excelOutputPath,
                        enhancementStrength,
                        growthLevels);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Test program execution failed: {ex.Message}");
                    Console.WriteLine($"Detailed error: {ex}");
                }
            }
        }
    }
}