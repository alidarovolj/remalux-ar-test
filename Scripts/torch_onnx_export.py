import torch
import torchvision
import os
import sys
from torchvision.models.segmentation import deeplabv3_mobilenet_v3_large

def export_onnx(input_size=320):
    """
    Exports a DeepLabV3 MobileNet model to ONNX format.
    
    Args:
        input_size (int): Input resolution (height and width) for the model.
    
    Returns:
        None
    """
    try:
        print(f"Loading DeepLabV3 MobileNet model...")
        # Load model with pretrained weights
        model = deeplabv3_mobilenet_v3_large(pretrained=True, progress=True)
        model.eval()
        
        # Create directory for saving model if it doesn't exist
        model_dir = "../Assets/Models"
        if not os.path.exists(model_dir):
            os.makedirs(model_dir)
            print(f"Created directory: {model_dir}")
        
        # Create example input
        dummy_input = torch.randn(1, 3, input_size, input_size)
        
        # Define output path
        output_path = os.path.join(model_dir, "deeplabv3_mobilenet.onnx")
        
        # Export model to ONNX format
        torch.onnx.export(
            model,                      # PyTorch model
            dummy_input,                # Input tensor
            output_path,                # Output file path
            export_params=True,         # Export model parameters
            opset_version=11,           # ONNX opset version (11 is well-supported)
            do_constant_folding=True,   # Optimize constant folding
            input_names=['input'],      # Names for input tensors
            output_names=['output'],    # Names for output tensors
            dynamic_axes={
                'input': {0: 'batch_size'},
                'output': {0: 'batch_size'}
            },                          # Support for dynamic batch size
            verbose=False
        )
        
        print(f"Exported ONNX model to: {output_path}")
        print(f"Model size: {os.path.getsize(output_path) / (1024 * 1024):.2f} MB")
        print(f"Input shape: (batch_size, 3, {input_size}, {input_size})")
        print("\nExport completed successfully.")
        
    except Exception as e:
        print(f"Error during model export: {e}")
        sys.exit(1)

if __name__ == "__main__":
    export_onnx(input_size=320) 