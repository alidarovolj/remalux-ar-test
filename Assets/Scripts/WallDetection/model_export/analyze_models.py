#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Model analysis script for DeepLabV3 model export
This script analyzes exported ONNX models and generates a comparison report
"""

import os
import sys
import argparse
import onnx
import numpy as np
import json
import csv
import time
from pathlib import Path
from tabulate import tabulate
import matplotlib.pyplot as plt
from PIL import Image

def get_model_size(model_path):
    """Get the file size of a model in MB"""
    return os.path.getsize(model_path) / (1024 * 1024)

def count_model_parameters(model_path):
    """Count the number of parameters in an ONNX model"""
    try:
        model = onnx.load(model_path)
        params_count = 0
        
        # Count parameters in initializers
        for initializer in model.graph.initializer:
            shape = [dim.dim_value for dim in initializer.type.tensor_type.shape.dim]
            params_count += np.prod(shape)
        
        return params_count
    except Exception as e:
        print(f"Error counting parameters in {model_path}: {e}")
        return 0

def count_ops(model_path):
    """Estimate the number of operations in an ONNX model"""
    try:
        model = onnx.load(model_path)
        ops_count = 0
        
        # Count nodes by type
        op_types = {}
        for node in model.graph.node:
            if node.op_type in op_types:
                op_types[node.op_type] += 1
            else:
                op_types[node.op_type] = 1
            ops_count += 1
        
        return ops_count, op_types
    except Exception as e:
        print(f"Error counting operations in {model_path}: {e}")
        return 0, {}

def get_input_size(model_path):
    """Get the input dimensions of an ONNX model"""
    try:
        model = onnx.load(model_path)
        for input_info in model.graph.input:
            shape = input_info.type.tensor_type.shape
            # Extract dimensions (usually [batch_size, channels, height, width])
            dims = [dim.dim_value for dim in shape.dim]
            if len(dims) == 4:  # Standard image input format
                return dims[2], dims[3]  # Height and width
        return None
    except Exception as e:
        print(f"Error getting input size for {model_path}: {e}")
        return None

def analyze_model(model_path):
    """Analyze an ONNX model and return its characteristics"""
    filename = os.path.basename(model_path)
    
    # Extract model type and resolution from filename
    # Expected format: deeplabv3_mobilenetv3_320x320_YYYYMMDD_HHMMSS.onnx
    parts = filename.split('_')
    model_type = parts[1] if len(parts) > 1 else "unknown"
    
    # Try to extract resolution from the filename
    resolution = None
    for part in parts:
        if 'x' in part and part.replace('x', '').isdigit():
            resolution = part
            break
    
    size_mb = get_model_size(model_path)
    params_count = count_model_parameters(model_path)
    ops_count, op_types = count_ops(model_path)
    input_dims = get_input_size(model_path)
    
    return {
        "filename": filename,
        "path": model_path,
        "model_type": model_type,
        "resolution": resolution,
        "size_mb": size_mb,
        "params_count": params_count,
        "ops_count": ops_count,
        "op_types": op_types,
        "input_dims": input_dims
    }

def find_models(directory):
    """Find all ONNX models in a directory"""
    models = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.onnx') and 'deeplabv3' in file.lower():
                models.append(os.path.join(root, file))
    return models

def generate_html_report(models_data, output_file):
    """Generate an HTML report for model comparison"""
    html = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>DeepLabV3 Models Comparison</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 20px; }
            h1, h2 { color: #333; }
            table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }
            th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
            th { background-color: #f2f2f2; }
            tr:nth-child(even) { background-color: #f9f9f9; }
            .chart-container { width: 800px; height: 400px; margin: 20px 0; }
            .model-details { margin: 20px 0; padding: 10px; background-color: #f5f5f5; border-radius: 5px; }
            .highlight { background-color: #e6f7ff; }
        </style>
    </head>
    <body>
        <h1>DeepLabV3 Models Comparison</h1>
        <p>Generated on """ + time.strftime("%Y-%m-%d %H:%M:%S") + """</p>
        
        <h2>Models Overview</h2>
        <table>
            <tr>
                <th>Filename</th>
                <th>Model Type</th>
                <th>Resolution</th>
                <th>Size (MB)</th>
                <th>Parameters</th>
                <th>Operations</th>
            </tr>
    """
    
    # Add model rows
    for model in models_data:
        html += f"""
            <tr>
                <td>{model['filename']}</td>
                <td>{model['model_type']}</td>
                <td>{model['resolution']}</td>
                <td>{model['size_mb']:.2f}</td>
                <td>{model['params_count']:,}</td>
                <td>{model['ops_count']}</td>
            </tr>
        """
    
    html += """
        </table>
        
        <h2>Size Comparison</h2>
        <div class="chart-container">
            <img src="model_size_chart.png" alt="Model Size Comparison" width="800">
        </div>
        
        <h2>Parameters Comparison</h2>
        <div class="chart-container">
            <img src="model_params_chart.png" alt="Model Parameters Comparison" width="800">
        </div>
        
        <h2>Detailed Analysis</h2>
    """
    
    # Add detailed analysis for each model
    for model in models_data:
        html += f"""
        <div class="model-details">
            <h3>{model['filename']}</h3>
            <p><strong>Type:</strong> {model['model_type']}</p>
            <p><strong>Resolution:</strong> {model['resolution']}</p>
            <p><strong>Size:</strong> {model['size_mb']:.2f} MB</p>
            <p><strong>Parameters:</strong> {model['params_count']:,}</p>
            <p><strong>Operations:</strong> {model['ops_count']}</p>
            <p><strong>Input Dimensions:</strong> {model['input_dims']}</p>
            
            <h4>Operation Types:</h4>
            <table>
                <tr>
                    <th>Operation</th>
                    <th>Count</th>
                </tr>
        """
        
        # Add operation types
        for op_type, count in model['op_types'].items():
            html += f"""
                <tr>
                    <td>{op_type}</td>
                    <td>{count}</td>
                </tr>
            """
            
        html += """
            </table>
        </div>
        """
    
    html += """
        <h2>Recommendations</h2>
        <ul>
            <li><strong>Mobile Devices:</strong> Use MobileNetV3 with 224x224 or 320x320 resolution</li>
            <li><strong>Tablets/Mid-range Devices:</strong> Use MobileNetV3 with 320x320 or 512x512 resolution</li>
            <li><strong>High-end Devices:</strong> Use ResNet50 with 512x512 or 768x768 resolution if available</li>
        </ul>
        
        <h2>Next Steps</h2>
        <ol>
            <li>Import selected models into Unity project</li>
            <li>Test with DeepLabModelTester for on-device performance</li>
            <li>Validate wall detection quality with WallDetectionTester</li>
            <li>Configure appropriate resolution presets per device target</li>
        </ol>
        
        <p>Generated by model_analyzer.py</p>
    </body>
    </html>
    """
    
    with open(output_file, 'w') as f:
        f.write(html)
    
    print(f"HTML report generated: {output_file}")

def generate_charts(models_data, output_dir):
    """Generate charts for model comparison"""
    # Model size chart
    plt.figure(figsize=(10, 6))
    models = [f"{m['model_type']}_{m['resolution']}" for m in models_data]
    sizes = [m['size_mb'] for m in models_data]
    
    plt.bar(range(len(models)), sizes, color='skyblue')
    plt.xticks(range(len(models)), models, rotation=45, ha='right')
    plt.ylabel('Size (MB)')
    plt.title('Model Size Comparison')
    plt.tight_layout()
    plt.savefig(os.path.join(output_dir, 'model_size_chart.png'))
    plt.close()
    
    # Parameters chart
    plt.figure(figsize=(10, 6))
    params = [m['params_count'] / 1_000_000 for m in models_data]  # Convert to millions
    
    plt.bar(range(len(models)), params, color='lightgreen')
    plt.xticks(range(len(models)), models, rotation=45, ha='right')
    plt.ylabel('Parameters (millions)')
    plt.title('Model Parameters Comparison')
    plt.tight_layout()
    plt.savefig(os.path.join(output_dir, 'model_params_chart.png'))
    plt.close()

def export_to_csv(models_data, output_file):
    """Export model comparison data to CSV"""
    with open(output_file, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['Filename', 'Model Type', 'Resolution', 'Size (MB)', 'Parameters', 'Operations'])
        
        for model in models_data:
            writer.writerow([
                model['filename'],
                model['model_type'],
                model['resolution'],
                f"{model['size_mb']:.2f}",
                model['params_count'],
                model['ops_count']
            ])
    
    print(f"CSV report generated: {output_file}")

def main():
    parser = argparse.ArgumentParser(description='Analyze DeepLabV3 ONNX models and generate comparison report')
    parser.add_argument('--models_dir', type=str, default='models',
                        help='Directory containing models to analyze')
    parser.add_argument('--output_dir', type=str, default='model_analysis',
                        help='Directory to save analysis reports')
    parser.add_argument('--csv', action='store_true',
                        help='Export model data to CSV file')
    args = parser.parse_args()
    
    print(f"\n{'='*50}")
    print("DeepLabV3 Models Analysis")
    print(f"{'='*50}\n")
    
    # Find models
    print(f"Searching for models in '{args.models_dir}'...")
    model_paths = find_models(args.models_dir)
    
    if not model_paths:
        print(f"No DeepLabV3 ONNX models found in '{args.models_dir}'")
        print("Please export models first using export_mobilenet.py or export_all_sizes.sh/bat")
        return
    
    print(f"Found {len(model_paths)} models to analyze")
    
    # Create output directory
    os.makedirs(args.output_dir, exist_ok=True)
    
    # Analyze models
    models_data = []
    for model_path in model_paths:
        print(f"Analyzing model: {os.path.basename(model_path)}")
        model_data = analyze_model(model_path)
        models_data.append(model_data)
    
    # Generate summary table
    print("\nModel Comparison Summary:")
    table_data = []
    for model in models_data:
        table_data.append([
            model['filename'],
            model['model_type'],
            model['resolution'],
            f"{model['size_mb']:.2f} MB",
            f"{model['params_count']:,}",
            model['ops_count']
        ])
    
    headers = ["Filename", "Model Type", "Resolution", "Size", "Parameters", "Operations"]
    print(tabulate(table_data, headers=headers, tablefmt="grid"))
    
    # Generate charts
    print("\nGenerating comparison charts...")
    generate_charts(models_data, args.output_dir)
    
    # Generate HTML report
    html_output = os.path.join(args.output_dir, "model_comparison.html")
    generate_html_report(models_data, html_output)
    
    # Export to CSV if requested
    if args.csv:
        csv_output = os.path.join(args.output_dir, "model_comparison.csv")
        export_to_csv(models_data, csv_output)
    
    print(f"\nAnalysis complete. Reports saved to: {args.output_dir}")
    print(f"HTML report: {html_output}")
    print("\nNext steps:")
    print("1. Review the model comparison report")
    print("2. Select suitable models for your target devices")
    print("3. Import selected models into your Unity project")
    print("4. Test with DeepLabModelTester component")

if __name__ == "__main__":
    main() 