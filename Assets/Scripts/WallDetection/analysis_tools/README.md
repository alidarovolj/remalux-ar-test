# Wall Detection Analysis Tools

This directory contains tools for analyzing the results of wall detection tests performed using the `WallDetectionTester` component.

## Requirements

To use these analysis tools, you need the following Python packages:
```
numpy
matplotlib
pillow
```

You can install them using pip:
```
pip install numpy matplotlib pillow
```

## analyze_test_results.py

This script analyzes the test results saved by the `WallDetectionTester` component.

### Usage

```
python analyze_test_results.py [--dir DIRECTORY] [--report]
```

### Parameters

- `--dir DIRECTORY`: The directory containing the test results (default: "WallDetectionTests")
- `--report`: Generate an HTML report with detailed analysis and visualizations

### Output

The script outputs:
1. Performance metrics (processing time, FPS)
2. Quality metrics (wall pixel percentage)
3. Histograms of processing times and wall pixel percentages
4. An HTML report (if `--report` flag is specified)

### Example

```
# Analyze test results in the default directory and generate a report
python analyze_test_results.py --report

# Analyze test results in a specific directory
python analyze_test_results.py --dir "/path/to/test/results"
```

## HTML Report

The generated HTML report includes:
- Performance statistics
- Quality statistics
- Visual comparison of test samples (original image, mask, result)
- Processing time and wall percentage for each sample

The report is saved in the same directory as the test results.

## Integration with Unity

To integrate these analysis tools with your Unity project:

1. Run tests using the `WallDetectionTester` component in Unity
2. Wait for test results to be saved to the specified directory
3. Run the analysis script to analyze the results and generate a report
4. Review the report to optimize DeepLabDecoder parameters

## Workflow Example

1. Set up a test scene with different resolution settings, lighting conditions, etc.
2. Run tests to collect data samples
3. Run the analysis script:
   ```
   python analyze_test_results.py --report
   ```
4. Open the generated HTML report to compare results
5. Adjust DeepLabDecoder parameters based on the analysis
6. Repeat the process to fine-tune parameters