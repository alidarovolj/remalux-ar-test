@echo off
REM Script to run a full model export and analysis workflow

echo ===== DeepLabV3 Model Export and Analysis Workflow =====
echo This script will:
echo 1. Export MobileNet and ResNet models at different resolutions
echo 2. Analyze test results (if available)
echo 3. Compare model performance

REM Create necessary directories
if not exist models mkdir models
if not exist WallDetectionTests mkdir WallDetectionTests

REM Check if Python is installed
python --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Python is not installed. Please install Python to continue.
    goto end
)

REM Check for required packages
echo.
echo Checking for required Python packages...
setlocal EnableDelayedExpansion
set REQUIRED_PACKAGES=torch torchvision onnx numpy matplotlib pillow
set MISSING=0

for %%p in (%REQUIRED_PACKAGES%) do (
    python -c "import %%p" >nul 2>&1
    if !ERRORLEVEL! neq 0 (
        echo - %%p is missing
        set MISSING=1
    )
)

if %MISSING% equ 1 (
    echo.
    echo Some required packages are missing.
    set /p INSTALL="Do you want to install them now? (y/n) "
    if /i "!INSTALL!"=="y" (
        pip install %REQUIRED_PACKAGES%
    ) else (
        echo Please install the missing packages and run the script again.
        goto end
    )
)

echo.
echo ===== Step 1: Export MobileNet Models =====
cd /d "%~dp0\model_export"

echo Exporting MobileNet models at different resolutions...
python export_mobilenet.py --input_size 224 --output_dir ../../models
python export_mobilenet.py --input_size 320 --output_dir ../../models
python export_mobilenet.py --input_size 512 --output_dir ../../models

echo.
echo ===== Step 2: Analyze Test Results =====
cd /d "%~dp0\analysis_tools"

REM Check if test results exist
dir "..\..\WallDetectionTests\*.json" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo No test results found in WallDetectionTests directory.
    echo Please run tests in Unity using the WallDetectionTester component first.
) else (
    echo Analyzing test results...
    python analyze_test_results.py --dir ../../WallDetectionTests --report
    
    echo.
    echo ===== Step 3: Compare Model Performance =====
    echo Comparing model performance...
    python compare_models.py --dir ../../WallDetectionTests
)

echo.
echo ===== Workflow Completed =====
echo Next steps:
echo 1. Import the exported models into your Unity project
echo 2. Test both ResNet and MobileNet models using DeepLabModelTester
echo 3. Review the analysis reports to optimize performance

:end
pause 