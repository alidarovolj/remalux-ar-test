@echo off
:: Script to export DeepLabV3 MobileNet model at different sizes with optimization and testing

:: Set environment
set export_dir=models
set test_image=test_images\room_wall.jpg

:: Create directories if they don't exist
if not exist %export_dir% mkdir %export_dir%
if not exist test_images mkdir test_images

:: Display help if needed
if "%1"=="-h" goto :help
if "%1"=="--help" goto :help
goto :continue

:help
echo Usage: export_all_sizes.bat [--no-optimize] [--test-image PATH]
echo   --no-optimize    : Skip model optimization
echo   --test-image PATH: Specify custom test image
exit /b 0

:continue
:: Process arguments
set optimize=--optimize
set args=%*
echo %args% | findstr /C:"--no-optimize" >nul && (
    set optimize=
    echo Model optimization disabled
)

:: Check if test image path is specified
set test_img_arg=
set prev_arg=
for %%a in (%*) do (
    if "!prev_arg!"=="--test-image" (
        set test_image=%%a
    )
    set prev_arg=%%a
)

:: Check if test image exists
if exist %test_image% (
    set test_img_arg=--test_image %test_image%
    echo Using test image: %test_image%
) else (
    echo No test image found at %test_image%, testing will be skipped
    echo Please add a test image to the test_images directory or specify with --test-image
)

echo ===== Exporting DeepLabV3 MobileNet models at different sizes =====
echo Export directory: %export_dir%
echo.

:: Export small model (224x224)
echo ===== Exporting 224x224 model =====
python export_mobilenet.py --input_size 224 --output_dir %export_dir% %optimize% %test_img_arg%

:: Export medium model (320x320)
echo ===== Exporting 320x320 model =====
python export_mobilenet.py --input_size 320 --output_dir %export_dir% %optimize% %test_img_arg%

:: Export large model (512x512)
echo ===== Exporting 512x512 model =====
python export_mobilenet.py --input_size 512 --output_dir %export_dir% %optimize% %test_img_arg%

:: Export very large model (768x768)
echo ===== Exporting 768x768 model =====
python export_mobilenet.py --input_size 768 --output_dir %export_dir% %optimize% %test_img_arg%

echo.
echo ===== Export completed =====
echo Models are saved in the '%export_dir%' directory
echo Test results (if any) are saved in the '%export_dir%\test_results' directory
echo Please import the models into your Unity project at Assets\Resources\Models
pause 