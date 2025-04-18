#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import torch
import torch.nn as nn
from torch.autograd import Variable
import torchvision.models.segmentation as segmentation
import onnx
import argparse
from datetime import datetime

def export_deeplabv3_mobilenet(output_dir='models', input_size=320, opset_version=11):
    """
    Export DeepLabV3 MobileNet model to ONNX format for use with Unity Barracuda.
    
    Args:
        output_dir: Directory to save the exported model
        input_size: Input resolution (square) for the model
        opset_version: ONNX opset version (11 is compatible with Barracuda)
        
    Returns:
        Path to the exported model
    """
    print(f"Exporting DeepLabV3 MobileNet model with input size {input_size}x{input_size}...")
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Load DeepLabV3 model with MobileNetV3-Large backbone
    try:
        model = segmentation.deeplabv3_mobilenet_v3_large(pretrained=True, progress=True)
        model.eval()
    except Exception as e:
        print(f"Error loading the model: {e}")
        print("Trying alternative loading method...")
        model = torch.hub.load('pytorch/vision:v0.10.0', 'deeplabv3_mobilenet_v3_large', pretrained=True)
        model.eval()
    
    print("Model loaded successfully")
    
    # Create dummy input tensor
    x = Variable(torch.randn(1, 3, input_size, input_size))
    
    # Define output filename
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output_filename = f"deeplabv3_mobilenet_v3_{input_size}x{input_size}_{timestamp}.onnx"
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
        onnx_model = onnx.load(output_path)
        onnx.checker.check_model(onnx_model)
        print("ONNX model checked - the exported model is valid")
    except Exception as e:
        print(f"Error verifying the model: {e}")
        return None
    
    return output_path

def main():
    parser = argparse.ArgumentParser(description='Export DeepLabV3 MobileNet model to ONNX format')
    parser.add_argument('--output_dir', type=str, default='models', help='Directory to save the exported model')
    parser.add_argument('--input_size', type=int, default=320, help='Input size for the model (square)')
    parser.add_argument('--opset_version', type=int, default=11, help='ONNX opset version (use 11 for Barracuda compatibility)')
    args = parser.parse_args()
    
    model_path = export_deeplabv3_mobilenet(
        output_dir=args.output_dir,
        input_size=args.input_size,
        opset_version=args.opset_version
    )
    
    if model_path:
        print("\nExport completed successfully.")
        print(f"Model: DeepLabV3 with MobileNetV3-Large backbone")
        print(f"Input size: {args.input_size}x{args.input_size}")
        print(f"ONNX opset version: {args.opset_version}")
        print(f"Output file: {model_path}")
        print("\nTo use this model in Unity:")
        print("1. Import the ONNX file into your Unity project")
        print("2. Use Unity Barracuda to load and run the model")
        print("3. Connect the model to the DeepLabDecoder component")
    else:
        print("\nExport failed. Please check the error messages above.")

if __name__ == "__main__":
    main() 