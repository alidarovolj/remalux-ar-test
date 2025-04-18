@echo off
:: Script to check the environment for DeepLabV3 model export

:: Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Python not found. Please install Python 3.x before running this script.
    pause
    exit /b 1
)

:: Run the environment check script
python check_environment.py
pause 