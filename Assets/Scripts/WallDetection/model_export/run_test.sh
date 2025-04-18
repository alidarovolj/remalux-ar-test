#!/bin/bash
# Script to run the test_export.py script for DeepLabV3 model export

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

# Check if requirements are installed
echo "Checking dependencies..."
$PYTHON_CMD -c "import torch, torchvision, onnx, numpy, matplotlib, PIL" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "Some dependencies are missing. Installing required packages..."
    $PYTHON_CMD -m pip install -r requirements.txt
    if [ $? -ne 0 ]; then
        echo "Failed to install dependencies. Please install them manually:"
        echo "pip install -r requirements.txt"
        exit 1
    fi
fi

echo "Starting DeepLabV3 model export test..."
echo "This will download a sample image and test the model export functionality."
echo ""

# Run the test script
$PYTHON_CMD test_export.py

if [ $? -eq 0 ]; then
    echo ""
    echo "Test completed. If successful, you should see test results in the 'test_export/test_results' directory."
    echo "To export all model sizes, run './export_all_sizes.sh'"
else
    echo ""
    echo "Test failed. Please check the error messages above."
fi 