#!/bin/bash
# Script to run a full model export and analysis workflow

echo "===== DeepLabV3 Model Export and Analysis Workflow ====="
echo "This script will:"
echo "1. Export MobileNet and ResNet models at different resolutions"
echo "2. Analyze test results (if available)"
echo "3. Compare model performance"

# Create necessary directories
mkdir -p models
mkdir -p WallDetectionTests

# Check if Python is installed
if ! command -v python &> /dev/null; then
    echo "Python is not installed. Please install Python to continue."
    exit 1
fi

# Check for required packages
echo "\nChecking for required Python packages..."
REQUIRED_PACKAGES=("torch" "torchvision" "onnx" "numpy" "matplotlib" "pillow")
MISSING_PACKAGES=()

for package in "${REQUIRED_PACKAGES[@]}"; do
    python -c "import $package" 2>/dev/null
    if [ $? -ne 0 ]; then
        MISSING_PACKAGES+=("$package")
    fi
done

if [ ${#MISSING_PACKAGES[@]} -ne 0 ]; then
    echo "The following required packages are missing:"
    for package in "${MISSING_PACKAGES[@]}"; do
        echo "- $package"
    done
    
    read -p "Do you want to install them now? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        pip install ${MISSING_PACKAGES[@]}
    else
        echo "Please install the missing packages and run the script again."
        exit 1
    fi
fi

echo "\n===== Step 1: Export MobileNet Models ====="
cd "$(dirname "$0")/model_export"

echo "Exporting MobileNet models at different resolutions..."
python export_mobilenet.py --input_size 224 --output_dir ../../models
python export_mobilenet.py --input_size 320 --output_dir ../../models
python export_mobilenet.py --input_size 512 --output_dir ../../models

echo "\n===== Step 2: Analyze Test Results ====="
cd ../analysis_tools

# Check if test results exist
if [ ! "$(ls -A ../../WallDetectionTests 2>/dev/null)" ]; then
    echo "No test results found in WallDetectionTests directory."
    echo "Please run tests in Unity using the WallDetectionTester component first."
else
    echo "Analyzing test results..."
    python analyze_test_results.py --dir ../../WallDetectionTests --report
    
    echo "\n===== Step 3: Compare Model Performance ====="
    echo "Comparing model performance..."
    python compare_models.py --dir ../../WallDetectionTests
fi

echo "\n===== Workflow Completed ====="
echo "Next steps:"
echo "1. Import the exported models into your Unity project"
echo "2. Test both ResNet and MobileNet models using DeepLabModelTester"
echo "3. Review the analysis reports to optimize performance" 