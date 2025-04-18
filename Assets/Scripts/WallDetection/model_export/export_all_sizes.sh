#!/bin/bash
# Script to export DeepLabV3 MobileNet model at different sizes

# Create output directory if not exists
mkdir -p models

echo "===== Exporting DeepLabV3 MobileNet models at different sizes ====="

# Export small model (224x224)
echo "\n\n===== Exporting 224x224 model ====="
python export_mobilenet.py --input_size 224

# Export medium model (320x320)
echo "\n\n===== Exporting 320x320 model ====="
python export_mobilenet.py --input_size 320

# Export large model (512x512)
echo "\n\n===== Exporting 512x512 model ====="
python export_mobilenet.py --input_size 512

# Export very large model (768x768)
echo "\n\n===== Exporting 768x768 model ====="
python export_mobilenet.py --input_size 768

echo "\n\n===== Export completed ====="
echo "Models are saved in the 'models' directory"
echo "Please import them into your Unity project" 