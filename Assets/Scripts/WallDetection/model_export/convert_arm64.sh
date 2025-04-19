#!/bin/bash
# convert_arm64.sh - Shell script to convert existing ONNX models for ARM64 compatibility
# This script uses convert_arm64.py to optimize existing models for Apple Silicon

# Set up error handling
set -e

# Script configuration
INPUT_DIR="models"
OUTPUT_DIR="models_arm64"
TEST_IMAGE="test_images/wall1.jpg"

# Function for displaying usage information
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Convert existing ONNX models for ARM64 compatibility"
    echo ""
    echo "Options:"
    echo "  -i, --input-dir DIR    Directory containing input models (default: $INPUT_DIR)"
    echo "  -o, --output-dir DIR   Directory to save converted models (default: $OUTPUT_DIR)"
    echo "  -q, --quantize         Apply quantization to reduce model size"
    echo "  -t, --test             Test each converted model with a sample input"
    echo "  -h, --help             Show this help message and exit"
    echo ""
    echo "Example: $0 --input-dir ./models --output-dir ./models_arm64 --quantize --test"
}

# Parse command line arguments
QUANTIZE=0
TEST=0

while [[ $# -gt 0 ]]; do
    case $1 in
        -i|--input-dir)
            INPUT_DIR="$2"
            shift 2
            ;;
        -o|--output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -q|--quantize)
            QUANTIZE=1
            shift
            ;;
        -t|--test)
            TEST=1
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Check if we're on Apple Silicon
if [[ $(uname -m) != "arm64" ]]; then
    echo "WARNING: This script is optimized for Apple Silicon (M1/M2/M3)."
    echo "Current architecture: $(uname -m)"
    read -p "Continue anyway? (y/n): " CONTINUE
    if [[ "$CONTINUE" != "y" ]]; then
        exit 0
    fi
fi

# Check if the virtual environment exists
if [ ! -d "venv" ]; then
    echo "Virtual environment not found. Creating one..."
    python3 -m venv venv
fi

# Activate the virtual environment
echo "Activating virtual environment..."
source venv/bin/activate

# Check if convert_arm64.py exists
if [ ! -f "convert_arm64.py" ]; then
    echo "ERROR: convert_arm64.py not found!"
    exit 1
fi

# Make sure required directories exist
mkdir -p "$INPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build command
CMD="python3 convert_arm64.py --input_dir $INPUT_DIR --output_dir $OUTPUT_DIR"

# Add options
if [ $QUANTIZE -eq 1 ]; then
    CMD="$CMD --quantize"
fi

if [ $TEST -eq 1 ]; then
    CMD="$CMD --test"
    if [ -f "$TEST_IMAGE" ]; then
        CMD="$CMD --test_image $TEST_IMAGE"
    fi
fi

# Print command and run it
echo "Running: $CMD"
$CMD

# Show available models after conversion
echo ""
echo "Available converted models in $OUTPUT_DIR:"
ls -lh "$OUTPUT_DIR"

# Deactivate virtual environment
deactivate

echo ""
echo "Conversion complete! Models saved to: $OUTPUT_DIR" 