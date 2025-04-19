@echo off
:: Script to download DeepLabV3 MobileNet model at different sizes

:: Create directories if they don't exist
if not exist models mkdir models

echo ===== Downloading DeepLabV3 MobileNet models at different sizes =====
echo.

:: Download small model (224x224)
echo ===== Downloading 224x224 model =====
python download_model.py --model_size 224 --model_type mobilenetv3 --output_dir models

:: Download medium model (320x320)
echo ===== Downloading 320x320 model =====
python download_model.py --model_size 320 --model_type mobilenetv3 --output_dir models

:: Download large model (512x512)
echo ===== Downloading 512x512 model =====
python download_model.py --model_size 512 --model_type mobilenetv3 --output_dir models

:: Download very large model (768x768)
echo ===== Downloading 768x768 model =====
python download_model.py --model_size 768 --model_type mobilenetv3 --output_dir models

echo.
echo ===== Download completed =====
echo Models are saved in the 'models' directory
echo Please import the models into your Unity project at Assets\Resources\Models
pause 