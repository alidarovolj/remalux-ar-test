#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Test script for DeepLabV3 model export
This script demonstrates how to export and test a DeepLabV3 model with a sample image
"""

import os
import sys
import urllib.request
from export_mobilenet import export_deeplabv3, test_model, load_model

def download_sample_image(url, save_path):
    """
    Download a sample image for testing if one doesn't exist
    """
    if os.path.exists(save_path):
        print(f"Sample image already exists at {save_path}")
        return True
    
    print(f"Downloading sample image from {url}")
    try:
        os.makedirs(os.path.dirname(save_path), exist_ok=True)
        urllib.request.urlretrieve(url, save_path)
        print(f"Sample image downloaded to {save_path}")
        return True
    except Exception as e:
        print(f"Error downloading sample image: {e}")
        return False

def main():
    # Create test directory
    test_dir = "test_images"
    os.makedirs(test_dir, exist_ok=True)
    
    # Sample image URL (Creative Commons licensed room image)
    sample_image_url = "https://live.staticflickr.com/4135/4915115384_14f756acf9_b.jpg"
    sample_image_path = os.path.join(test_dir, "room_wall.jpg")
    
    # Download sample image if it doesn't exist
    if not download_sample_image(sample_image_url, sample_image_path):
        print("Failed to download sample image. Trying an alternative...")
        # Alternative image
        sample_image_url = "https://images.pexels.com/photos/1643383/pexels-photo-1643383.jpeg"
        sample_image_path = os.path.join(test_dir, "room_wall.jpg")
        if not download_sample_image(sample_image_url, sample_image_path):
            print("Could not download sample images. Please provide your own test image.")
            return
    
    # Define test parameters
    output_dir = "test_export"
    os.makedirs(output_dir, exist_ok=True)
    
    # Test with small model for quicker results
    input_size = 224
    model_type = "mobilenet_v3_large"
    
    print("\n" + "="*50)
    print(f"Testing DeepLabV3 export with:")
    print(f"- Model type: {model_type}")
    print(f"- Input size: {input_size}x{input_size}")
    print(f"- Test image: {sample_image_path}")
    print(f"- Output directory: {output_dir}")
    print("="*50 + "\n")
    
    # Export the model
    model_path = export_deeplabv3(
        model_type=model_type,
        output_dir=output_dir,
        input_size=input_size,
        optimize=True,
        test_image=sample_image_path
    )
    
    if model_path:
        print("\nExport test completed successfully!")
        print(f"Exported model: {model_path}")
        
        # Verify file size
        model_size_mb = os.path.getsize(model_path) / (1024 * 1024)
        print(f"Model size: {model_size_mb:.2f} MB")
        
        print("\nTest outputs are available in the test_export/test_results directory")
        print("You can now try exporting models with different sizes or backbones.")
        
        print("\nSuggested next steps:")
        print("1. Run './export_all_sizes.sh --test-image test_images/room_wall.jpg'")
        print("2. Import the resulting models into Unity")
        print("3. Test with the DeepLabModelTester component in your Unity project")
    else:
        print("\nExport test failed. Please check the error messages above.")

if __name__ == "__main__":
    main() 