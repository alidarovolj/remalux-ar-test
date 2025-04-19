#!/bin/bash
# Simplified script to export DeepLabV3 MobileNet model at different sizes

# Set environment
export_dir="models"
test_image="test_images/room_wall.jpg"

# Create directories if they don't exist
mkdir -p $export_dir
mkdir -p test_images

# Display help if needed
if [ "$1" == "-h" ] || [ "$1" == "--help" ]; then
  echo "Usage: ./export_simplified.sh [--test-image PATH]"
  echo "  --test-image PATH: Specify custom test image"
  exit 0
fi

# Check if test image path is specified
if [[ "$*" == *"--test-image"* ]]; then
  for arg in "$@"; do
    if [[ "$prev_arg" == "--test-image" ]]; then
      test_image="$arg"
      break
    fi
    prev_arg="$arg"
  done
fi

# Check if test image exists
if [ -f "$test_image" ]; then
  test_img_arg="--test_image $test_image"
  echo "Using test image: $test_image"
else
  test_img_arg=""
  echo "No test image found at $test_image, testing will be skipped"
  echo "Please add a test image to the test_images directory or specify with --test-image"
fi

echo "===== Exporting DeepLabV3 MobileNet models at different sizes ====="
echo "Export directory: $export_dir"
echo ""

# Export small model (224x224)
echo "===== Exporting 224x224 model ====="
python export_mobilenet_simplified.py --input_size 224 --output_dir $export_dir $test_img_arg

# Export medium model (320x320)
echo "===== Exporting 320x320 model ====="
python export_mobilenet_simplified.py --input_size 320 --output_dir $export_dir $test_img_arg

# Export large model (512x512)
echo "===== Exporting 512x512 model ====="
python export_mobilenet_simplified.py --input_size 512 --output_dir $export_dir $test_img_arg

echo ""
echo "===== Export completed ====="
echo "Models are saved in the '$export_dir' directory"
echo "Test results (if any) are saved in the '$export_dir/test_results' directory"
echo "Please import the models into your Unity project at Assets/Resources/Models" 