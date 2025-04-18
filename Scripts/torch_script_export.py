import torch
import torchvision
import os
import sys
from torchvision.models.segmentation import deeplabv3_mobilenet_v3_large

def export_model(input_size=320):
    """
    Exports a DeepLabV3 MobileNet model to TorchScript format.
    
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
        example_input = torch.rand(1, 3, input_size, input_size)
        
        # Export standard model
        standard_output_path = os.path.join(model_dir, "deeplabv3_mobilenet.pt")
        traced_model = torch.jit.trace(model, example_input)
        torch.jit.save(traced_model, standard_output_path)
        print(f"Exported standard TorchScript model to: {standard_output_path}")
        
        # Export optimized model for mobile
        mobile_output_path = os.path.join(model_dir, "deeplabv3_mobilenet_mobile.pt")
        optimized_model = torch.jit.script(model)
        optimized_model_for_mobile = torch._C._jit_pass_optimize_for_mobile(optimized_model)
        torch.jit.save(optimized_model_for_mobile, mobile_output_path)
        print(f"Exported mobile-optimized TorchScript model to: {mobile_output_path}")
        
        # Print model details
        print("\nModel Details:")
        print(f"Input shape: (1, 3, {input_size}, {input_size})")
        print(f"Standard model size: {os.path.getsize(standard_output_path) / (1024 * 1024):.2f} MB")
        print(f"Mobile model size: {os.path.getsize(mobile_output_path) / (1024 * 1024):.2f} MB")
        
        print("\nExport completed successfully.")
    
    except Exception as e:
        print(f"Error during model export: {e}")
        sys.exit(1)

if __name__ == "__main__":
    export_model(input_size=320) 