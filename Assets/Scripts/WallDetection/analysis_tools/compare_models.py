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

def find_model_results(base_dir="WallDetectionTests", model_identifier=None):
    """Find test results for specific models."""
    if not os.path.exists(base_dir):
        print(f"Error: Test directory {base_dir} not found")
        return None
    
    # Find all JSON files (metadata)
    json_files = glob.glob(os.path.join(base_dir, "*.json"))
    model_results = {}
    
    for json_file in json_files:
        with open(json_file, 'r') as f:
            try:
                metadata = json.load(f)
                model_name = metadata.get('modelName', 'unknown')
                
                if model_identifier and model_identifier.lower() not in model_name.lower():
                    continue
                    
                if model_name not in model_results:
                    model_results[model_name] = []
                
                base_name = os.path.splitext(os.path.basename(json_file))[0]
                original_file = os.path.join(base_dir, f"{base_name}_original.png")
                mask_file = os.path.join(base_dir, f"{base_name}_mask.png")
                result_file = os.path.join(base_dir, f"{base_name}_result.png")
                
                if os.path.exists(original_file) and os.path.exists(mask_file) and os.path.exists(result_file):
                    model_results[model_name].append({
                        'id': base_name,
                        'metadata': metadata,
                        'original_path': original_file,
                        'mask_path': mask_file,
                        'result_path': result_file
                    })
            except json.JSONDecodeError:
                print(f"Warning: Could not parse JSON file: {json_file}")
                continue
    
    # Print summary of found models
    for model_name, results in model_results.items():
        print(f"Found {len(results)} test results for model: {model_name}")
    
    return model_results

def compare_performance(model_results):
    """Compare performance metrics between models."""
    if not model_results:
        return
        
    models = list(model_results.keys())
    avg_times = []
    fps_values = []
    wall_percentages = []
    input_sizes = []
    
    for model in models:
        results = model_results[model]
        if not results:
            continue
            
        # Extract processing times
        processing_times = [r['metadata']['processingTimeMs'] for r in results if 'processingTimeMs' in r['metadata']]
        if processing_times:
            avg_time = np.mean(processing_times)
            avg_times.append(avg_time)
            fps_values.append(1000 / avg_time if avg_time > 0 else 0)
        else:
            avg_times.append(0)
            fps_values.append(0)
        
        # Extract wall percentages
        wall_perc = [r['metadata']['wallPixelPercentage'] for r in results if 'wallPixelPercentage' in r['metadata']]
        if wall_perc:
            wall_percentages.append(np.mean(wall_perc))
        else:
            wall_percentages.append(0)
            
        # Extract input sizes
        if results[0]['metadata'].get('resolution'):
            try:
                # Extract numeric resolution (e.g., "320x320" -> 320)
                resolution = results[0]['metadata']['resolution']
                size = int(resolution.split('x')[0])
                input_sizes.append(size)
            except (ValueError, IndexError):
                input_sizes.append(0)
        else:
            input_sizes.append(0)
    
    # Plot performance comparison
    if len(models) > 1:
        plt.figure(figsize=(12, 8))
        
        # Plot processing times
        plt.subplot(2, 2, 1)
        plt.bar(models, avg_times, color='blue', alpha=0.7)
        plt.title('Average Processing Time (ms)')
        plt.ylabel('Time (ms)')
        plt.xticks(rotation=45)
        
        # Plot FPS
        plt.subplot(2, 2, 2)
        plt.bar(models, fps_values, color='green', alpha=0.7)
        plt.title('Average FPS')
        plt.ylabel('Frames per Second')
        plt.xticks(rotation=45)
        
        # Plot wall percentages
        plt.subplot(2, 2, 3)
        plt.bar(models, wall_percentages, color='red', alpha=0.7)
        plt.title('Average Wall Pixel Percentage')
        plt.ylabel('Percentage (%)')
        plt.xticks(rotation=45)
        
        # Plot performance vs size (if available)
        if any(input_sizes):
            plt.subplot(2, 2, 4)
            plt.scatter(input_sizes, fps_values, s=100, alpha=0.7)
            for i, model in enumerate(models):
                plt.annotate(model, (input_sizes[i], fps_values[i]), fontsize=8)
            plt.title('Performance vs Input Size')
            plt.xlabel('Input Size (pixels)')
            plt.ylabel('FPS')
        
        plt.tight_layout()
        plt.savefig(os.path.join(os.path.dirname(model_results[models[0]][0]['original_path']), 'model_comparison.png'))
        plt.close()
        
        # Print comparison
        print("\nModel Performance Comparison:")
        print(f"{'Model':<20} {'Avg Time (ms)':<15} {'FPS':<10} {'Wall %':<10} {'Input Size':<10}")
        print("-" * 65)
        for i, model in enumerate(models):
            input_size_str = f"{input_sizes[i]}x{input_sizes[i]}" if input_sizes[i] > 0 else "N/A"
            print(f"{model:<20} {avg_times[i]:<15.2f} {fps_values[i]:<10.2f} {wall_percentages[i]:<10.2f} {input_size_str:<10}")
    else:
        print("Need at least two models to compare.")

def create_visual_comparison(model_results, output_dir=None, sample_count=5):
    """Create visual comparison of model results."""
    if not model_results or len(model_results) < 2:
        print("Need at least two models to compare visually.")
        return
    
    # Use first model's directory as default output directory
    models = list(model_results.keys())
    if not output_dir:
        output_dir = os.path.dirname(model_results[models[0]][0]['original_path'])
    
    # Determine common originals across models
    # For simplicity, we'll just take the first few samples from the first model
    samples_to_compare = min(sample_count, min(len(results) for results in model_results.values()))
    
    # Create comparison directory
    comparison_dir = os.path.join(output_dir, "model_comparisons")
    os.makedirs(comparison_dir, exist_ok=True)
    
    # Generate comparison HTML
    html_content = f"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>DeepLabV3 Model Comparison</title>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; }}
            h1, h2, h3 {{ color: #333; }}
            .comparison-container {{ margin-bottom: 50px; }}
            .model-grid {{ display: grid; grid-template-columns: repeat({len(models)}, 1fr); gap: 10px; }}
            .model-cell {{ border: 1px solid #ddd; padding: 10px; }}
            .model-cell img {{ width: 100%; height: auto; }}
            .original-img {{ width: 100%; max-width: 500px; margin: 10px 0; }}
            table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #f2f2f2; }}
        </style>
    </head>
    <body>
        <h1>DeepLabV3 Model Comparison</h1>
        <p>Report generated on {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        
        <h2>Performance Summary</h2>
        <table>
            <tr>
                <th>Model</th>
                <th>Average Processing Time (ms)</th>
                <th>FPS</th>
                <th>Wall Pixel %</th>
                <th>Input Resolution</th>
            </tr>
    """
    
    # Add performance data
    for model in models:
        results = model_results[model]
        if not results:
            continue
            
        # Calculate averages
        processing_times = [r['metadata']['processingTimeMs'] for r in results if 'processingTimeMs' in r['metadata']]
        wall_perc = [r['metadata']['wallPixelPercentage'] for r in results if 'wallPixelPercentage' in r['metadata']]
        
        avg_time = np.mean(processing_times) if processing_times else 0
        fps = 1000 / avg_time if avg_time > 0 else 0
        avg_wall = np.mean(wall_perc) if wall_perc else 0
        
        # Get resolution
        resolution = "N/A"
        if 'resolution' in results[0]['metadata']:
            resolution = results[0]['metadata']['resolution']
        
        html_content += f"""
            <tr>
                <td>{model}</td>
                <td>{avg_time:.2f}</td>
                <td>{fps:.2f}</td>
                <td>{avg_wall:.2f}%</td>
                <td>{resolution}</td>
            </tr>
        """
    
    html_content += """
        </table>
        
        <h2>Visual Comparisons</h2>
    """
    
    # Add visual comparisons
    for i in range(samples_to_compare):
        html_content += f"""
        <div class="comparison-container">
            <h3>Sample {i+1}</h3>
        """
        
        # Add original image from first model
        original_path = model_results[models[0]][i]['original_path']
        original_filename = f"original_{i}.png"
        # Copy original to comparison directory
        Image.open(original_path).save(os.path.join(comparison_dir, original_filename))
        
        html_content += f"""
            <h4>Original Image</h4>
            <img class="original-img" src="{original_filename}" alt="Original Image">
            
            <h4>Model Results</h4>
            <div class="model-grid">
        """
        
        # Add results from each model
        for model in models:
            if i < len(model_results[model]):
                mask_path = model_results[model][i]['mask_path']
                result_path = model_results[model][i]['result_path']
                
                # Generate filenames for copied images
                mask_filename = f"{model}_mask_{i}.png"
                result_filename = f"{model}_result_{i}.png"
                
                # Copy images to comparison directory
                Image.open(mask_path).save(os.path.join(comparison_dir, mask_filename))
                Image.open(result_path).save(os.path.join(comparison_dir, result_filename))
                
                processing_time = model_results[model][i]['metadata'].get('processingTimeMs', 'N/A')
                wall_percent = model_results[model][i]['metadata'].get('wallPixelPercentage', 'N/A')
                
                html_content += f"""
                <div class="model-cell">
                    <h5>{model}</h5>
                    <p>Processing Time: {processing_time:.2f} ms</p>
                    <p>Wall Pixel %: {wall_percent:.2f}%</p>
                    
                    <h6>Mask</h6>
                    <img src="{mask_filename}" alt="{model} Mask">
                    
                    <h6>Result</h6>
                    <img src="{result_filename}" alt="{model} Result">
                </div>
                """
        
        html_content += """
            </div>
        </div>
        """
    
    html_content += """
    </body>
    </html>
    """
    
    # Write the HTML file
    report_path = os.path.join(comparison_dir, 'model_comparison.html')
    with open(report_path, 'w') as f:
        f.write(html_content)
    
    print(f"\nVisual comparison report generated at: {report_path}")

def main():
    parser = argparse.ArgumentParser(description='Compare DeepLabV3 Model Results')
    parser.add_argument('--dir', type=str, default='WallDetectionTests', help='Directory containing test results')
    parser.add_argument('--model', type=str, help='Filter by model name (e.g., "mobilenet")')
    parser.add_argument('--samples', type=int, default=5, help='Number of samples to include in visual comparison')
    args = parser.parse_args()
    
    model_results = find_model_results(args.dir, args.model)
    if model_results:
        if len(model_results) > 1:
            compare_performance(model_results)
            create_visual_comparison(model_results, sample_count=args.samples)
        else:
            print("Need at least two models to compare. Only found one model.")

if __name__ == "__main__":
    main() 