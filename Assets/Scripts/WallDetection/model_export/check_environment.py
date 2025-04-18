#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Environment check script for DeepLabV3 model export
This script verifies that your environment is correctly set up for exporting models
"""

import sys
import platform
import importlib.util
import subprocess
import os
from pathlib import Path

def check_package(package_name, min_version=None):
    """Check if a package is installed and meets minimum version requirements"""
    try:
        spec = importlib.util.find_spec(package_name)
        if spec is None:
            print(f"❌ {package_name}: Not installed")
            return False
        
        if min_version:
            package = importlib.import_module(package_name)
            version = getattr(package, "__version__", "unknown")
            if version == "unknown":
                print(f"✅ {package_name}: Installed (version unknown)")
                return True
            
            if version < min_version:
                print(f"⚠️ {package_name}: Installed but outdated (v{version}, recommended: v{min_version}+)")
                return True
            else:
                print(f"✅ {package_name}: Installed (v{version})")
                return True
        else:
            print(f"✅ {package_name}: Installed")
            return True
    except Exception as e:
        print(f"❌ {package_name}: Error checking - {str(e)}")
        return False

def check_gpu():
    """Check for CUDA availability"""
    try:
        import torch
        if torch.cuda.is_available():
            gpu_name = torch.cuda.get_device_name(0)
            cuda_version = torch.version.cuda
            print(f"✅ GPU: Available ({gpu_name}, CUDA {cuda_version})")
            return True
        else:
            print("⚠️ GPU: Not available (CPU will be used for export)")
            return False
    except Exception as e:
        print(f"⚠️ GPU: Error checking - {str(e)}")
        return False

def check_paths():
    """Check if necessary directories exist"""
    paths = {
        "models": Path("models"),
        "test_images": Path("test_images")
    }
    
    for name, path in paths.items():
        if path.exists():
            print(f"✅ Directory '{name}': Exists")
        else:
            print(f"⚠️ Directory '{name}': Missing (will be created during export)")
            path.mkdir(exist_ok=True)

def check_export_scripts():
    """Check if export scripts exist and are executable"""
    scripts = {
        "export_mobilenet.py": Path("export_mobilenet.py"),
        "test_export.py": Path("test_export.py")
    }
    
    if platform.system() != "Windows":
        scripts.update({
            "export_all_sizes.sh": Path("export_all_sizes.sh"),
            "run_test.sh": Path("run_test.sh")
        })
    else:
        scripts.update({
            "export_all_sizes.bat": Path("export_all_sizes.bat"),
            "run_test.bat": Path("run_test.bat")
        })
    
    for name, path in scripts.items():
        if path.exists():
            if platform.system() != "Windows" and name.endswith(".sh"):
                if os.access(path, os.X_OK):
                    print(f"✅ Script '{name}': Exists and is executable")
                else:
                    print(f"⚠️ Script '{name}': Exists but is not executable (run 'chmod +x {name}')")
            else:
                print(f"✅ Script '{name}': Exists")
        else:
            print(f"❌ Script '{name}': Missing")

def main():
    """Run all environment checks"""
    print("\n" + "="*50)
    print("DeepLabV3 Model Export Environment Check")
    print("="*50)
    
    # System information
    print(f"\nSystem Information:")
    print(f"Python version: {platform.python_version()}")
    print(f"OS: {platform.system()} {platform.release()}")
    print(f"Machine: {platform.machine()}")
    
    # Package checks
    print("\nChecking required packages:")
    packages = [
        ("torch", "1.9.0"),
        ("torchvision", "0.10.0"),
        ("onnx", "1.10.0"),
        ("numpy", "1.19.0"),
        ("matplotlib", "3.4.0"),
        ("PIL", None)  # Pillow
    ]
    
    missing_packages = []
    for package, min_version in packages:
        if not check_package(package, min_version):
            missing_packages.append(package)
    
    # GPU availability
    print("\nChecking GPU availability:")
    check_gpu()
    
    # Path checks
    print("\nChecking directories:")
    check_paths()
    
    # Script checks
    print("\nChecking export scripts:")
    check_export_scripts()
    
    # Summary
    print("\n" + "="*50)
    print("Environment Check Summary")
    print("="*50)
    
    if missing_packages:
        print("\n⚠️ Missing required packages:")
        for package in missing_packages:
            print(f"  - {package}")
        print("\nInstall missing packages with:")
        print("pip install -r requirements.txt")
    else:
        print("\n✅ All required packages are installed")
    
    print("\nNext steps:")
    if platform.system() != "Windows":
        print("1. Run './run_test.sh' to test the export functionality")
        print("2. Run './export_all_sizes.sh' to export models at different sizes")
    else:
        print("1. Run 'run_test.bat' to test the export functionality")
        print("2. Run 'export_all_sizes.bat' to export models at different sizes")
    
    print("3. Import the generated models into your Unity project")
    print("="*50)

if __name__ == "__main__":
    main() 