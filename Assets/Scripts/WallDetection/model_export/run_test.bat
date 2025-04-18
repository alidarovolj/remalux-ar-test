@echo off
:: Script to run the test_export.py script for DeepLabV3 model export

:: Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Python not found. Please install Python 3.x before running this script.
    pause
    exit /b 1
)

:: Check if requirements are installed
echo Checking dependencies...
python -c "import torch, torchvision, onnx, numpy, matplotlib, PIL" >nul 2>&1
if %errorlevel% neq 0 (
    echo Some dependencies are missing. Installing required packages...
    python -m pip install -r requirements.txt
    if %errorlevel% neq 0 (
        echo Failed to install dependencies. Please install them manually:
        echo pip install -r requirements.txt
        pause
        exit /b 1
    )
)

echo Starting DeepLabV3 model export test...
echo This will download a sample image and test the model export functionality.
echo.

:: Run the test script
python test_export.py

if %errorlevel% equ 0 (
    echo.
    echo Test completed. If successful, you should see test results in the 'test_export\test_results' directory.
    echo To export all model sizes, run 'export_all_sizes.bat'
) else (
    echo.
    echo Test failed. Please check the error messages above.
)

pause 