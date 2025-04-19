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

# Create a wrapper class to make DeepLabV3 traceable
class DeepLabV3Wrapper(nn.Module):
    def __init__(self, model):
        super(DeepLabV3Wrapper, self).__init__()
        self.model = model
        
    def forward(self, x):
        # Extract only the 'out' key from the output dictionary
        return self.model(x)['out']

def preprocess_image(image_path, input_size):
    """
    Preprocess an image for model input.
    
    Args:
        image_path: Path to the test image
        input_size: Input resolution for the model
        
    Returns:
        Preprocessed tensor
    """
    if not os.path.exists(image_path):
        print(f"Test image not found: {image_path}")
        return None
    
    try:
        image = Image.open(image_path).convert('RGB')
        preprocess = transforms.Compose([
            transforms.Resize((input_size, input_size)),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])
        input_tensor = preprocess(image)
        input_batch = input_tensor.unsqueeze(0)
        return input_batch
    except Exception as e:
        print(f"Error preprocessing image: {e}")
        return None

def test_model(model, image_path, input_size, save_path=None):
    """
    Test the model with a sample image and visualize results.
    
    Args:
        model: PyTorch model to test
        image_path: Path to test image
        input_size: Input resolution for the model
        save_path: Path to save visualization (optional)
        
    Returns:
        True if test was successful, False otherwise
    """
    if not image_path or not os.path.exists(image_path):
        print("No test image provided or image not found. Skipping model testing.")
        return False
    
    print(f"Testing model with image: {image_path}")
    input_batch = preprocess_image(image_path, input_size)
    
    if input_batch is None:
        return False
    
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    input_batch = input_batch.to(device)
    model = model.to(device)
    
    try:
        # Measure inference time
        start_time = time.time()
        with torch.no_grad():
            output = model(input_batch)['out'][0]
        inference_time = time.time() - start_time
        
        # Process output
        output_predictions = output.argmax(0).byte().cpu().numpy()
        
        # Wall class is typically class 12 in Pascal VOC
        wall_mask = (output_predictions == 12).astype(np.uint8) * 255
        
        # Visualize results
        original_image = Image.open(image_path).convert('RGB')
        original_image = original_image.resize((input_size, input_size))
        
        plt.figure(figsize=(12, 4))
        
        plt.subplot(1, 3, 1)
        plt.imshow(original_image)
        plt.title("Original Image")
        plt.axis('off')
        
        plt.subplot(1, 3, 2)
        plt.imshow(output_predictions, cmap='tab20', vmin=0, vmax=20)
        plt.title("All Segmentation Classes")
        plt.axis('off')
        
        plt.subplot(1, 3, 3)
        plt.imshow(wall_mask, cmap='gray')
        plt.title("Wall Mask")
        plt.axis('off')
        
        plt.suptitle(f"Input Size: {input_size}x{input_size}, Inference Time: {inference_time:.3f}s")
        
        if save_path:
            plt.savefig(save_path)
            print(f"Visualization saved to: {save_path}")
        else:
            plt.show()
        
        plt.close()
        print(f"Inference completed in {inference_time:.3f} seconds")
        return True
    
    except Exception as e:
        print(f"Error testing model: {e}")
        return False

def export_deeplabv3(model_type='mobilenet_v3_large', output_dir='models', input_size=320, 
                    test_image=None):
    """
    Export DeepLabV3 model to TorchScript format for use with Unity Barracuda.
    
    Args:
        model_type: Type of backbone ('mobilenet_v3_large' or 'resnet50')
        output_dir: Directory to save the exported model
        input_size: Input resolution (square) for the model
        test_image: Path to test image (optional)
        
    Returns:
        Path to the exported model
    """
    print(f"\n{'='*50}")
    print(f"Exporting DeepLabV3 {model_type} model with input size {input_size}x{input_size}...")
    print(f"{'='*50}\n")
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Load model
    original_model = load_model(model_type)
    
    # Test model with sample image if provided
    if test_image:
        test_result_dir = os.path.join(output_dir, "test_results")
        os.makedirs(test_result_dir, exist_ok=True)
        test_output_path = os.path.join(test_result_dir, f"test_{model_type}_{input_size}x{input_size}.png")
        test_model(original_model, test_image, input_size, test_output_path)
    
    # Wrap the model to get only the 'out' tensor
    model = DeepLabV3Wrapper(original_model)
    
    # Create dummy input tensor for tracing
    x = torch.randn(1, 3, input_size, input_size)
    
    # Define output filename
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    model_type_str = "mobilenetv3" if model_type == "mobilenet_v3_large" else model_type
    output_filename = f"deeplabv3_{model_type_str}_{input_size}x{input_size}_{timestamp}.pt"
    output_path = os.path.join(output_dir, output_filename)
    
    # Export the model to TorchScript format
    print("Exporting to TorchScript format...")
    try:
        # Trace the model with the dummy input
        traced_model = torch.jit.trace(model, x)
        
        # Save the traced model
        torch.jit.save(traced_model, output_path)
        print(f"Model exported successfully to {output_path}")
        
        # Also save in a Torch serialized format as a backup
        torch_output_path = os.path.join(output_dir, f"deeplabv3_{model_type_str}_{input_size}x{input_size}_{timestamp}.pth")
        torch.save(original_model.state_dict(), torch_output_path)
        print(f"Model state dictionary saved to {torch_output_path}")
        
        return output_path
    
    except Exception as e:
        print(f"Error during export: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description='Export DeepLabV3 model to TorchScript format for Unity Barracuda')
    parser.add_argument('--model_type', type=str, default='mobilenet_v3_large', 
                        choices=['mobilenet_v3_large', 'resnet50'],
                        help='Type of backbone to use (mobilenet_v3_large or resnet50)')
    parser.add_argument('--output_dir', type=str, default='models', 
                        help='Directory to save the exported model')
    parser.add_argument('--input_size', type=int, default=320, 
                        help='Input size for the model (square)')
    parser.add_argument('--test_image', type=str, default=None, 
                        help='Path to test image for validating the model')
    args = parser.parse_args()
    
    # Print system info
    print(f"\nSystem information:")
    print(f"PyTorch version: {torch.__version__}")
    print(f"CUDA available: {torch.cuda.is_available()}")
    if torch.cuda.is_available():
        print(f"CUDA version: {torch.version.cuda}")
        print(f"GPU: {torch.cuda.get_device_name(0)}")
    print()
    
    # Export model
    model_path = export_deeplabv3(
        model_type=args.model_type,
        output_dir=args.output_dir,
        input_size=args.input_size,
        test_image=args.test_image
    )
    
    if model_path:
        print("\n" + "="*50)
        print("Export completed successfully.")
        print("="*50)
        print(f"Model: DeepLabV3 with {args.model_type} backbone")
        print(f"Input size: {args.input_size}x{args.input_size}")
        print(f"Output file: {model_path}")
        print("\nTo use this model in Unity:")
        print("1. Import the TorchScript (.pt) file into your Unity project")
        print("2. Use Unity Barracuda to load and run the model")
        print("3. Connect the model to the DeepLabDecoder component")
        print("4. Set the correct resolution preset to match the model's input size")
        
        file_size_mb = os.path.getsize(model_path) / (1024 * 1024)
        print(f"\nModel file size: {file_size_mb:.2f} MB")
    else:
        print("\nExport failed. Please check the error messages above.")

if __name__ == "__main__":
    main() 