# DeepLabV3 Model Export for ARM64 (M1/M2 Mac)

This README provides instructions for exporting DeepLabV3 models on Apple Silicon (M1/M2) Macs where ONNX compilation may have compatibility issues with ARM64 architecture.

## The Problem

When attempting to build ONNX on Apple Silicon Macs, you may encounter errors related to SSE4.1 instructions that are not supported on ARM processors. The error typically looks like:

```
error: unsupported option '-msse4.1' for target 'arm64-apple-darwin'
```

This happens because ONNX's build process attempts to use x86-specific optimizations that are incompatible with ARM architecture.

## The Solution: TorchScript Export

Instead of using ONNX, we've created a simplified export pipeline that uses TorchScript, which is also supported by Unity Barracuda. This approach avoids the ARM64 compatibility issues and still produces models that work well with the Unity Barracuda runtime.

## Requirements

The simplified export process requires:
```
torch>=1.9.0
torchvision>=0.10.0
numpy>=1.19.0
matplotlib>=3.4.0
pillow>=8.0.0
tabulate>=0.8.9
```

You can install them using pip:
```bash
pip install -r requirements.txt
```

## Scripts

### export_mobilenet_simplified.py

This script exports DeepLabV3 models to TorchScript format instead of ONNX:

```bash
python export_mobilenet_simplified.py [--model_type MODEL_TYPE] [--output_dir OUTPUT_DIR] [--input_size INPUT_SIZE] [--test_image TEST_IMAGE]
```

Parameters:
- `--model_type`: Backbone architecture ('mobilenet_v3_large' or 'resnet50', default: 'mobilenet_v3_large')
- `--output_dir`: Directory to save the exported model (default: "models")
- `--input_size`: Input resolution for the model (default: 320)
- `--test_image`: Path to a test image for validating the model (optional)

### export_simplified.sh

Batch script to export models at different resolutions:

```bash
./export_simplified.sh [--test-image PATH]
```

### copy_models_to_unity.sh

Script to copy exported models to the Unity Resources folder:

```bash
./copy_models_to_unity.sh
```

## How It Works

The simplified export process:

1. Loads the DeepLabV3 model (MobileNetV3 or ResNet50) from torchvision
2. Creates a wrapper class that extracts only the 'out' tensor from the model's output dictionary
3. Traces the model with a dummy input tensor
4. Exports the traced model to TorchScript (.pt) format
5. Also saves the model's state dictionary as a backup
6. Copies the TorchScript models to Unity's Resources folder

## Testing in Unity

Once the models are exported and copied to the Resources folder:

1. In Unity, select your DeepLabDecoder component
2. Assign the TorchScript (.pt) file to the modelAsset field
3. Set the appropriate resolutionPreset to match the model's input size
4. Use the DeepLabModelTester to compare the performance and quality of different models

## Advantages Over ONNX

- No ARM64 compatibility issues
- Simpler export process
- No need for complex model optimization
- Direct integration with Barracuda

## Limitations

- Cannot use ONNX-specific optimizations
- May have slightly different runtime characteristics
- Requires using the DeepLabV3Wrapper class for proper tracing

## Additional Notes

- The TorchScript models may be slightly larger than optimized ONNX models
- Test multiple resolutions to find the best balance between quality and performance
- For production use on iOS/Android, you may want to export on an x86 machine with ONNX if possible 