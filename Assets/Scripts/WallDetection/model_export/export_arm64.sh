#!/bin/bash

# DeepLabV3 Export Script for Apple Silicon (ARM64)
# This script exports DeepLabV3 models optimized for M1/M2/M3 processors

# Stop on error
set -e

# Config
OUTPUT_DIR="./exported_models_arm64"
SIZES=(224 256 320 384)
MODELS=("mobilenet" "resnet50")

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Check architecture
ARCH=$(uname -m)
if [ "$ARCH" != "arm64" ]; then
    echo "WARNING: This script is optimized for ARM64 architecture (Apple Silicon)."
    echo "Current architecture: $ARCH"
    read -p "Continue anyway? (y/n): " CONTINUE
    if [ "$CONTINUE" != "y" ]; then
        exit 0
    fi
fi

# Check Python and pip
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is not installed or not in PATH"
    exit 1
fi

# Check if virtual environment exists, create if not
if [ ! -d "venv" ]; then
    echo "Creating virtual environment..."
    python3 -m venv venv
fi

# Activate virtual environment
source venv/bin/activate

# Install dependencies
echo "Installing dependencies for ARM64..."
pip install --upgrade pip
pip install -r requirements_arm64.txt

# Install PyTorch with MPS support (Apple Silicon optimized)
echo "Installing PyTorch with MPS support for Apple Silicon..."
pip install torch torchvision

# Check if PyTorch MPS is available
python3 -c "import torch; print('MPS available:', torch.backends.mps.is_available())"

# Export models
for MODEL in "${MODELS[@]}"; do
    for SIZE in "${SIZES[@]}"; do
        echo "Exporting $MODEL model with size ${SIZE}x${SIZE}..."
        python3 export_arm64.py --model_type "$MODEL" --size "$SIZE" --output_dir "$OUTPUT_DIR" --test
        
        # Optional: Export quantized version
        python3 export_arm64.py --model_type "$MODEL" --size "$SIZE" --output_dir "$OUTPUT_DIR" --quantize
    done
done

echo "All models exported successfully to $OUTPUT_DIR"
echo "Available models:"
ls -lh "$OUTPUT_DIR"

# Deactivate virtual environment
deactivate 