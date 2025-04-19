@echo off
:: Script to analyze DeepLabV3 models and generate comparison reports

:: Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Python not found. Please install Python 3.x before running this script.
    pause
    exit /b 1
)

:: Check if required packages are installed
echo Checking dependencies...
python -c "import onnx, numpy, matplotlib, tabulate" >nul 2>&1
if %errorlevel% neq 0 (
    echo Some dependencies are missing. Installing required packages...
    python -m pip install onnx numpy matplotlib tabulate
    if %errorlevel% neq 0 (
        echo Failed to install dependencies. Please install them manually:
        echo pip install onnx numpy matplotlib tabulate
        pause
        exit /b 1
    )
)

:: Parse command-line arguments
set MODELS_DIR=models
set OUTPUT_DIR=model_analysis
set CSV_FLAG=

:: Process arguments
for %%a in (%*) do (
    if "%%a"=="-h" (
        goto :help
    ) else if "%%a"=="--help" (
        goto :help
    ) else if "%%a"=="--csv" (
        set CSV_FLAG=--csv
    ) else (
        for /f "tokens=1,2 delims==" %%b in ("%%a") do (
            if "%%b"=="--models_dir" (
                set MODELS_DIR=%%c
            ) else if "%%b"=="--output_dir" (
                set OUTPUT_DIR=%%c
            )
        )
    )
)

goto :main

:help
echo Usage: analyze_models.bat [--models_dir=DIR] [--output_dir=DIR] [--csv]
echo   --models_dir=DIR : Directory containing models to analyze (default: models)
echo   --output_dir=DIR : Directory to save analysis reports (default: model_analysis)
echo   --csv            : Export model data to CSV file
exit /b 0

:main
echo Starting DeepLabV3 model analysis...
echo Models directory: %MODELS_DIR%
echo Output directory: %OUTPUT_DIR%
if defined CSV_FLAG (
    echo CSV export: Enabled
)
echo.

:: Run the analysis script
python analyze_models.py --models_dir "%MODELS_DIR%" --output_dir "%OUTPUT_DIR%" %CSV_FLAG%

if %errorlevel% equ 0 (
    echo.
    echo Analysis completed. Results are available in the '%OUTPUT_DIR%' directory.
    echo Open the HTML report in a browser for detailed information.
) else (
    echo.
    echo Analysis failed. Please check the error messages above.
)

pause 