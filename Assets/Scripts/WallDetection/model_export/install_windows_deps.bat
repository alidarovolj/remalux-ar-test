@echo off
setlocal enabledelayedexpansion

echo "Installing dependencies for model export on Windows..."

:: Check if Python is installed
where python >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Python not found. Please install Python 3.8 or later.
    goto :end
)

:: Check Python version
for /f "tokens=2" %%V in ('python --version 2^>^&1') do set py_version=%%V
echo Python version: %py_version%

:: Create and activate virtual environment if it doesn't exist
if not exist "venv" (
    echo Creating virtual environment...
    python -m venv venv
) else (
    echo Virtual environment already exists.
)

echo Activating virtual environment...
call venv\Scripts\activate.bat

:: Upgrade pip
echo Upgrading pip...
python -m pip install --upgrade pip

:: Install PyTorch with CUDA support
echo Installing PyTorch...
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118

:: Install ONNX and other dependencies
echo Installing ONNX and other dependencies...
pip install onnx onnxruntime onnxruntime-gpu matplotlib pillow

:: Verify installations
echo Verifying installations...
python -c "import torch; print('PyTorch version:', torch.__version__); print('CUDA available:', torch.cuda.is_available())"
python -c "import torchvision; print('TorchVision version:', torchvision.__version__)"
python -c "import onnx; print('ONNX version:', onnx.__version__)"
python -c "import onnxruntime; print('ONNX Runtime version:', onnxruntime.__version__)"

echo.
echo Installation complete!
echo To use this environment, activate it with: venv\Scripts\activate.bat
echo Then run the export script with: python export_mobilenet.py

:end
pause 