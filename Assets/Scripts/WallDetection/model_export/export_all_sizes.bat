@echo off
REM Script to export DeepLabV3 MobileNet model at different sizes

REM Create output directory if not exists
if not exist models mkdir models

echo ===== Exporting DeepLabV3 MobileNet models at different sizes =====

REM Export small model (224x224)
echo.
echo ===== Exporting 224x224 model =====
python export_mobilenet.py --input_size 224

REM Export medium model (320x320)
echo.
echo ===== Exporting 320x320 model =====
python export_mobilenet.py --input_size 320

REM Export large model (512x512)
echo.
echo ===== Exporting 512x512 model =====
python export_mobilenet.py --input_size 512

REM Export very large model (768x768)
echo.
echo ===== Exporting 768x768 model =====
python export_mobilenet.py --input_size 768

echo.
echo ===== Export completed =====
echo Models are saved in the 'models' directory
echo Please import them into your Unity project
pause 