#!/bin/bash
# setup_arm64.sh - Setup script for DeepLabV3 model export on Apple Silicon (M1/M2/M3)
# This script installs the required dependencies and prepares the environment for model export.

# Set up error handling
set -e

echo "===== DeepLabV3 Export Environment Setup for Apple Silicon ====="
echo "Setting up Python environment for model export on ARM64 architecture"

# Check if running on Apple Silicon
if [[ $(uname -m) != "arm64" ]]; then
    echo "WARNING: This script is designed for Apple Silicon (M1/M2/M3) Macs."
    echo "You appear to be running on a different architecture: $(uname -m)"
    read -p "Continue anyway? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 is required but not installed."
    echo "Please install Python 3 using Homebrew or from python.org"
    exit 1
fi

# Check Python version
PYTHON_VERSION=$(python3 --version | cut -d " " -f 2)
echo "Found Python $PYTHON_VERSION"

# Create a virtual environment if it doesn't exist
if [ ! -d "venv" ]; then
    echo "Creating Python virtual environment..."
    python3 -m venv venv
fi

# Activate virtual environment
echo "Activating virtual environment..."
source venv/bin/activate

# Upgrade pip
echo "Upgrading pip..."
python -m pip install --upgrade pip

# Install PyTorch with MPS support for Apple Silicon
echo "Installing PyTorch with Apple Silicon support..."
python -m pip install torch torchvision

# Check if PyTorch is using MPS
echo "Verifying PyTorch MPS support..."
python -c "import torch; print(f'PyTorch version: {torch.__version__}'); print(f'MPS available: {torch.backends.mps.is_available()}')"

# Install other dependencies
echo "Installing remaining dependencies..."
python -m pip install --no-build-isolation -r requirements_arm64.txt

# Verify ONNX installation
echo "Verifying ONNX installation..."
python -c "import onnx; print(f'ONNX version: {onnx.__version__}')"
python -c "import onnxruntime; print(f'ONNX Runtime version: {onnxruntime.__version__}'); print(f'Available providers: {onnxruntime.get_available_providers()}')"

echo "===== Setup complete! ====="
echo "To use this environment:"
echo "  1. Activate the virtual environment: source venv/bin/activate"
echo "  2. Run the export script: python export_arm64.py --model_type mobilenet --size 320 --test"
echo ""
echo "For additional options, run: python export_arm64.py --help" 