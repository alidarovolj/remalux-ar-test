#!/usr/bin/env python3
# convert_arm64.py - Convert existing ONNX models to be compatible with ARM64 (Apple Silicon)
# This script optimizes existing models for ARM64 architecture and can apply quantization

import os
import sys
import argparse
import numpy as np
import onnx
from onnx import shape_inference
import onnxruntime as ort
from PIL import Image
import glob
import time

def get_arguments():
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(description="Convert ONNX models for ARM64 compatibility")
    parser.add_argument('--input_dir', type=str, default='output',
                        help='Directory containing input ONNX models')
    parser.add_argument('--output_dir', type=str, default='output_arm64',
                        help='Directory to save converted models')
    parser.add_argument('--quantize', action='store_true',
                        help='Apply quantization to the model')
    parser.add_argument('--test', action='store_true',
                        help='Test the converted model with a sample input')
    parser.add_argument('--test_image', type=str, default=None,
                        help='Path to test image (optional)')
    return parser.parse_args()

def optimize_model_for_arm64(model_path, output_path, quantize=False):
    """Optimize the ONNX model for ARM64 architecture."""
    print(f"Loading model: {model_path}")
    model = onnx.load(model_path)
    
    # Run shape inference
    print("Running shape inference...")
    model = shape_inference.infer_shapes(model)
    
    # Optimize the model
    print("Optimizing model...")
    from onnxruntime.transformers import optimizer
    optimized_model = optimizer.optimize_model(
        model_path,
        model_type='bert',  # Using bert as a generic option
        opt_level=99,
        use_gpu=False,
        only_onnxruntime=True
    )
    
    # Save the optimized model
    print(f"Saving optimized model to {output_path}")
    optimized_model.save_model_to_file(output_path)
    
    # Apply quantization if requested
    if quantize:
        apply_quantization(output_path, output_path)
    
    return output_path

def apply_quantization(input_model_path, output_model_path):
    """Apply quantization to reduce model size."""
    from onnxruntime.quantization import quantize_dynamic, QuantType
    
    print(f"Applying quantization to model: {input_model_path}")
    quantize_dynamic(
        input_model_path,
        output_model_path,
        weight_type=QuantType.QUInt8
    )
    print(f"Quantized model saved to: {output_model_path}")

def test_model(model_path, test_image_path=None):
    """Test the converted model with a sample input."""
    print(f"Testing model: {model_path}")
    
    # Load the model
    session_options = ort.SessionOptions()
    session_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
    providers = ['CPUExecutionProvider']
    
    # Check if MPS provider is available for Apple Silicon
    if 'MacExecutionProvider' in ort.get_available_providers():
        providers = ['MacExecutionProvider'] + providers
    
    session = ort.InferenceSession(model_path, sess_options=session_options, providers=providers)
    
    # Get model input details
    input_name = session.get_inputs()[0].name
    input_shape = session.get_inputs()[0].shape
    
    # Prepare input data
    if test_image_path and os.path.exists(test_image_path):
        # Load and preprocess an actual image
        img = Image.open(test_image_path).convert('RGB')
        img = img.resize((input_shape[2], input_shape[3]))
        img_data = np.array(img).transpose(2, 0, 1).astype(np.float32)
        img_data = np.expand_dims(img_data, axis=0)
        input_data = img_data / 255.0  # Normalize to [0,1]
    else:
        # Create a dummy input tensor
        batch_size = input_shape[0] if input_shape[0] > 0 else 1
        channels = input_shape[1]
        height = input_shape[2]
        width = input_shape[3]
        input_data = np.random.rand(batch_size, channels, height, width).astype(np.float32)
    
    # Run inference
    print(f"Running inference with input shape: {input_data.shape}")
    try:
        start_time = time.time()
        outputs = session.run(None, {input_name: input_data})
        end_time = time.time()
        
        print(f"Inference successful! Execution time: {(end_time - start_time)*1000:.2f} ms")
        
        # Print output information
        for i, output in enumerate(session.get_outputs()):
            print(f"Output {i}: name={output.name}, shape={outputs[i].shape}")
        
        return True
    except Exception as e:
        print(f"Inference failed: {str(e)}")
        return False

def main():
    """Main function to convert and optimize models."""
    args = get_arguments()
    
    # Check if we're on Apple Silicon
    is_arm64 = sys.platform == 'darwin' and os.uname().machine == 'arm64'
    if is_arm64:
        print("Running on Apple Silicon (ARM64)")
    else:
        print(f"WARNING: Not running on ARM64. Current architecture: {os.uname().machine}")
    
    # Create output directory if it doesn't exist
    os.makedirs(args.output_dir, exist_ok=True)
    
    # Find all ONNX models in the input directory
    model_files = glob.glob(os.path.join(args.input_dir, "*.onnx"))
    if not model_files:
        print(f"No ONNX models found in {args.input_dir}")
        return
    
    print(f"Found {len(model_files)} ONNX models to convert")
    
    # Process each model
    for model_path in model_files:
        model_name = os.path.basename(model_path)
        output_path = os.path.join(args.output_dir, model_name)
        
        # Optimize model for ARM64
        output_path = optimize_model_for_arm64(model_path, output_path, args.quantize)
        
        # Test the converted model if requested
        if args.test:
            test_model(output_path, args.test_image)
    
    print("Conversion complete!")

if __name__ == "__main__":
    start_time = time.time()
    main()
    print(f"Total execution time: {time.time() - start_time:.2f} seconds") 