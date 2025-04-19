#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import argparse
from huggingface_hub import hf_hub_download
import shutil
import time
from datetime import datetime

def download_deeplabv3_model(output_dir, model_size=320, model_type='mobilenetv3', optimize=True):
    """
    Download DeepLabV3 model from Hugging Face Hub in ONNX format
    
    Args:
        output_dir: Directory to save the model
        model_size: Input resolution for the model (224, 320, 512, or 768)
        model_type: Type of backbone ('mobilenetv3' or 'resnet50')
        optimize: Whether to use optimized model if available
        
    Returns:
        Path to the downloaded model
    """
    print(f"\n{'='*50}")
    print(f"Downloading DeepLabV3 {model_type} model with input size {model_size}x{model_size}...")
    print(f"{'='*50}\n")
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Define model name and repository on Hugging Face
    repo_id = "remalux/deeplabv3-onnx-models"
    
    if model_type == 'mobilenetv3':
        model_name = f"deeplabv3_mobilenetv3_{model_size}x{model_size}"
    else:
        model_name = f"deeplabv3_resnet50_{model_size}x{model_size}"
    
    if optimize:
        model_name += "_optimized"
    
    model_file = f"{model_name}.onnx"
    
    try:
        print(f"Downloading model from Hugging Face Hub: {repo_id}/{model_file}")
        
        # Download model from Hugging Face Hub
        downloaded_path = hf_hub_download(
            repo_id=repo_id,
            filename=model_file,
            local_dir=output_dir,
            local_dir_use_symlinks=False
        )
        
        # Add timestamp to filename
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        new_filename = f"{model_name}_{timestamp}.onnx"
        new_path = os.path.join(output_dir, new_filename)
        
        shutil.copy(downloaded_path, new_path)
        print(f"Model saved to: {new_path}")
        
        # Get file size
        file_size_mb = os.path.getsize(new_path) / (1024 * 1024)
        print(f"Model file size: {file_size_mb:.2f} MB")
        
        return new_path
    
    except Exception as e:
        print(f"Error downloading model: {e}")
        print("Please check your internet connection and try again.")
        return None

def main():
    parser = argparse.ArgumentParser(description='Download DeepLabV3 model in ONNX format for Unity Barracuda')
    parser.add_argument('--model_type', type=str, default='mobilenetv3', 
                        choices=['mobilenetv3', 'resnet50'],
                        help='Type of backbone to use (mobilenetv3 or resnet50)')
    parser.add_argument('--output_dir', type=str, default='models', 
                        help='Directory to save the exported model')
    parser.add_argument('--model_size', type=int, default=320, 
                        choices=[224, 320, 512, 768],
                        help='Input size for the model (square)')
    parser.add_argument('--no-optimize', action='store_true', 
                        help='Use non-optimized model')
    args = parser.parse_args()
    
    start_time = time.time()
    
    model_path = download_deeplabv3_model(
        output_dir=args.output_dir,
        model_size=args.model_size,
        model_type=args.model_type,
        optimize=not args.no_optimize
    )
    
    end_time = time.time()
    
    if model_path:
        print("\n" + "="*50)
        print("Download completed successfully.")
        print("="*50)
        print(f"Model: DeepLabV3 with {args.model_type} backbone")
        print(f"Input size: {args.model_size}x{args.model_size}")
        print(f"Optimized: {not args.no_optimize}")
        print(f"Output file: {model_path}")
        print(f"Download time: {end_time - start_time:.2f} seconds")
        print("\nTo use this model in Unity:")
        print("1. Import the ONNX file into your Unity project")
        print("2. Use Unity Barracuda to load and run the model")
        print("3. Connect the model to the DeepLabDecoder component")
        print("4. Set the correct resolution preset to match the model's input size")
    else:
        print("\nDownload failed. Please check the error messages above.")
        print("You can also try to download the model manually from: https://huggingface.co/remalux/deeplabv3-onnx-models")

if __name__ == "__main__":
    main() 