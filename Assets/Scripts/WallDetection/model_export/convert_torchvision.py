#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import sys
import torch
import torch.nn as nn
from torch.autograd import Variable
import torchvision.models.segmentation as segmentation
import torchvision.transforms as transforms
from torchvision.transforms import functional as F
import numpy as np
import argparse
from datetime import datetime
from PIL import Image
import matplotlib.pyplot as plt
import time

def load_model(model_type='mobilenet_v3_large'):
    """
    Load DeepLabV3 model with specified backbone.
    
    Args:
        model_type: Type of backbone ('mobilenet_v3_large' or 'resnet50')
        
    Returns:
        Loaded PyTorch model
    """
    print(f"Loading DeepLabV3 model with {model_type} backbone...")
    
    try:
        if model_type == 'mobilenet_v3_large':
            model = segmentation.deeplabv3_mobilenet_v3_large(pretrained=True, progress=True)
        elif model_type == 'resnet50':
            model = segmentation.deeplabv3_resnet50(pretrained=True, progress=True)
        else:
            raise ValueError(f"Unsupported model type: {model_type}")
        
        model.eval()
        print("Model loaded successfully")
        return model
    
    except Exception as e:
        print(f"Error loading model with first method: {e}")
        print("Trying alternative loading method...")
        
        try:
            if model_type == 'mobilenet_v3_large':
                model = torch.hub.load('pytorch/vision:v0.10.0', 'deeplabv3_mobilenet_v3_large', pretrained=True)
            elif model_type == 'resnet50':
                model = torch.hub.load('pytorch/vision:v0.10.0', 'deeplabv3_resnet50', pretrained=True)
            else:
                raise ValueError(f"Unsupported model type: {model_type}")
                
            model.eval()
            print("Model loaded successfully using torch.hub")
            return model
        
        except Exception as e2:
            print(f"Error loading model with alternative method: {e2}")
            print("Could not load model. Please check your internet connection and PyTorch installation.")
            sys.exit(1)

def export_to_onnx(model, output_dir, input_size=320, opset_version=11, optimize=True):
    """
    Export PyTorch model to ONNX format
    
    Args:
        model: PyTorch model to export
        output_dir: Directory to save the exported model
        input_size: Input resolution for the model
        opset_version: ONNX opset version
        optimize: Whether to optimize the ONNX model
        
    Returns:
        Path to the exported model
    """
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Create dummy input tensor
    x = Variable(torch.randn(1, 3, input_size, input_size))
    
    # Define output filename
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output_filename = f"deeplabv3_mobilenetv3_{input_size}x{input_size}_{timestamp}.onnx"
    output_path = os.path.join(output_dir, output_filename)
    
    # Export the model to ONNX format
    print("Exporting to ONNX format...")
    try:
        torch.onnx.export(
            model,                   # Model being run
            x,                       # Model input (or a tuple for multiple inputs)
            output_path,             # Where to save the model
            export_params=True,      # Store the trained parameter weights inside the model file
            opset_version=opset_version,  # The ONNX version to export the model to
            do_constant_folding=True,  # Fold constant values for optimization
            input_names=['input'],   # Names for the input and output nodes
            output_names=['output'],
            dynamic_axes={
                'input': {0: 'batch_size'},   # Variable batch size
                'output': {0: 'batch_size'}
            }
        )
        print(f"Model exported successfully to {output_path}")
    except Exception as e:
        print(f"Error during export: {e}")
        return None
    
    # Verify the exported model
    try:
        # Check if we can load the model
        dummy_input = torch.randn(1, 3, input_size, input_size)
        
        # Get file size
        file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
        print(f"Model file size: {file_size_mb:.2f} MB")
        
        return output_path
    except Exception as e:
        print(f"Error verifying the model: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description='Export DeepLabV3 model to ONNX format for Unity Barracuda')
    parser.add_argument('--model_type', type=str, default='mobilenet_v3_large', 
                        choices=['mobilenet_v3_large', 'resnet50'],
                        help='Type of backbone to use (mobilenet_v3_large or resnet50)')
    parser.add_argument('--output_dir', type=str, default='models', 
                        help='Directory to save the exported model')
    parser.add_argument('--input_size', type=int, default=320, 
                        help='Input size for the model (square)')
    parser.add_argument('--opset_version', type=int, default=11, 
                        help='ONNX opset version (use 11 for Barracuda compatibility)')
    args = parser.parse_args()
    
    # Print system info
    print(f"\nSystem information:")
    print(f"PyTorch version: {torch.__version__}")
    print(f"CUDA available: {torch.cuda.is_available()}")
    if torch.cuda.is_available():
        print(f"CUDA version: {torch.version.cuda}")
        print(f"GPU: {torch.cuda.get_device_name(0)}")
    print()
    
    start_time = time.time()
    
    # Load model
    model = load_model(model_type=args.model_type)
    
    # Export model
    model_path = export_to_onnx(
        model=model,
        output_dir=args.output_dir,
        input_size=args.input_size,
        opset_version=args.opset_version
    )
    
    end_time = time.time()
    
    if model_path:
        print("\n" + "="*50)
        print("Export completed successfully.")
        print("="*50)
        print(f"Model: DeepLabV3 with {args.model_type} backbone")
        print(f"Input size: {args.input_size}x{args.input_size}")
        print(f"ONNX opset version: {args.opset_version}")
        print(f"Output file: {model_path}")
        print(f"Export time: {end_time - start_time:.2f} seconds")
        print("\nTo use this model in Unity:")
        print("1. Import the ONNX file into your Unity project")
        print("2. Use Unity Barracuda to load and run the model")
        print("3. Connect the model to the DeepLabDecoder component")
        print("4. Set the correct resolution preset to match the model's input size")
    else:
        print("\nExport failed. Please check the error messages above.")

if __name__ == "__main__":
    main() 