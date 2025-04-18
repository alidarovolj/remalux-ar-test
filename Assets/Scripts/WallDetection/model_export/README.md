# DeepLabV3 Model Export for Unity Barracuda

This directory contains scripts for exporting DeepLabV3 models to ONNX format for use with Unity Barracuda.

## Requirements

To use these scripts, you need the following Python packages:
```
torch
torchvision
onnx
```

You can install them using pip:
```
pip install torch torchvision onnx
```

## export_mobilenet.py

This script exports a DeepLabV3 model with MobileNetV3-Large backbone to ONNX format.

### Usage

```
python export_mobilenet.py [--output_dir OUTPUT_DIR] [--input_size INPUT_SIZE] [--opset_version OPSET_VERSION]
```

### Parameters

- `--output_dir`: Directory to save the exported model (default: "models")
- `--input_size`: Input resolution (square) for the model (default: 320)
- `--opset_version`: ONNX opset version (default: 11, recommended for Barracuda compatibility)

### Example

```
# Export model with default settings (320x320, opset 11)
python export_mobilenet.py

# Export model with custom input size
python export_mobilenet.py --input_size 512

# Export model to a specific directory
python export_mobilenet.py --output_dir "/path/to/models"
```

## Exporting Different Model Sizes

You can export models with different input sizes to compare performance and quality:

```
# Export small model (224x224)
python export_mobilenet.py --input_size 224

# Export medium model (320x320)
python export_mobilenet.py --input_size 320

# Export large model (512x512)
python export_mobilenet.py --input_size 512

# Export very large model (768x768)
python export_mobilenet.py --input_size 768
```

## Using the Exported Models in Unity

After exporting the model:

1. Import the ONNX file into your Unity project's Assets folder
2. In your DeepLabDecoder component, assign the ONNX file to the `modelAsset` field
3. Configure the appropriate `resolutionPreset` to match the exported model's input size
4. Use the DeepLabModelTester to compare performance and quality of different models

## Troubleshooting

If you encounter errors during export:

1. Ensure you have the correct versions of torch, torchvision, and onnx installed
2. Try using a different opset version (9, 10, or 11)
3. Check that you have enough memory available (larger models require more memory)
4. If using CUDA, ensure your GPU drivers are up to date

## Comparing with ResNet Backbone

The MobileNetV3 backbone is optimized for mobile devices and runs faster than ResNet, but may have slightly lower accuracy. Use the DeepLabModelTester to compare:

- Processing speed
- Memory usage
- Segmentation quality
- Device compatibility 