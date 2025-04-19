@echo off
:: Script to convert DeepLabV3 MobileNet model at different sizes

:: Create directories if they don't exist
if not exist models mkdir models

echo ===== Converting DeepLabV3 MobileNet models at different sizes =====
echo.

:: Convert small model (224x224)
echo ===== Converting 224x224 model =====
python convert_torchvision.py --input_size 224 --model_type mobilenet_v3_large --output_dir models

:: Convert medium model (320x320)
echo ===== Converting 320x320 model =====
python convert_torchvision.py --input_size 320 --model_type mobilenet_v3_large --output_dir models

:: Convert large model (512x512)
echo ===== Converting 512x512 model =====
python convert_torchvision.py --input_size 512 --model_type mobilenet_v3_large --output_dir models

:: Convert very large model (768x768)
echo ===== Converting 768x768 model =====
python convert_torchvision.py --input_size 768 --model_type mobilenet_v3_large --output_dir models

echo.
echo ===== Conversion completed =====
echo Models are saved in the 'models' directory
echo Please import the models into your Unity project at Assets\Resources\Models
pause 