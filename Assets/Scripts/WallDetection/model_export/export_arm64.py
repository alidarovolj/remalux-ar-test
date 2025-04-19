#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
DeepLabV3 model exporter optimized for ARM64 architecture (Apple Silicon)
Exports pre-trained DeepLabV3 models to ONNX format with optimizations for M1/M2/M3 processors
"""

import os
import sys
import argparse
import warnings
import numpy as np
import torch
import torch.nn as nn
from torchvision.models.segmentation import deeplabv3_mobilenet_v3_large, deeplabv3_resnet50
from torchvision.models.segmentation.deeplabv3 import DeepLabHead
import onnx
import onnxruntime as ort
from PIL import Image
import time
import matplotlib.pyplot as plt
import torchvision

# Check for MPS availability (Metal Performance Shaders for Apple Silicon)
HAS_MPS = torch.backends.mps.is_available()

def get_arguments():
    parser = argparse.ArgumentParser(description="Export DeepLabV3 models to ONNX format with ARM64 optimizations")
    parser.add_argument("--model_type", type=str, default="mobilenet", choices=["mobilenet", "resnet50"],
                      help="Model backbone to use (mobilenet or resnet50)")
    parser.add_argument("--size", type=int, default=320,
                      help="Input resolution (square)")
    parser.add_argument("--batch_size", type=int, default=1,
                      help="Batch size for export")
    parser.add_argument("--output_dir", type=str, default="./exported_models_arm64",
                      help="Directory to save the exported model")
    parser.add_argument("--quantize", action="store_true",
                      help="Quantize the model to reduce size and improve performance on ARM64")
    parser.add_argument("--test", action="store_true",
                      help="Test exported model with sample input")
    parser.add_argument("--test_image", type=str, default=None,
                      help="Path to test image")
    return parser.parse_args()

def get_model(model_type, num_classes=21):
    """Create a DeepLabV3 model with the specified backbone"""
    print(f"Creating DeepLabV3 model with {model_type} backbone...")
    
    if model_type.lower() == 'mobilenet':
        model = deeplabv3_mobilenet_v3_large(pretrained=True, progress=True, num_classes=num_classes)
    elif model_type.lower() == 'resnet50':
        model = deeplabv3_resnet50(pretrained=True, progress=True, num_classes=num_classes)
    else:
        raise ValueError(f"Unsupported model type: {model_type}. Use 'mobilenet' or 'resnet50'")
    
    return model

def prepare_dummy_input(size, batch_size=1):
    # Create a dummy input tensor with the specified size
    return torch.randn(batch_size, 3, size, size)

def export_model(model, size, output_path, quantize=False):
    """Export model to ONNX format"""
    if not os.path.exists(os.path.dirname(output_path)):
        os.makedirs(os.path.dirname(output_path))
    
    # Set model to evaluation mode
    model.eval()
    
    # Move model to MPS device if available
    device = torch.device("mps") if HAS_MPS else torch.device("cpu")
    model = model.to(device)
    print(f"Using device: {device}")
    
    # Create dummy input
    dummy_input = torch.randn(1, 3, size, size).to(device)
    
    # Get input and output names
    input_names = ["input"]
    output_names = ["output"]
    
    # Define dynamic axes
    dynamic_axes = {
        "input": {0: "batch_size", 2: "height", 3: "width"},
        "output": {0: "batch_size", 2: "height", 3: "width"},
    }
    
    # Export model to ONNX
    print(f"Exporting model to {output_path}...")
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        export_params=True,
        opset_version=12,
        do_constant_folding=True,
        input_names=input_names,
        output_names=output_names,
        dynamic_axes=dynamic_axes,
        verbose=False
    )
    
    # Optimize and quantize model if requested
    if quantize:
        try:
            from onnxsim import simplify
            
            print("Optimizing and quantizing model...")
            
            # Load ONNX model
            onnx_model = onnx.load(output_path)
            
            # Optimize model with ONNX Simplifier
            optimized_model, check = simplify(onnx_model)
            if not check:
                warnings.warn("Simplified ONNX model could not be validated!")
            
            # Save optimized model
            onnx.save(optimized_model, output_path)
            print("Model optimized and saved successfully")
            
        except ImportError:
            warnings.warn("Quantization requires onnx-simplifier. Install with 'pip install onnx-simplifier'")

def test_model(model_path, test_image_path=None, input_size=320):
    """Test exported ONNX model"""
    try:
        print(f"Testing exported model: {model_path}")
        
        # Create ONNX Runtime session
        session_options = ort.SessionOptions()
        session_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
        session = ort.InferenceSession(model_path, session_options)
        
        # Prepare input data
        if test_image_path and os.path.exists(test_image_path):
            print(f"Using test image: {test_image_path}")
            image = Image.open(test_image_path).convert("RGB")
            image = image.resize((input_size, input_size))
        else:
            print("Using random test image")
            image = Image.fromarray(np.random.randint(0, 255, (input_size, input_size, 3), dtype=np.uint8))
        
        # Preprocess image
        preprocess = torchvision.transforms.Compose([
            torchvision.transforms.ToTensor(),
            torchvision.transforms.Normalize(
                mean=[0.485, 0.456, 0.406], 
                std=[0.229, 0.224, 0.225]
            ),
        ])
        input_tensor = preprocess(image).unsqueeze(0).numpy()
        
        # Run inference
        input_name = session.get_inputs()[0].name
        output_name = session.get_outputs()[0].name
        
        print("Running inference...")
        start_time = torch.cuda.Event(enable_timing=True) if torch.cuda.is_available() else None
        end_time = torch.cuda.Event(enable_timing=True) if torch.cuda.is_available() else None
        
        if start_time:
            start_time.record()
            
        outputs = session.run([output_name], {input_name: input_tensor})
        
        if end_time:
            end_time.record()
            torch.cuda.synchronize()
            inference_time = start_time.elapsed_time(end_time)
            print(f"Inference time: {inference_time:.2f} ms")
        
        print("Inference completed successfully!")
        
        # Return output for potential visualization
        return outputs[0]
        
    except ImportError:
        warnings.warn("Testing requires onnxruntime. Install with 'pip install onnxruntime'")
        return None

def main():
    args = get_arguments()
    
    # Check if running on Apple Silicon
    if sys.platform == "darwin" and os.uname().machine != "arm64":
        print("WARNING: This script is optimized for ARM64 (Apple Silicon). You are running on a different architecture.")
        response = input("Continue anyway? (y/n): ")
        if response.lower() != 'y':
            sys.exit(0)
    
    # Enable MPS acceleration if available
    if torch.backends.mps.is_available():
        device = torch.device("mps")
        print("Using MPS acceleration for Apple Silicon")
    else:
        device = torch.device("cpu")
        print("MPS acceleration not available, using CPU")
    
    # Create and export the model
    model = get_model(args.model_type)
    model.to(device)
    model.eval()
    
    # Create dummy input
    dummy_input = prepare_dummy_input(args.size, args.batch_size).to(device)
    
    # Export to ONNX
    output_filename = f"deeplabv3_{args.model_type}_{args.size}x{args.size}_arm64.onnx"
    output_path = os.path.join(args.output_dir, output_filename)
    
    print(f"Exporting {args.model_type} model with input size {args.size}x{args.size}...")
    export_model(model, args.size, output_path, args.quantize)
    print(f"Model exported to {output_path}")
    
    # Test the exported model if requested
    if args.test:
        test_model(output_path, args.test_image, args.size)

if __name__ == "__main__":
    main() 