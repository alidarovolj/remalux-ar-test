#!/bin/bash
# Script to analyze DeepLabV3 models and generate comparison reports

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

# Check if required packages are installed
echo "Checking dependencies..."
$PYTHON_CMD -c "import onnx, numpy, matplotlib, tabulate" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "Some dependencies are missing. Installing required packages..."
    $PYTHON_CMD -m pip install onnx numpy matplotlib tabulate
    if [ $? -ne 0 ]; then
        echo "Failed to install dependencies. Please install them manually:"
        echo "pip install onnx numpy matplotlib tabulate"
        exit 1
    fi
fi

# Parse command-line arguments
MODELS_DIR="models"
OUTPUT_DIR="model_analysis"
CSV_FLAG=""

# Process arguments
for arg in "$@"; do
    if [[ "$arg" == "--models_dir="* ]]; then
        MODELS_DIR="${arg#*=}"
    elif [[ "$arg" == "--output_dir="* ]]; then
        OUTPUT_DIR="${arg#*=}"
    elif [[ "$arg" == "--csv" ]]; then
        CSV_FLAG="--csv"
    elif [[ "$arg" == "-h" || "$arg" == "--help" ]]; then
        echo "Usage: ./analyze_models.sh [--models_dir=DIR] [--output_dir=DIR] [--csv]"
        echo "  --models_dir=DIR : Directory containing models to analyze (default: models)"
        echo "  --output_dir=DIR : Directory to save analysis reports (default: model_analysis)"
        echo "  --csv            : Export model data to CSV file"
        exit 0
    fi
done

echo "Starting DeepLabV3 model analysis..."
echo "Models directory: $MODELS_DIR"
echo "Output directory: $OUTPUT_DIR"
if [[ -n "$CSV_FLAG" ]]; then
    echo "CSV export: Enabled"
fi
echo ""

# Run the analysis script
$PYTHON_CMD analyze_models.py --models_dir "$MODELS_DIR" --output_dir "$OUTPUT_DIR" $CSV_FLAG

if [ $? -eq 0 ]; then
    echo ""
    echo "Analysis completed. Results are available in the '$OUTPUT_DIR' directory."
    echo "Open the HTML report in a browser for detailed information."
else
    echo ""
    echo "Analysis failed. Please check the error messages above."
fi 