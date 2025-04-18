#!/bin/bash
# Script to check the environment for DeepLabV3 model export

# Check if Python is installed
if ! command -v python &> /dev/null; then
    if command -v python3 &> /dev/null; then
        PYTHON_CMD="python3"
    else
        echo "Python not found. Please install Python 3.x before running this script."
        exit 1
    fi
else
    PYTHON_CMD="python"
fi

# Run the environment check script
$PYTHON_CMD check_environment.py 