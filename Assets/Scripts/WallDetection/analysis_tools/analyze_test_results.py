#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import json
import glob
import argparse
import numpy as np
import matplotlib.pyplot as plt
from PIL import Image
from datetime import datetime

def load_test_data(base_dir="WallDetectionTests"):
    """Load all test data from the specified directory."""
    if not os.path.exists(base_dir):
        print(f"Error: Test directory {base_dir} not found")
        return None
    
    test_data = []
    
    # Find all JSON files (metadata)
    json_files = glob.glob(os.path.join(base_dir, "*.json"))
    
    for json_file in json_files:
        base_name = os.path.splitext(os.path.basename(json_file))[0]
        
        # Find related image files
        original_file = os.path.join(base_dir, f"{base_name}_original.png")
        mask_file = os.path.join(base_dir, f"{base_name}_mask.png")
        result_file = os.path.join(base_dir, f"{base_name}_result.png")
        
        if os.path.exists(original_file) and os.path.exists(mask_file) and os.path.exists(result_file):
            # Load metadata
            with open(json_file, 'r') as f:
                metadata = json.load(f)
                
            test_data.append({
                'id': base_name,
                'metadata': metadata,
                'original_path': original_file,
                'mask_path': mask_file,
                'result_path': result_file
            })
    
    print(f"Loaded {len(test_data)} test samples from {base_dir}")
    return test_data

def analyze_performance(test_data):
    """Analyze performance metrics from test data."""
    if not test_data:
        return
    
    processing_times = [sample['metadata']['processingTimeMs'] for sample in test_data if 'processingTimeMs' in sample['metadata']]
    if not processing_times:
        print("No processing time data found in test samples")
        return
    
    avg_time = np.mean(processing_times)
    min_time = np.min(processing_times)
    max_time = np.max(processing_times)
    std_time = np.std(processing_times)
    fps = 1000 / avg_time if avg_time > 0 else 0
    
    print(f"\nPerformance Metrics:")
    print(f"Average processing time: {avg_time:.2f} ms")
    print(f"Min processing time: {min_time:.2f} ms")
    print(f"Max processing time: {max_time:.2f} ms")
    print(f"Standard deviation: {std_time:.2f} ms")
    print(f"Average FPS: {fps:.2f}")
    
    # Create histogram of processing times
    plt.figure(figsize=(10, 6))
    plt.hist(processing_times, bins=20, alpha=0.7, color='blue')
    plt.title('Distribution of Processing Times')
    plt.xlabel('Processing Time (ms)')
    plt.ylabel('Number of Samples')
    plt.axvline(avg_time, color='red', linestyle='dashed', linewidth=1, label=f'Mean: {avg_time:.2f} ms')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # Save figure
    plt.savefig(os.path.join(os.path.dirname(test_data[0]['original_path']), 'processing_time_histogram.png'))
    plt.close()

def analyze_quality(test_data):
    """Analyze quality metrics from test data."""
    if not test_data:
        return
    
    wall_pixel_percentages = [sample['metadata']['wallPixelPercentage'] for sample in test_data 
                              if 'wallPixelPercentage' in sample['metadata']]
    
    if not wall_pixel_percentages:
        print("No wall pixel percentage data found in test samples")
        return
    
    avg_percentage = np.mean(wall_pixel_percentages)
    min_percentage = np.min(wall_pixel_percentages)
    max_percentage = np.max(wall_pixel_percentages)
    std_percentage = np.std(wall_pixel_percentages)
    
    print(f"\nQuality Metrics:")
    print(f"Average wall pixel percentage: {avg_percentage:.2f}%")
    print(f"Min wall pixel percentage: {min_percentage:.2f}%")
    print(f"Max wall pixel percentage: {max_percentage:.2f}%")
    print(f"Standard deviation: {std_percentage:.2f}%")
    
    # Create histogram of wall pixel percentages
    plt.figure(figsize=(10, 6))
    plt.hist(wall_pixel_percentages, bins=20, alpha=0.7, color='green')
    plt.title('Distribution of Wall Pixel Percentages')
    plt.xlabel('Wall Pixel Percentage (%)')
    plt.ylabel('Number of Samples')
    plt.axvline(avg_percentage, color='red', linestyle='dashed', linewidth=1, label=f'Mean: {avg_percentage:.2f}%')
    plt.legend()
    plt.grid(True, alpha=0.3)
    
    # Save figure
    plt.savefig(os.path.join(os.path.dirname(test_data[0]['original_path']), 'wall_pixel_percentage_histogram.png'))
    plt.close()

def generate_report(test_data, output_dir=None):
    """Generate a comprehensive HTML report of test results."""
    if not test_data:
        return
    
    if output_dir is None:
        output_dir = os.path.dirname(test_data[0]['original_path'])
    
    # Calculate statistics
    processing_times = [sample['metadata']['processingTimeMs'] for sample in test_data if 'processingTimeMs' in sample['metadata']]
    wall_percentages = [sample['metadata']['wallPixelPercentage'] for sample in test_data if 'wallPixelPercentage' in sample['metadata']]
    
    # Generate HTML content
    html_content = f"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Wall Detection Test Results</title>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; }}
            h1, h2, h3 {{ color: #333; }}
            table {{ border-collapse: collapse; width: 100%; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #f2f2f2; }}
            tr:nth-child(even) {{ background-color: #f9f9f9; }}
            .sample-container {{ display: flex; flex-wrap: wrap; gap: 10px; margin-top: 20px; }}
            .sample-card {{ border: 1px solid #ddd; padding: 10px; width: 300px; }}
            .sample-card img {{ width: 100%; height: auto; }}
            .stats {{ margin: 20px 0; padding: 15px; background-color: #f8f8f8; border-radius: 4px; }}
        </style>
    </head>
    <body>
        <h1>Wall Detection Test Results</h1>
        <p>Report generated on {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        
        <div class="stats">
            <h2>Performance Statistics</h2>
            <table>
                <tr><th>Metric</th><th>Value</th></tr>
                <tr><td>Number of test samples</td><td>{len(test_data)}</td></tr>
                <tr><td>Average processing time</td><td>{np.mean(processing_times):.2f} ms</td></tr>
                <tr><td>Min processing time</td><td>{np.min(processing_times):.2f} ms</td></tr>
                <tr><td>Max processing time</td><td>{np.max(processing_times):.2f} ms</td></tr>
                <tr><td>Processing time standard deviation</td><td>{np.std(processing_times):.2f} ms</td></tr>
                <tr><td>Estimated FPS</td><td>{1000 / np.mean(processing_times):.2f}</td></tr>
            </table>
            
            <h2>Quality Statistics</h2>
            <table>
                <tr><th>Metric</th><th>Value</th></tr>
                <tr><td>Average wall pixel percentage</td><td>{np.mean(wall_percentages):.2f}%</td></tr>
                <tr><td>Min wall pixel percentage</td><td>{np.min(wall_percentages):.2f}%</td></tr>
                <tr><td>Max wall pixel percentage</td><td>{np.max(wall_percentages):.2f}%</td></tr>
                <tr><td>Wall percentage standard deviation</td><td>{np.std(wall_percentages):.2f}%</td></tr>
            </table>
        </div>
        
        <h2>Test Samples</h2>
        <div class="sample-container">
    """
    
    # Add sample cards
    for sample in test_data[:30]:  # Limit to 30 samples to keep the report manageable
        rel_original = os.path.relpath(sample['original_path'], output_dir)
        rel_mask = os.path.relpath(sample['mask_path'], output_dir)
        rel_result = os.path.relpath(sample['result_path'], output_dir)
        
        html_content += f"""
        <div class="sample-card">
            <h3>Sample {sample['id']}</h3>
            <p>Processing time: {sample['metadata'].get('processingTimeMs', 'N/A'):.2f} ms</p>
            <p>Wall pixel percentage: {sample['metadata'].get('wallPixelPercentage', 'N/A'):.2f}%</p>
            <p>Resolution: {sample['metadata'].get('resolution', 'N/A')}</p>
            
            <h4>Original</h4>
            <img src="{rel_original}" alt="Original Image">
            
            <h4>Mask</h4>
            <img src="{rel_mask}" alt="Wall Mask">
            
            <h4>Result</h4>
            <img src="{rel_result}" alt="Final Result">
        </div>
        """
    
    html_content += """
        </div>
    </body>
    </html>
    """
    
    # Write the HTML report
    report_path = os.path.join(output_dir, 'wall_detection_report.html')
    with open(report_path, 'w') as f:
        f.write(html_content)
    
    print(f"\nReport generated at: {report_path}")

def main():
    parser = argparse.ArgumentParser(description='Analyze Wall Detection Test Results')
    parser.add_argument('--dir', type=str, default='WallDetectionTests', help='Directory containing test results')
    parser.add_argument('--report', action='store_true', help='Generate HTML report')
    args = parser.parse_args()
    
    test_data = load_test_data(args.dir)
    if test_data:
        analyze_performance(test_data)
        analyze_quality(test_data)
        if args.report:
            generate_report(test_data)

if __name__ == "__main__":
    main() 