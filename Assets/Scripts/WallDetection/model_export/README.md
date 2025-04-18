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
```

You can install them using pip:
```
pip install -r requirements.txt
```

## export_mobilenet.py

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

## Batch Export Scripts

Two scripts are provided to export models at different sizes:

### export_all_sizes.sh (Linux/macOS)

```
./export_all_sizes.sh [--no-optimize] [--test-image PATH]
```

### export_all_sizes.bat (Windows)

```
export_all_sizes.bat [--no-optimize] [--test-image PATH]
```

These scripts export models with input sizes of 224x224, 320x320, 512x512, and 768x768.

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

## Comparing Model Performance

Use the DeepLabModelTester in Unity to compare:

- Processing speed (FPS)
- Memory usage
- Segmentation quality
- Device compatibility 