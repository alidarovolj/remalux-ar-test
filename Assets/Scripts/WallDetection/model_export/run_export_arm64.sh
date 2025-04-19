#!/bin/bash

# Check for M1/M2 Mac
if [[ $(uname -m) != "arm64" ]]; then
    echo "Warning: This script is optimized for Apple Silicon (M1/M2) Macs."
    echo "Your system is running on $(uname -m) architecture."
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Setup Python environment
echo "Setting up Python environment for ARM64 export..."

# Create virtual environment if it doesn't exist
if [ ! -d "venv" ]; then
    echo "Creating virtual environment..."
    python3 -m venv venv
fi

# Activate virtual environment
source venv/bin/activate

# Install requirements with ARM64 optimizations
echo "Installing requirements..."
pip install --upgrade pip
pip install -r requirements.txt --no-build-isolation

# Add ARM64-specific packages
echo "Installing ARM64-specific packages..."
pip install torch torchvision --no-binary torch,torchvision --extra-index-url https://download.pytorch.org/whl/cpu

# Set environment variables for MPS acceleration
export PYTORCH_ENABLE_MPS_FALLBACK=1

# Run the export script
echo "Running model export for ARM64..."
python export_arm64.py mobilenet_v3_large

# Export ResNet50 models as well
echo "Running ResNet50 model export for ARM64..."
python export_arm64.py resnet50

# Deactivate virtual environment
deactivate

echo "Export complete! Models saved to ./exported_models_arm64"
echo "Copy the optimized models to your Unity project's Assets/Models directory." 