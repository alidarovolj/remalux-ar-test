@echo off
echo Installing dependencies for model export on Windows...

:: Check if Python is installed
python --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Python not found. Please install Python 3.8 or newer.
    exit /b 1
)

:: Create virtual environment if it doesn't exist
if not exist venv (
    echo Creating Python virtual environment...
    python -m venv venv
)

:: Activate virtual environment
echo Activating virtual environment...
call venv\Scripts\activate.bat

:: Upgrade pip
echo Upgrading pip...
python -m pip install --upgrade pip

:: Install dependencies
echo Installing required packages...
pip install numpy
pip install Pillow
pip install matplotlib
pip install torch torchvision torchaudio
pip install onnx
pip install onnxruntime

echo.
echo Dependencies installed successfully.
echo To activate the environment, use: venv\Scripts\activate.bat
echo To run export script, use: python export_arm64.py --help 