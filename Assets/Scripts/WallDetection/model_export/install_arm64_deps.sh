#!/bin/bash
# install_arm64_deps.sh - Install dependencies for model export on Apple Silicon
# This script handles the proper installation of all dependencies avoiding ARM64 compatibility issues

set -e  # Exit on any error

echo "===== Installing dependencies for Apple Silicon (ARM64) ====="

# Check if we're on ARM64 architecture
if [[ $(uname -m) != "arm64" ]]; then
    echo "WARNING: This script is optimized for Apple Silicon (M1/M2/M3)."
    echo "Current architecture: $(uname -m)"
    read -p "Continue anyway? (y/n): " CONTINUE
    if [[ "$CONTINUE" != "y" ]]; then
        exit 0
    fi
fi

# Create virtual environment if it doesn't exist
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

# Install standard dependencies first
echo "Installing standard dependencies..."
pip install numpy Pillow matplotlib tqdm netron tabulate numpy-quaternion

# Install PyTorch with MPS support
echo "Installing PyTorch with MPS support for Apple Silicon..."
pip install torch torchvision

# Verify PyTorch installation
python -c "import torch; print(f'PyTorch version: {torch.__version__}'); print(f'MPS available: {torch.backends.mps.is_available()}')"

# Install ONNX with no-build-isolation to avoid SSE4.1 errors
echo "Installing ONNX packages with ARM64 compatibility flags..."
pip install --no-build-isolation onnx
pip install --no-build-isolation onnxruntime
pip install --no-build-isolation onnx-simplifier
pip install --no-build-isolation protobuf>=3.20.0,<4.0.0
pip install --no-build-isolation onnxoptimizer

# Verify installations
echo "Verifying installations..."
python -c "import onnx; print(f'ONNX version: {onnx.__version__}')"
python -c "import onnxruntime; print(f'ONNX Runtime version: {onnxruntime.__version__}'); print(f'Available providers: {onnxruntime.get_available_providers()}')"

echo "===== All dependencies installed successfully! ====="
echo "To activate this environment, run: source venv/bin/activate" 