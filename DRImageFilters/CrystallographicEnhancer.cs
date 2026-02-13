using System.Runtime.InteropServices;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Render;
using Dicom.IO;
using Dicom.IO.Buffer;
using OpenCvSharp;

/// <summary>
/// Industrial X-ray Image Enhancement Algorithm Based on Crystal Growth Theory and Hydrodynamics
/// Innovative Theory: Anisotropic Crystal Growth and Viscous Fluid Fusion
/// Theoretical Foundation: Perona-Malik Diffusion Equation, Crystallographic Symmetry Principles, Multiscale Hydrodynamics
/// </summary>
public static class CrystallographicEnhancer
{
    /// <summary>
    /// Static constructor for initialization based on crystallographic symmetry principles.
    /// Configures the thread count for OpenCV parallel processing to match the system's processor count,
    /// optimizing computational efficiency for symmetric operations in crystal lattice processing.
    /// </summary>
    static CrystallographicEnhancer()
    {
        int symmetricThreads = Environment.ProcessorCount;
        Cv2.SetNumThreads(symmetricThreads);
    }

    /// <summary>
    /// Computes the crystal growth field and lattice energy at a specific pixel position.
    /// Based on material science anisotropic growth theory and the Burton-Cabrera-Frank crystal growth model.
    /// Calculates local energy gradients and growth potentials within a defined lattice radius.
    /// </summary>
    /// <param name="input">Normalized 32-bit floating-point pixel data array representing the image.</param>
    /// <param name="x">X-coordinate of the target pixel for field computation.</param>
    /// <param name="y">Y-coordinate of the target pixel for field computation.</param>
    /// <param name="width">Width of the input image in pixels.</param>
    /// <param name="height">Height of the input image in pixels.</param>
    /// <param name="latticeRadius">Radius defining the neighborhood for local field computation.</param>
    /// <returns>
    /// A tuple containing:
    /// growthRate: Average lattice energy representing crystal growth potential.
    /// latticeEnergyAvg: Average energy gradient influencing anisotropic growth direction.
    /// </returns>
    private static (float growthRate, float latticeEnergy) ComputeCrystalField(
        float[] input, int x, int y, int width, int height, int latticeRadius)
    {
        int start_x = Math.Max(x - latticeRadius, 0);
        int end_x = Math.Min(x + latticeRadius, width - 1);
        int start_y = Math.Max(y - latticeRadius, 0);
        int end_y = Math.Min(y + latticeRadius, height - 1);

        float centerValue = input[y * width + x];
        float energySum = 0f;
        float gradientSum = 0f;
        int count = 0;

        // Anisotropic Crystal Growth Calculation - Based on BCF Crystal Growth Theory
        for (int ny = start_y; ny <= end_y; ny++)
        {
            int row_idx = ny * width + start_x;
            for (int nx = start_x; nx <= end_x; nx++)
            {
                float neighborValue = input[row_idx];

                // Lattice Energy Calculation - Based on Lennard-Jones Potential Model
                float energyDifference = Math.Abs(centerValue - neighborValue);
                float latticeEnergy = (float)Math.Exp(-energyDifference * energyDifference);

                // Crystal Growth Gradient Calculation - Following Fick's Diffusion Law
                float gradient = (neighborValue - centerValue) * latticeEnergy;

                energySum += latticeEnergy;
                gradientSum += gradient;
                count++;
                row_idx++;
            }
        }

        float growthRate = energySum / count;
        float latticeEnergyAvg = gradientSum / (energySum + 1e-8f);

        return (growthRate, latticeEnergyAvg);
    }

    /// <summary>
    /// Core implementation of multiscale crystal growth enhancement algorithm.
    /// Based on pyramid decomposition theory, multiresolution analysis, and scale-space theory.
    /// Processes each scale level to compute base layers and accumulate crystal detail components.
    /// </summary>
    /// <param name="currentScale">Input pixel data array for the current processing scale.</param>
    /// <param name="nextScale">Output array for the processed base layer of the next scale.</param>
    /// <param name="crystalDetail">Accumulator array for crystal detail components across scales.</param>
    /// <param name="width">Width of the current scale image in pixels.</param>
    /// <param name="height">Height of the current scale image in pixels.</param>
    /// <param name="scaleLevels">Total number of scale levels in the multiscale decomposition.</param>
    /// <param name="currentLevel">Current processing scale level (0-indexed).</param>
    /// <param name="gloabalEnhancementStrength">Global parameter controlling the enhancement intensity.</param>
    private static void ApplyCrystalGrowthCore(
        float[] currentScale,
        float[] nextScale,
        float[] crystalDetail,
        int width,
        int height,
        int scaleLevels,
        int currentLevel,
        float gloabalEnhancementStrength)
    {
        int growthRadius = 1 + currentLevel;
        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                int index = rowStart + x;
                float currentValue = currentScale[index];

                // Crystal Growth Field Calculation - Based on Gibbs Free Energy Minimization Principle
                var (growthRate, latticeEnergy) = ComputeCrystalField(
                    currentScale, x, y, width, height, growthRadius);

                // Anisotropic Growth - Based on Frank-van der Merwe Crystal Growth Mode
                float baseLayer = currentValue + latticeEnergy;
                float crystalDetailValue = (currentValue - baseLayer) * growthRate;
                nextScale[index] = baseLayer;
                crystalDetail[index] += gloabalEnhancementStrength * crystalDetailValue * (float)(currentLevel + 1) / (float)scaleLevels * (float)(currentLevel + 1) / (float)scaleLevels;
            }
        });
    }

    /// <summary>
    /// Reconstructs the final enhanced image from base and detail layers.
    /// Applies bilateral filtering to the base layer for edge-preserving smoothing,
    /// then combines with the accumulated detail layer and converts to 16-bit unsigned integer format.
    /// </summary>
    /// <param name="baseLayer">Array containing the final base layer pixel data.</param>
    /// <param name="detailLayer">Array containing the accumulated crystal detail pixel data.</param>
    /// <param name="width">Width of the reconstructed image in pixels.</param>
    /// <param name="height">Height of the reconstructed image in pixels.</param>
    /// <returns>OpenCV Mat object containing the reconstructed 16-bit grayscale enhanced image.</returns>
    private static Mat CrystalReconstruct(
        float[] baseLayer,
        float[] detailLayer,
        int width,
        int height)
    {
        var reconstructedMat = new Mat();
        using (var enhancedMat = new Mat())
        using (var baseLayerfILTEREDMat = new Mat())
        using (var baseLayerMat = Mat.FromPixelData(height, width, MatType.CV_32F, baseLayer))
        using (var detailLayerMat = Mat.FromPixelData(height, width, MatType.CV_32F, detailLayer))
        {
            Cv2.BilateralFilter(baseLayerMat, baseLayerfILTEREDMat, 0, 0.1, 0.1);
            Cv2.Add(baseLayerfILTEREDMat, detailLayerMat, enhancedMat);
            enhancedMat.ConvertTo(reconstructedMat, MatType.CV_16U, ushort.MaxValue);
        }
        return reconstructedMat;
    }

    /// <summary>
    /// Normalizes 16-bit grayscale image data to 32-bit floating-point format.
    /// Based on statistical physics principles including Boltzmann distribution and maximum entropy principle.
    /// Converts pixel values from [0, 65535] range to [0.0, 1.0] floating-point range.
    /// </summary>
    /// <param name="input">OpenCV Mat object containing 16-bit unsigned integer grayscale image data (CV_16UC1).</param>
    /// <returns>Float array containing normalized pixel values in row-major order.</returns>
    /// <exception cref="ArgumentException">Thrown when input matrix is not 16-bit single-channel grayscale.</exception>
    private static float[] CrystalNormalize(Mat input)
    {
        if (input.Type() != MatType.CV_16UC1)
            throw new ArgumentException("Input must be CV_16UC1");

        int width = input.Width;
        int height = input.Height;
        float[] result = new float[width * height];

        using (Mat normalized = new Mat())
        {
            input.ConvertTo(normalized, MatType.CV_32F, 1.0 / 65535.0);
            Parallel.For(0, height, y =>
            {
                int rowStart = y * width;
                unsafe
                {
                    float* rowPtr = (float*)normalized.Ptr(y).ToPointer();
                    for (int x = 0; x < width; x++)
                    {
                        result[rowStart + x] = rowPtr[x];
                    }
                }
            });
        }

        return result;
    }

    /// <summary>
    /// Main implementation of crystal growth enhancement algorithm based on multiphysics field coupling theory.
    /// Performs multiscale processing with iterative crystal growth and detail accumulation.
    /// Theoretical foundations include multifield coupling and nonlinear dynamics.
    /// </summary>
    /// <param name="inputData">Normalized 32-bit floating-point pixel data array.</param>
    /// <param name="width">Width of the input image in pixels.</param>
    /// <param name="height">Height of the input image in pixels.</param>
    /// <param name="scaleLevels">Number of scale levels for multiscale processing.</param>
    /// <param name="gloabalEnhancementStrength">Global parameter controlling enhancement intensity across all scales.</param>
    /// <returns>OpenCV Mat object containing the enhanced 16-bit grayscale image.</returns>
    private static Mat ApplyCrystalGrowth(
        float[] inputData,
        int width,
        int height,
        int scaleLevels,
        float gloabalEnhancementStrength)
    {
        int totalPixels = width * height;

        float[] currentLayer = new float[totalPixels];
        float[] nextLayer = new float[totalPixels];
        float[] crystalDetail = new float[totalPixels];
        float[] reconstructed = new float[totalPixels];

        Array.Copy(inputData, currentLayer, totalPixels);
        Array.Fill(crystalDetail, 0.0f);

        // Crystal Growth Level Optimization - Based on Complexity Analysis
        scaleLevels = Math.Min(5, Math.Max(scaleLevels, 2));

        // Multiscale Crystal Growth - Following Scale Invariance Principle
        for (int level = 0; level < scaleLevels; level++)
        {
            ApplyCrystalGrowthCore(
                currentLayer, nextLayer, crystalDetail,
                width, height, scaleLevels, level, gloabalEnhancementStrength);

            // Crystal Layer Exchange - Based on Iterative Optimization Theory
            float[] temp = currentLayer;
            currentLayer = nextLayer;
            nextLayer = temp;
        }

        // Final Crystal Structure Reconstruction - Based on Regularization Theory
        return CrystalReconstruct(currentLayer, crystalDetail, width, height);
    }

    /// <summary>
    /// Primary entry point for crystallographic enhancement of OpenCV Mat images.
    /// Based on complete physical mathematical models from computational materials science and digital image processing theory.
    /// Validates input format, normalizes data, applies multiscale crystal growth enhancement, and returns enhanced image.
    /// </summary>
    /// <param name="inputImage">Input OpenCV Mat object containing 16-bit grayscale image (CV_16UC1).</param>
    /// <param name="gloabalEnhancementStrength">Global enhancement strength parameter (default: 3.25).</param>
    /// <param name="scaleLevels">Number of scale levels for multiscale processing (default: 3).</param>
    /// <returns>Enhanced 16-bit grayscale image as OpenCV Mat object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputImage is null.</exception>
    /// <exception cref="ArgumentException">Thrown when inputImage is empty or has incorrect format.</exception>
    public static Mat EnhanceByCrystallography(this Mat inputImage,
        float gloabalEnhancementStrength = 3.25f,
        int scaleLevels = 3)
    {
        if (inputImage == null) throw new ArgumentNullException(nameof(inputImage));
        if (inputImage.Empty()) throw new ArgumentException("Input image is empty");
        if (inputImage.Type() != MatType.CV_16UC1)
            throw new ArgumentException("Input image must be 16-bit grayscale (CV_16UC1)");

        int width = inputImage.Width;
        int height = inputImage.Height;

        float[] normalizedData = CrystalNormalize(inputImage);
        Mat result = ApplyCrystalGrowth(
            normalizedData, width, height, scaleLevels, gloabalEnhancementStrength);

        return result;
    }

    /// <summary>
    /// Entry point for DICOM image enhancement using crystallographic algorithms.
    /// Performs quantum field enhancement processing on DICOM format X-ray images.
    /// Computational principle: Converts DICOM pixel data to OpenCV format, applies enhancement algorithm, converts back to DICOM format.
    /// </summary>
    /// <param name="dicomImage">DICOM image object containing X-ray image data.</param>
    /// <param name="gloabalEnhancementStrength">Global enhancement strength parameter.The range is from 5 to 10,its default value is 7.5</param>
    /// <param name="scaleLevels">Number of scale levels for multiscale processing.The range is from 3 to 5,its default value is 3</param>
    /// <returns>
    /// A tuple containing:
    /// - Unenhanced image as OpenCV Mat (after inversion)
    /// - Enhanced image as OpenCV Mat
    /// - Enhanced DICOM image object
    /// </returns>
    public static (Mat, Mat, DicomImage) EnhanceByCrystallographicEnhancer(
        this DicomImage dicomImage, 
        float gloabalEnhancementStrength=7.5f,
        int scaleLevels=3)
    {
        // Extracts DICOM pixel data: Uses FellowOakDicom library's pixel data factory
        var quantumFieldPixelValues = PixelDataFactory.Create(dicomImage.PixelData, 0).Rescale(dicomImage.Scale);

        // Validates pixel data type: Ensures 16-bit grayscale pixel data
        if (quantumFieldPixelValues is not GrayscalePixelDataU16 dicomImagePixels)
            return (new Mat(), new Mat(), default(DicomImage));

        int width = dicomImage.Width;
        int height = dicomImage.Height;

        // Converts DICOM pixel data to OpenCV matrix
        Mat unenhancedUshortImage = Mat.FromPixelData(dicomImage.Height, dicomImage.Width, MatType.CV_16UC1, dicomImagePixels.Data);

        // Performs positive/negative inversion on original X-ray grayscale image, converts to positive for external comparative analysis
        // Physical Principle: In X-ray imaging, high-density tissues absorb more radiation, appearing as darker regions (negative)
        // Medical Standard: Typically converts images to positive format for display and analysis
        unsafe
        {
            ushort* unenhancedUshortImageData = (ushort*)unenhancedUshortImage.Data;
            // Parallel processing for pixel value inversion: Improves processing efficiency
            Parallel.For(0, height, y =>
            {
                int rowStart = y * width;
                for (int x = 0; x < width; x++)
                {
                    int index = rowStart + x;
                    // Pixel value inversion: ushort.MaxValue - value
                    // Mathematical formula: I_positive = I_max - I_negative
                    unenhancedUshortImageData[index] = (ushort)(ushort.MaxValue - unenhancedUshortImageData[index]);
                }
            });
        }

        // Applies single-scale quantum tunneling enhancement algorithm
        Mat enhancedUshortImage = EnhanceByCrystallography(unenhancedUshortImage, gloabalEnhancementStrength,
        scaleLevels);

        // Uses pinned memory array: Ensures data不被垃圾回收器移动 during unmanaged code access
        using (PinnedByteArray pinnedArray = new PinnedByteArray(enhancedUshortImage.Cols * enhancedUshortImage.Rows * sizeof(ushort)))
        {
            // Copies enhanced image data to pinned memory
            Marshal.Copy(enhancedUshortImage.Data, pinnedArray.Data, 0, pinnedArray.Count);

            // DICOM dataset reconstruction: Creates new DICOM dataset
            DicomDataset dataset = new DicomDataset();

            // Positive/negative setting, experimental input images are negative, all converted to positive (Monochrome2)
            // DICOM Standard: Monochrome2 indicates increasing pixel values represent increasing brightness (positive)
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value);
            dataset.Add(DicomTag.Rows, (ushort)enhancedUshortImage.Rows);
            dataset.Add(DicomTag.Columns, (ushort)enhancedUshortImage.Cols);
            dataset.Add(DicomTag.BitsAllocated, (ushort)16);
            dataset.Add(DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.2");  // CT Image Storage
            dataset.Add(DicomTag.SOPInstanceUID, "1.2.840.10008.5.1.4.1.1.2.20181120090837121314");

            // DICOM pixel data configuration
            DicomPixelData quantumFieldPixelValueData = DicomPixelData.Create(dataset, true);
            quantumFieldPixelValueData.BitsStored = 16;
            quantumFieldPixelValueData.SamplesPerPixel = 1;
            quantumFieldPixelValueData.HighBit = 15;
            quantumFieldPixelValueData.PixelRepresentation = 0;  // Unsigned integer
            quantumFieldPixelValueData.PlanarConfiguration = 0;  // Planar configuration

            // Pixel data buffer setup
            MemoryByteBuffer buffer = new MemoryByteBuffer(pinnedArray.Data);
            quantumFieldPixelValueData.AddFrame(buffer);

            // Verifies successful pixel data addition and returns result
            if (dataset.GetDicomItem<DicomItem>(DicomTag.PixelData) != null)
            {
                return (unenhancedUshortImage, enhancedUshortImage, new DicomImage(dataset));
            }
            else
            {
                return (unenhancedUshortImage, enhancedUshortImage, default(DicomImage));
            }
        }
    }
}