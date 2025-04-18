#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import glob
import argparse
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import MaxNLocator
from datetime import datetime

def find_performance_logs(base_dir="."):
    """Find performance log files in the specified directory."""
    log_files = glob.glob(os.path.join(base_dir, "PerfLog_*.csv"))
    
    if not log_files:
        log_files = glob.glob(os.path.join(base_dir, "*.csv"))
        log_files = [f for f in log_files if "PerfLog" in f or "PerformanceLog" in f]
    
    return log_files

def parse_log_file(log_file):
    """Parse a performance log file into a pandas DataFrame."""
    try:
        df = pd.read_csv(log_file)
        # Ensure all required columns are present
        required_columns = ["FPS", "TotalMemoryMB", "AllocatedMemoryMB", "CPUUsage", 
                           "ProcessingTimeMs", "ModelName", "ModelResolution"]
        
        missing_columns = [col for col in required_columns if col not in df.columns]
        if missing_columns:
            print(f"Warning: Log file {log_file} is missing required columns: {missing_columns}")
            return None
            
        # Extract model name and resolution from filename if not in data
        if "ModelName" not in df.columns or df["ModelName"].isnull().all():
            filename = os.path.basename(log_file)
            parts = filename.split("_")
            if len(parts) >= 3:
                df["ModelName"] = parts[1]
                
        if "ModelResolution" not in df.columns or df["ModelResolution"].isnull().all():
            filename = os.path.basename(log_file)
            parts = filename.split("_")
            if len(parts) >= 3:
                try:
                    df["ModelResolution"] = int(parts[2])
                except ValueError:
                    pass
        
        return df
        
    except Exception as e:
        print(f"Error parsing log file {log_file}: {e}")
        return None

def analyze_performance_log(df, log_file):
    """Analyze a performance log DataFrame and return summary statistics."""
    if df is None or len(df) == 0:
        return None
    
    # Basic statistics
    stats = {
        "file": os.path.basename(log_file),
        "model_name": df["ModelName"].iloc[0] if "ModelName" in df.columns else "Unknown",
        "model_resolution": df["ModelResolution"].iloc[0] if "ModelResolution" in df.columns else 0,
        "samples": len(df),
        "duration_sec": df["Timestamp"].max() - df["Timestamp"].min() if "Timestamp" in df.columns else 0,
    }
    
    # FPS statistics
    if "FPS" in df.columns:
        stats.update({
            "avg_fps": df["FPS"].mean(),
            "min_fps": df["FPS"].min(),
            "max_fps": df["FPS"].max(),
            "std_fps": df["FPS"].std()
        })
    
    # Memory statistics
    if "AllocatedMemoryMB" in df.columns:
        stats.update({
            "avg_memory_mb": df["AllocatedMemoryMB"].mean(),
            "peak_memory_mb": df["AllocatedMemoryMB"].max()
        })
    
    # CPU statistics
    if "CPUUsage" in df.columns:
        stats.update({
            "avg_cpu_usage": df["CPUUsage"].mean(),
            "peak_cpu_usage": df["CPUUsage"].max()
        })
    
    # Processing time statistics
    if "ProcessingTimeMs" in df.columns:
        stats.update({
            "avg_processing_ms": df["ProcessingTimeMs"].mean(),
            "min_processing_ms": df["ProcessingTimeMs"].min(),
            "max_processing_ms": df["ProcessingTimeMs"].max()
        })
    
    return stats

def plot_performance_metrics(log_dfs, output_dir="."):
    """Generate performance metric plots for the analyzed log files."""
    if not log_dfs or len(log_dfs) == 0:
        print("No valid log data to plot.")
        return
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Prepare data for comparison plots
    model_names = []
    model_resolutions = []
    avg_fps_values = []
    avg_memory_values = []
    avg_processing_values = []
    
    for df in log_dfs:
        if df is None or len(df) == 0:
            continue
            
        model_name = df["ModelName"].iloc[0] if "ModelName" in df.columns else "Unknown"
        model_resolution = df["ModelResolution"].iloc[0] if "ModelResolution" in df.columns else 0
        
        # Create a label with model name and resolution
        label = f"{model_name} ({model_resolution}x{model_resolution})"
        model_names.append(label)
        model_resolutions.append(model_resolution)
        
        # Collect metric values
        if "FPS" in df.columns:
            avg_fps_values.append(df["FPS"].mean())
        else:
            avg_fps_values.append(0)
            
        if "AllocatedMemoryMB" in df.columns:
            avg_memory_values.append(df["AllocatedMemoryMB"].mean())
        else:
            avg_memory_values.append(0)
            
        if "ProcessingTimeMs" in df.columns:
            avg_processing_values.append(df["ProcessingTimeMs"].mean())
        else:
            avg_processing_values.append(0)
    
    # Plot comparison bar charts
    if len(model_names) > 1:
        # FPS Comparison
        plt.figure(figsize=(10, 6))
        bars = plt.bar(model_names, avg_fps_values, color='blue', alpha=0.7)
        
        # Add value labels on top of bars
        for bar in bars:
            height = bar.get_height()
            plt.text(bar.get_x() + bar.get_width()/2., height + 0.5,
                    f'{height:.1f}', ha='center', va='bottom')
        
        plt.title('Average FPS Comparison')
        plt.xlabel('Model')
        plt.ylabel('Frames Per Second')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'fps_comparison.png'))
        plt.close()
        
        # Memory Usage Comparison
        plt.figure(figsize=(10, 6))
        bars = plt.bar(model_names, avg_memory_values, color='green', alpha=0.7)
        
        # Add value labels on top of bars
        for bar in bars:
            height = bar.get_height()
            plt.text(bar.get_x() + bar.get_width()/2., height + 0.5,
                    f'{height:.1f}', ha='center', va='bottom')
        
        plt.title('Average Memory Usage Comparison')
        plt.xlabel('Model')
        plt.ylabel('Memory Usage (MB)')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'memory_comparison.png'))
        plt.close()
        
        # Processing Time Comparison
        plt.figure(figsize=(10, 6))
        bars = plt.bar(model_names, avg_processing_values, color='red', alpha=0.7)
        
        # Add value labels on top of bars
        for bar in bars:
            height = bar.get_height()
            plt.text(bar.get_x() + bar.get_width()/2., height + 0.5,
                    f'{height:.1f}', ha='center', va='bottom')
        
        plt.title('Average Processing Time Comparison')
        plt.xlabel('Model')
        plt.ylabel('Processing Time (ms)')
        plt.xticks(rotation=45, ha='right')
        plt.tight_layout()
        plt.savefig(os.path.join(output_dir, 'processing_time_comparison.png'))
        plt.close()
        
        # Resolution vs Performance scatter plot
        if len(model_resolutions) > 1 and len(set(model_resolutions)) > 1:
            plt.figure(figsize=(10, 6))
            plt.scatter(model_resolutions, avg_fps_values, s=100, alpha=0.7, c='blue')
            
            # Add labels to points
            for i, model in enumerate(model_names):
                plt.annotate(model, (model_resolutions[i], avg_fps_values[i]), 
                             textcoords="offset points", xytext=(0,10), ha='center')
            
            # Add trend line
            if len(model_resolutions) > 2:
                z = np.polyfit(model_resolutions, avg_fps_values, 1)
                p = np.poly1d(z)
                x_trend = np.linspace(min(model_resolutions), max(model_resolutions), 100)
                plt.plot(x_trend, p(x_trend), "r--", alpha=0.7)
            
            plt.title('Resolution vs FPS Performance')
            plt.xlabel('Input Resolution (pixels)')
            plt.ylabel('Average FPS')
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, 'resolution_vs_fps.png'))
            plt.close()
    
    # Plot individual metrics for each log file
    for i, df in enumerate(log_dfs):
        if df is None or len(df) == 0:
            continue
            
        model_name = df["ModelName"].iloc[0] if "ModelName" in df.columns else "Unknown"
        model_resolution = df["ModelResolution"].iloc[0] if "ModelResolution" in df.columns else 0
        
        # Create a unique prefix for the output files
        file_prefix = f"{model_name}_{model_resolution}_"
        
        # FPS over time
        if "FPS" in df.columns and "Timestamp" in df.columns:
            plt.figure(figsize=(10, 6))
            plt.plot(df["Timestamp"], df["FPS"], 'b-', alpha=0.7)
            plt.axhline(y=df["FPS"].mean(), color='r', linestyle='--', alpha=0.7, 
                        label=f'Avg: {df["FPS"].mean():.1f}')
            plt.title(f'FPS over Time - {model_name} ({model_resolution}x{model_resolution})')
            plt.xlabel('Time (seconds)')
            plt.ylabel('Frames Per Second')
            plt.legend()
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f'{file_prefix}fps_time.png'))
            plt.close()
        
        # Memory usage over time
        if "AllocatedMemoryMB" in df.columns and "Timestamp" in df.columns:
            plt.figure(figsize=(10, 6))
            plt.plot(df["Timestamp"], df["AllocatedMemoryMB"], 'g-', alpha=0.7)
            plt.title(f'Memory Usage over Time - {model_name} ({model_resolution}x{model_resolution})')
            plt.xlabel('Time (seconds)')
            plt.ylabel('Memory Usage (MB)')
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f'{file_prefix}memory_time.png'))
            plt.close()
        
        # Processing time over time
        if "ProcessingTimeMs" in df.columns and "Timestamp" in df.columns:
            plt.figure(figsize=(10, 6))
            plt.plot(df["Timestamp"], df["ProcessingTimeMs"], 'r-', alpha=0.7)
            plt.axhline(y=df["ProcessingTimeMs"].mean(), color='b', linestyle='--', alpha=0.7,
                        label=f'Avg: {df["ProcessingTimeMs"].mean():.1f} ms')
            plt.title(f'Processing Time over Time - {model_name} ({model_resolution}x{model_resolution})')
            plt.xlabel('Time (seconds)')
            plt.ylabel('Processing Time (ms)')
            plt.legend()
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f'{file_prefix}processing_time.png'))
            plt.close()
        
        # FPS Distribution histogram
        if "FPS" in df.columns:
            plt.figure(figsize=(10, 6))
            plt.hist(df["FPS"], bins=20, alpha=0.7, color='blue')
            plt.axvline(x=df["FPS"].mean(), color='r', linestyle='--', alpha=0.7,
                       label=f'Mean: {df["FPS"].mean():.1f}')
            plt.title(f'FPS Distribution - {model_name} ({model_resolution}x{model_resolution})')
            plt.xlabel('Frames Per Second')
            plt.ylabel('Frequency')
            plt.legend()
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f'{file_prefix}fps_histogram.png'))
            plt.close()
        
        # Processing Time Distribution histogram
        if "ProcessingTimeMs" in df.columns:
            plt.figure(figsize=(10, 6))
            plt.hist(df["ProcessingTimeMs"], bins=20, alpha=0.7, color='red')
            plt.axvline(x=df["ProcessingTimeMs"].mean(), color='b', linestyle='--', alpha=0.7,
                       label=f'Mean: {df["ProcessingTimeMs"].mean():.1f} ms')
            plt.title(f'Processing Time Distribution - {model_name} ({model_resolution}x{model_resolution})')
            plt.xlabel('Processing Time (ms)')
            plt.ylabel('Frequency')
            plt.legend()
            plt.grid(True, alpha=0.3)
            plt.tight_layout()
            plt.savefig(os.path.join(output_dir, f'{file_prefix}processing_time_histogram.png'))
            plt.close()

def generate_report(stats_list, output_dir="."):
    """Generate an HTML performance comparison report."""
    if not stats_list:
        print("No performance data to include in report.")
        return None
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Define report file path
    report_path = os.path.join(output_dir, "performance_report.html")
    
    # Generate HTML content
    html_content = f"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>DeepLabV3 Performance Report</title>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; margin: 20px; }}
            h1, h2, h3 {{ color: #333; }}
            table {{ border-collapse: collapse; width: 100%; margin-bottom: 20px; }}
            th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
            th {{ background-color: #f2f2f2; }}
            tr:nth-child(even) {{ background-color: #f9f9f9; }}
            .chart-container {{ margin: 20px 0; display: flex; flex-wrap: wrap; gap: 20px; }}
            .chart {{ box-shadow: 0 0 5px rgba(0,0,0,0.1); padding: 10px; margin-bottom: 20px; }}
            .performance-card {{ border: 1px solid #ddd; border-radius: 4px; padding: 15px; margin-bottom: 20px; box-shadow: 0 0 5px rgba(0,0,0,0.1); }}
            .metrics {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 10px; margin-top: 10px; }}
            .metric {{ background-color: #f8f8f8; padding: 10px; border-radius: 4px; }}
            .metric-value {{ font-size: 24px; font-weight: bold; margin-top: 5px; }}
            .good {{ color: green; }}
            .moderate {{ color: orange; }}
            .poor {{ color: red; }}
        </style>
    </head>
    <body>
        <h1>DeepLabV3 Performance Analysis Report</h1>
        <p>Report generated on {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        
        <h2>Performance Comparison</h2>
        <table>
            <tr>
                <th>Model</th>
                <th>Resolution</th>
                <th>Avg FPS</th>
                <th>Avg Processing Time (ms)</th>
                <th>Avg Memory (MB)</th>
                <th>Peak Memory (MB)</th>
                <th>Samples</th>
            </tr>
    """
    
    # Add rows for each model
    for stats in stats_list:
        model_name = stats.get("model_name", "Unknown")
        resolution = stats.get("model_resolution", 0)
        avg_fps = stats.get("avg_fps", 0)
        avg_processing = stats.get("avg_processing_ms", 0)
        avg_memory = stats.get("avg_memory_mb", 0)
        peak_memory = stats.get("peak_memory_mb", 0)
        samples = stats.get("samples", 0)
        
        # Determine color classes based on performance
        fps_class = "good" if avg_fps > 25 else ("moderate" if avg_fps > 15 else "poor")
        processing_class = "good" if avg_processing < 40 else ("moderate" if avg_processing < 70 else "poor")
        
        html_content += f"""
            <tr>
                <td>{model_name}</td>
                <td>{resolution}x{resolution}</td>
                <td class="{fps_class}">{avg_fps:.1f}</td>
                <td class="{processing_class}">{avg_processing:.1f}</td>
                <td>{avg_memory:.1f}</td>
                <td>{peak_memory:.1f}</td>
                <td>{samples}</td>
            </tr>
        """
    
    html_content += """
        </table>
        
        <h2>Performance Charts</h2>
        <div class="chart-container">
            <div class="chart">
                <h3>FPS Comparison</h3>
                <img src="fps_comparison.png" alt="FPS Comparison" style="max-width:100%;">
            </div>
            
            <div class="chart">
                <h3>Processing Time Comparison</h3>
                <img src="processing_time_comparison.png" alt="Processing Time Comparison" style="max-width:100%;">
            </div>
            
            <div class="chart">
                <h3>Memory Usage Comparison</h3>
                <img src="memory_comparison.png" alt="Memory Usage Comparison" style="max-width:100%;">
            </div>
            
            <div class="chart">
                <h3>Resolution vs Performance</h3>
                <img src="resolution_vs_fps.png" alt="Resolution vs Performance" style="max-width:100%;">
            </div>
        </div>
        
        <h2>Individual Model Analysis</h2>
    """
    
    # Add individual model details
    for stats in stats_list:
        model_name = stats.get("model_name", "Unknown")
        resolution = stats.get("model_resolution", 0)
        file_prefix = f"{model_name}_{resolution}_"
        
        avg_fps = stats.get("avg_fps", 0)
        min_fps = stats.get("min_fps", 0)
        max_fps = stats.get("max_fps", 0)
        
        avg_processing = stats.get("avg_processing_ms", 0)
        min_processing = stats.get("min_processing_ms", 0)
        max_processing = stats.get("max_processing_ms", 0)
        
        avg_memory = stats.get("avg_memory_mb", 0)
        peak_memory = stats.get("peak_memory_mb", 0)
        
        avg_cpu = stats.get("avg_cpu_usage", 0)
        peak_cpu = stats.get("peak_cpu_usage", 0)
        
        html_content += f"""
        <div class="performance-card">
            <h3>{model_name} ({resolution}x{resolution})</h3>
            
            <div class="metrics">
                <div class="metric">
                    <h4>Average FPS</h4>
                    <div class="metric-value">{avg_fps:.1f}</div>
                    <div>Min: {min_fps:.1f} | Max: {max_fps:.1f}</div>
                </div>
                
                <div class="metric">
                    <h4>Processing Time</h4>
                    <div class="metric-value">{avg_processing:.1f} ms</div>
                    <div>Min: {min_processing:.1f} | Max: {max_processing:.1f}</div>
                </div>
                
                <div class="metric">
                    <h4>Memory Usage</h4>
                    <div class="metric-value">{avg_memory:.1f} MB</div>
                    <div>Peak: {peak_memory:.1f} MB</div>
                </div>
                
                <div class="metric">
                    <h4>CPU Usage</h4>
                    <div class="metric-value">{avg_cpu:.1f}%</div>
                    <div>Peak: {peak_cpu:.1f}%</div>
                </div>
            </div>
            
            <h4>FPS Distribution</h4>
            <img src="{file_prefix}fps_histogram.png" alt="FPS Histogram" style="max-width:100%;">
            
            <h4>Processing Time Distribution</h4>
            <img src="{file_prefix}processing_time_histogram.png" alt="Processing Time Histogram" style="max-width:100%;">
            
            <h4>Performance over Time</h4>
            <img src="{file_prefix}fps_time.png" alt="FPS over Time" style="max-width:100%;">
            <img src="{file_prefix}processing_time.png" alt="Processing Time over Time" style="max-width:100%;">
            <img src="{file_prefix}memory_time.png" alt="Memory Usage over Time" style="max-width:100%;">
        </div>
        """
    
    html_content += """
        <h2>Recommendations</h2>
        <div class="performance-card">
            <h3>Device-Specific Recommendations</h3>
            
            <h4>Low-End Devices</h4>
            <ul>
                <li>Use MobileNet with 224x224 or 320x320 resolution</li>
                <li>Increase processing interval to maintain reasonable FPS</li>
                <li>Reduce post-processing complexity</li>
            </ul>
            
            <h4>Mid-Range Devices</h4>
            <ul>
                <li>Use MobileNet with 320x320 or 512x512 resolution</li>
                <li>Balance processing interval and quality</li>
                <li>Apply moderate post-processing</li>
            </ul>
            
            <h4>High-End Devices</h4>
            <ul>
                <li>Use ResNet with 320x320 or 512x512 for best quality</li>
                <li>Apply full post-processing for optimal results</li>
                <li>Lower processing interval for smoother experience</li>
            </ul>
        </div>
    </body>
    </html>
    """
    
    # Write HTML report to file
    with open(report_path, "w") as f:
        f.write(html_content)
    
    print(f"Performance report generated at: {report_path}")
    return report_path

def main():
    parser = argparse.ArgumentParser(description='Analyze Performance Log Files')
    parser.add_argument('--dir', type=str, default='.', help='Directory containing performance log files')
    parser.add_argument('--output', type=str, default='performance_analysis', help='Output directory for analysis results')
    args = parser.parse_args()
    
    # Find log files
    log_files = find_performance_logs(args.dir)
    if not log_files:
        print(f"No performance log files found in {args.dir}")
        return
    
    print(f"Found {len(log_files)} performance log files")
    
    # Parse log files
    log_dfs = []
    stats_list = []
    
    for log_file in log_files:
        print(f"Analyzing {log_file}...")
        df = parse_log_file(log_file)
        if df is not None:
            log_dfs.append(df)
            stats = analyze_performance_log(df, log_file)
            if stats:
                stats_list.append(stats)
                
                # Print summary statistics
                print(f"  Model: {stats['model_name']} ({stats['model_resolution']}x{stats['model_resolution']})")
                print(f"  FPS: {stats['avg_fps']:.1f} (min: {stats['min_fps']:.1f}, max: {stats['max_fps']:.1f})")
                print(f"  Processing Time: {stats['avg_processing_ms']:.1f} ms")
                print(f"  Memory: {stats['avg_memory_mb']:.1f} MB (peak: {stats['peak_memory_mb']:.1f} MB)")
                print(f"  Samples: {stats['samples']}")
                print()
    
    if not log_dfs:
        print("No valid performance log data found.")
        return
    
    # Create output directory
    os.makedirs(args.output, exist_ok=True)
    
    # Generate plots
    print("Generating performance visualizations...")
    plot_performance_metrics(log_dfs, args.output)
    
    # Generate HTML report
    print("Generating performance report...")
    report_path = generate_report(stats_list, args.output)
    
    if report_path:
        print(f"\nAnalysis completed. Open {report_path} to view the full report.")

if __name__ == "__main__":
    main() 