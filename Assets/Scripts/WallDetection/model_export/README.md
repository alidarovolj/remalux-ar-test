# DeepLabV3 Model Export for Unity Barracuda

This directory contains scripts for exporting DeepLabV3 models to ONNX format for use with Unity Barracuda.

## Requirements

To use these scripts, you need the following Python packages:
```
torch
torchvision
onnx
numpy
matplotlib
pillow
tabulate
```

You can install them using pip:
```
pip install -r requirements.txt
```

## Standard Tools

### export_mobilenet.py

This script exports DeepLabV3 models with various backbones (MobileNetV3-Large or ResNet50) to ONNX format, with options for optimization and validation.

### Usage

```
python export_mobilenet.py [--model_type MODEL_TYPE] [--output_dir OUTPUT_DIR] [--input_size INPUT_SIZE] [--opset_version OPSET_VERSION] [--optimize] [--test_image TEST_IMAGE]
```

### Parameters

- `--model_type`: Backbone architecture to use ('mobilenet_v3_large' or 'resnet50', default: 'mobilenet_v3_large')
- `--output_dir`: Directory to save the exported model (default: "models")
- `--input_size`: Input resolution (square) for the model (default: 320)
- `--opset_version`: ONNX opset version (default: 11, recommended for Barracuda compatibility)
- `--optimize`: Flag to optimize the model for inference (optional)
- `--test_image`: Path to a test image for validating the model (optional)

### Examples

```
# Export model with default settings (MobileNetV3, 320x320, opset 11)
python export_mobilenet.py

# Export model with custom input size and optimization
python export_mobilenet.py --input_size 512 --optimize

# Export ResNet50 model with test validation
python export_mobilenet.py --model_type resnet50 --test_image test_images/room_wall.jpg

# Export model to a specific directory
python export_mobilenet.py --output_dir "/path/to/models" --optimize
```

## Apple Silicon (ARM64) Tools

### Apple Silicon Compatibility

When working with Apple Silicon (M1/M2/M3) Macs, you may encounter compatibility issues with ONNX compilation due to x86-specific optimizations that are not compatible with ARM architecture. We've created specialized tools for exporting and converting models on ARM64 devices.

### ARM64-Specific Scripts

#### export_arm64.py
Exports DeepLabV3 models optimized for ARM64 architecture:

```
python export_arm64.py --model_type MODEL_TYPE --size SIZE --output_dir OUTPUT_DIR [--quantize] [--test]
```

Parameters:
- `--model_type`: Model backbone ('mobilenet' or 'resnet50')
- `--size`: Input resolution (e.g., 224, 256, 320, 384)
- `--output_dir`: Directory for saving models (default: "./exported_models_arm64")
- `--quantize`: Apply quantization to reduce model size
- `--test`: Test the model after export

#### convert_arm64.py
Converts existing ONNX models for ARM64 compatibility:

```
python convert_arm64.py --input_dir INPUT_DIR --output_dir OUTPUT_DIR [--quantize] [--test]
```

Parameters:
- `--input_dir`: Directory containing input ONNX models
- `--output_dir`: Directory to save converted models
- `--quantize`: Apply quantization to the model
- `--test`: Test the converted model

#### setup_arm64.sh
Sets up the Python environment for model export on Apple Silicon:

```
./setup_arm64.sh
```

This script:
- Checks if running on ARM64 architecture
- Creates a Python virtual environment
- Installs PyTorch with MPS support
- Installs all dependencies from requirements_arm64.txt
- Verifies ONNX installation

#### export_arm64.sh
Batch exports models at different resolutions for ARM64:

```
./export_arm64.sh
```

### ARM64 Requirements

For Apple Silicon Macs, use the ARM64-specific requirements:

```
pip install -r requirements_arm64.txt
```

Key differences in ARM64 requirements:
- PyTorch is installed separately with MPS support
- ONNX components are installed with `--no-build-isolation` to avoid SSE4.1 compilation errors
- Additional dependencies for ARM64 optimization

## analyze_models.py

This script analyzes exported ONNX models and generates comprehensive comparison reports including file size, parameter count, operations, and visualizations.

### Usage

```
python analyze_models.py [--models_dir MODELS_DIR] [--output_dir OUTPUT_DIR] [--csv]
```

### Parameters

- `--models_dir`: Directory containing models to analyze (default: "models")
- `--output_dir`: Directory to save analysis reports (default: "model_analysis")
- `--csv`: Flag to export model data to CSV file (optional)

### Examples

```
# Analyze models with default settings
python analyze_models.py

# Analyze models in a specific directory and output CSV
python analyze_models.py --models_dir "/path/to/models" --csv

# Specify custom output directory for reports
python analyze_models.py --output_dir "my_reports"
```

## Batch Scripts

Two sets of scripts are provided for convenience:

### Export Scripts

Export models with different configurations:

```
# Linux/macOS
./export_all_sizes.sh [--no-optimize] [--test-image PATH]

# Windows
export_all_sizes.bat [--no-optimize] [--test-image PATH]

# Apple Silicon (ARM64)
./export_arm64.sh
```

### Analysis Scripts

Analyze exported models and generate reports:

```
# Linux/macOS
./analyze_models.sh [--models_dir=DIR] [--output_dir=DIR] [--csv]

# Windows
analyze_models.bat [--models_dir=DIR] [--output_dir=DIR] [--csv]
```

## Test Scripts

Test the export functionality with sample images:

```
# Linux/macOS
./run_test.sh

# Windows
run_test.bat
```

## Environment Check Scripts

Verify your environment is properly configured:

```
# Linux/macOS
./check_env.sh

# Windows
check_env.bat
```

## Model Optimization

The script can optimize ONNX models for improved inference performance using the `--optimize` flag or directly in the batch scripts. Optimizations include:

- Eliminating identity operations
- Fusing batch normalization into convolutions
- Eliminating redundant transposes
- Optimizing computational graphs

## Model Testing and Validation

You can validate models by providing a test image with the `--test_image` parameter. The script will:

1. Run the model on the provided image
2. Measure inference time
3. Generate visualizations of the segmentation results
4. Save these visualizations in the `test_results` directory

This allows you to compare the quality and performance of different model configurations before importing them into Unity.

## Model Analysis

The analyzer generates detailed reports, including:

1. Interactive HTML report with model comparisons
2. Size and parameter charts
3. Operation type analysis
4. Performance recommendations

View the reports in any web browser after running the analyzer.

## Using the Exported Models in Unity

After exporting the models:

1. Import the ONNX files into your Unity project's Assets/Resources/Models folder
2. In your DeepLabDecoder component, assign the ONNX file to the `modelAsset` field
3. Configure the appropriate `resolutionPreset` to match the exported model's input size
4. Use the DeepLabModelTester to compare performance and quality of different models

## Performance Considerations

- **MobileNetV3-Large**: Optimized for mobile devices, faster inference, smaller size
- **ResNet50**: Higher accuracy but slower and larger file size
- **Input Resolution**: Higher resolutions provide better segmentation quality but slower inference
- **Optimization**: Optimized models may run significantly faster on mobile devices

## Troubleshooting

If you encounter errors during export:

1. Ensure you have the correct versions of torch, torchvision, and onnx installed
2. Try using a different opset version (9, 10, or 11)
3. Check that you have enough memory available (larger models require more memory)
4. If using CUDA, ensure your GPU drivers are up to date
5. For visualization issues, ensure matplotlib and pillow are correctly installed

### ARM64-Specific Issues

When building on Apple Silicon (M1/M2/M3) Macs:

1. **SSE4.1 Error**: If you see `unsupported option '-msse4.1' for target 'arm64-apple-darwin'`, use the ARM64-specific tools
2. **ONNX Build Failures**: Install ONNX with `pip install --no-build-isolation onnx`
3. **PyTorch MPS Issues**: Ensure PyTorch is installed with MPS support using `pip install torch torchvision`
4. **Slow Model Inference**: Enable MPS acceleration in your code with `device = torch.device("mps")`
5. **Compatibility Problems**: Consider using `convert_arm64.py` to adapt models built on x86 machines

## Comparing Model Performance

Use the DeepLabModelTester in Unity to compare:

- Processing speed (FPS)
- Memory usage
- Segmentation quality
- Device compatibility

## Workflow

A typical workflow for finding the optimal model would be:

1. Run `./export_all_sizes.sh` (or `./export_arm64.sh` on Apple Silicon) to generate models at different resolutions
2. Run `./analyze_models.sh` to generate comparison reports
3. Import promising models into Unity
4. Test on target devices with DeepLabModelTester
5. Select the best model based on quality and performance 