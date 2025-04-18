# Enhanced DeepLabV3 Model Export System

## Overview

This project provides a comprehensive system for exporting, optimizing, and testing DeepLabV3 models for use in the Remalux AR Wall Painting application. The system is designed to:

1. Export DeepLabV3 models with different backbones (MobileNetV3-Large, ResNet50)
2. Support various input resolutions for performance/quality tradeoffs
3. Optimize models for improved inference performance
4. Validate models with test images and visualize results
5. Generate comprehensive reports on model characteristics

## Key Features

### Model Export

- Support for multiple backbone architectures:
  - MobileNetV3-Large (optimized for mobile devices)
  - ResNet50 (higher accuracy, larger size)
- Configurable input resolutions (224x224, 320x320, 512x512, 768x768)
- ONNX export with proper configuration for Unity Barracuda
- Customizable opset version (default: 11 for Barracuda compatibility)

### Model Optimization

- Automatic optimization of ONNX models
- Elimination of redundant operations
- Fusion of batch normalization into convolutions
- Optimization of computational graphs
- File size reduction

### Model Validation

- Testing with sample images
- Performance measurement (inference time)
- Visualization of segmentation results
- Comparison of different model configurations
- Quality assessment of wall segmentation

### Batch Processing

- Scripts for exporting multiple models in one operation
- Support for both Windows and Linux/macOS environments
- Customizable export parameters

## Components

### Core Scripts

- `export_mobilenet.py`: Main Python script for model export and optimization
- `test_export.py`: Helper script for testing the export process
- `run_test.sh`/`run_test.bat`: Platform-specific scripts to run the test script
- `export_all_sizes.sh`/`export_all_sizes.bat`: Batch export scripts

### Supporting Files

- `requirements.txt`: Python dependencies
- `README.md`: Usage instructions
- `test_images/`: Directory for test images

## Quick Start

### Installation

1. Ensure Python 3.6+ is installed
2. Install dependencies: `pip install -r requirements.txt`

### Basic Usage

To run a quick test of the export functionality:

```
# Linux/macOS
./run_test.sh

# Windows
run_test.bat
```

To export models at multiple resolutions:

```
# Linux/macOS
./export_all_sizes.sh

# Windows
export_all_sizes.bat
```

### Advanced Usage

For customized export:

```
python export_mobilenet.py --model_type mobilenet_v3_large --input_size 512 --optimize --test_image test_images/room_wall.jpg
```

## Integration with Unity

1. Export the models using the provided scripts
2. Import the ONNX files into your Unity project's Assets/Resources/Models folder
3. In the DeepLabDecoder component, assign the ONNX file to the `modelAsset` field
4. Configure the `resolutionPreset` to match the model's input size
5. Use the DeepLabModelTester to compare different models

## Performance Considerations

- **Mobile Devices**: Use MobileNetV3-Large with 224x224 or 320x320 resolution
- **Tablets/Mid-range Devices**: Use MobileNetV3-Large with 320x320 or 512x512 resolution
- **High-end Devices**: Use ResNet50 with 512x512 or 768x768 resolution

## Workflow Integration

This export system is designed to integrate with the development workflow:

1. Export models with different configurations
2. Import into Unity for testing
3. Use the DeepLabModelTester to evaluate performance
4. Select optimal configuration based on target devices
5. Generate production-ready models

## Troubleshooting

- If PyTorch fails to load the model, check your internet connection as it may need to download weights
- For CUDA-related errors, ensure compatible GPU drivers are installed
- If getting import errors, verify that all dependencies are correctly installed
- For visualization issues, ensure matplotlib and PIL are working correctly

## Future Improvements

- Integration with the Unity Editor for direct model export
- Support for additional model architectures
- Quantization to reduce model size
- Adaptive model selection based on device capabilities
- Extended test suite with diverse image datasets 