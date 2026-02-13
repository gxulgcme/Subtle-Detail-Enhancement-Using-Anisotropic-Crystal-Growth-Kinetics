This project constitutes a computational imaging system designed for the enhancement and quality assessment of industrial X-ray images, specifically those in DICOM format. The implementation is grounded in a novel theoretical framework combining crystallographic growth models and fluid dynamics principles. The entire codebase is written in C# targeting the .NET 8.0 runtime.

Project Composition and Architecture
The solution is structured as a console application (DRImageFilters) comprising several core modules that work in concert to load, process, enhance, analyze, and export medical imaging data.

1. Core Image Enhancement Algorithm (CrystallographicEnhancer.cs)
This static class encapsulates the primary image processing algorithm, termed "Crystallographic Enhancement." Its design is inspired by anisotropic crystal growth theory and viscous fluid dynamics.

Theoretical Foundation: It explicitly references the Burton-Cabrera-Frank (BCF) crystal growth model, Perona-Malik diffusion, Gibbs free energy minimization, and multi-scale fluid dynamics.

Key Methods:

ComputeCrystalField: Calculates local growth rate and lattice energy based on neighborhood pixel analysis, simulating anisotropic crystal growth and Lennard-Jones potential interactions.

ApplyCrystalGrowthCore: Implements a multi-scale enhancement loop using a pyramid decomposition approach. It applies the crystal growth model at different scales, governed by a scale-dependent radius and a global enhancement strength parameter.

CrystalReconstruct: Recombines the processed base layer and enhanced detail layer using a bilateral filter for edge-preserving smoothing.

EnhanceByCrystallography: The main public method that orchestrates the enhancement pipeline for OpenCV Mat objects, including normalization and multi-scale processing.

EnhanceByCrystallographicEnhancer: An extension method for DicomImage objects. It handles the DICOM data pipeline: pixel data extraction, photometric interpretation inversion (from negative to positive Monochrome2 standard), application of the core enhancement algorithm, and reconstruction of a new DICOM dataset with the enhanced pixel data.

2. Image Quality Evaluation Module (ImageMetricsCalculator.cs)
This class provides objective, quantitative metrics to evaluate the performance of the enhancement algorithm.

Computable Metrics:

ComputePSNR: Calculates the Peak Signal-to-Noise Ratio between the original and processed 16-bit grayscale images.

ComputeSSIM: Calculates the Structural Similarity Index, measuring perceptual image fidelity.

ComputeSpatialFrequency: Computes the Spatial Frequency (SF) of an image, which correlates with its overall sharpness and texture detail.

ComputeAllMetrics: A convenience method that returns all three metrics (PSNR, SSIM, SF for both original and processed) in a single call.

3. Application Entry Point and Batch Processing (Program.cs)
This file contains the Program class with the Main entry point and defines the ImageQualityMetrics data model.

ImageQualityMetrics: A data class that stores results for a single processed image, including filename, dimensions, processing time, date, and all calculated quality metrics.

Core Functions:

ProcessDicomFileWithEnhanceByCrystallographicEnhancer: Processes a single DICOM file. It loads the image, applies the enhancement algorithm, calculates metrics, saves the enhanced DICOM file, and returns a populated ImageQualityMetrics object.

BatchProcessDicomFilesWithEnhanceByCrystallographicEnhancer: Orchestrates the batch processing of all DICOM files (.dcm, .diconde) within a specified directory. It iterates through files, calls the single-file processor, aggregates results, and handles errors.

SaveResultsToExcel: Exports the collected list of ImageQualityMetrics to an Excel file using the MiniExcel library.

Main: The application's entry point. It executes a parameter study, iterating over five different enhancement strength values (5.0 to 9.0). For each strength, it configures paths for input (e.g., TestedDICOM\DR\), output, and the Excel report, and initiates batch processing.

4. Utility and Extension (IntPtrExtensions.cs)
This static class provides unsafe extension methods to convert various primitive-type arrays (sbyte[], byte[], short[], ushort[], int[], uint[], float[], double[]) into IntPtr pointers. These utilities facilitate low-level memory operations, particularly when passing data between managed C# code and unmanaged libraries like OpenCV.

5. Project Configuration (DRImageFilters.csproj)
The MSBuild project file defines the following key properties and dependencies:

Target Framework: .NET 8.0

Output Type: Exe (Console Application)

Critical Configuration: AllowUnsafeBlocks is set to True, which is required for the pointer operations used in the image processing and extension methods.

Dependencies:

fo-dicom.NetCore (Version 4.0.8): The primary library for reading, writing, and manipulating DICOM files. It is used for loading DICOM datasets, extracting pixel data, and constructing enhanced DICOM files.

OpenCvSharp4.Windows (Version 4.11.0.20250507): A .NET wrapper for the OpenCV computer vision library. It is essential for all core image matrix operations, including the Mat class, filtering functions (BilateralFilter), and pixel-level manipulations performed by the enhancement algorithm.

MiniExcel (Version 2.0.0-preview.2): A lightweight library used for efficiently exporting the tabular quality assessment results to Microsoft Excel (.xlsx) format.

File Management: The project includes a list of DICOM test files (with .diconde extension) that are configured to be copied to the output directory during build.

Operational Pipeline
The system operates through a sequential pipeline:

Input: DICOM files are read from a specified directory using fo-dicom.

Preprocessing: Pixel data is extracted and converted to an OpenCV Mat format. The photometric interpretation is inverted from the typical X-ray negative to a standard positive display format.

Enhancement: The crystallographic enhancement algorithm (CrystallographicEnhancer) processes the image matrix.

Analysis: The original and enhanced images are compared using the ImageMetricsCalculator to generate PSNR, SSIM, and Spatial Frequency metrics.

Output & Reporting: The enhanced image is saved as a new DICOM file. Metrics for all processed images in the batch are aggregated and written to an Excel spreadsheet via MiniExcel.