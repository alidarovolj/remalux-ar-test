# Real Wall Painting System Using Computer Vision

## Overview

The Real Wall Painting System allows you to detect walls in the real world using computer vision and repaint them in virtual space, creating an augmented reality effect similar to the Dulux Visualizer app.

## Features

- Wall detection using computer vision (OpenCV)
- Real-time 3D wall model creation
- Wall painting with various colors
- Intuitive user interface
- Reset wall colors functionality
- Color preview before painting

## Requirements

- Unity 2021.3 or newer
- OpenCVForUnity (activated for your platform)
- Device with camera

## Installation and Setup

### Quick Setup via Editor

1. Open the **Remalux > Create Real Wall Painting Scene** menu in Unity.
2. Choose a location to save the new scene.
3. The system will automatically create all necessary objects, materials, and prefabs.

### Manual Setup

If you prefer to set up the system manually, follow these steps:

1. Create a new scene in Unity.
2. Create an empty GameObject and name it "RealWallPaintingSystem".
3. Add the following components:
   - `RealWallPaintingController`
   - `WallDetector`
   - `WallMeshBuilder`
4. Configure references between components.
5. Create paint materials and assign them in the `RealWallPaintingController` component.
6. Create UI elements (Canvas, color buttons, reset button).

## Using the System

1. **Launch the scene** in Play mode.
2. **Point the camera at walls** in the real world.
3. The system will automatically detect walls and create 3D models of them.
4. **Select a color** from the color palette at the bottom of the screen.
5. **Tap on a wall** to paint it with the selected color.
6. **Press the "Reset" button** to return all walls to their original color.

## System Structure

### Main Components

- **RealWallPaintingController**: Main system controller, coordinates all components.
- **WallDetector**: Detects walls using computer vision.
- **WallMeshBuilder**: Creates 3D models of walls based on detected data.
- **WallMaterialInstanceTracker**: Tracks wall materials and manages their changes.
- **SimpleColorButton**: Component for color selection buttons.
- **SimpleColorPreview**: Component for color preview on walls.

### Wall Detection Algorithm

1. Get image from camera.
2. Convert to grayscale.
3. Apply Canny edge detector.
4. Detect lines using Hough transform.
5. Classify lines as vertical and horizontal.
6. Group nearby lines.
7. Determine walls based on pairs of vertical lines.
8. Create 3D models for detected walls.

## Parameter Configuration

### WallDetector

- **cannyThreshold1**, **cannyThreshold2**: Thresholds for Canny edge detector.
- **minLineLength**: Minimum line length for detection.
- **maxLineGap**: Maximum gap between lines.
- **minWallHeight**, **minWallWidth**: Minimum dimensions for wall determination.
- **showDebugImage**: Enables/disables debug information display.

### WallMeshBuilder

- **wallDistance**: Distance from camera to wall.
- **wallDepth**: Wall thickness.
- **wallExtensionFactor**: Wall extension factor.

### RealWallPaintingController

- **raycastDistance**: Maximum distance for wall determination when tapping.
- **showColorPreview**: Enables/disables color preview.

## Troubleshooting

### Walls Not Detected

- Make sure the camera is pointed at well-lit vertical surfaces.
- Try adjusting the `cannyThreshold1` and `cannyThreshold2` parameters in the `WallDetector` component.
- Increase the `maxLineGap` value for better connection of fragmented lines.

### OpenCVForUnity Errors

- Make sure the OpenCVForUnity plugin is activated for your platform.
- Check that all necessary DLL files are included in the build.

### Wall Painting Issues

- Make sure painting mode is enabled (`isPaintingMode = true`).
- Check that paint materials are properly configured.
- Make sure walls have the `WallMaterialInstanceTracker` component.

## Extending Functionality

You can extend the system's functionality in the following ways:

1. **Adding New Materials**: Create new materials and add them to the `paintMaterials` array.
2. **Improving Wall Detection Algorithm**: Modify the `WallDetector` class for more accurate wall detection.
3. **Adding Textures**: Use textured materials instead of simple colors.
4. **Saving State**: Add functionality to save and load wall painting state.
5. **AR Foundation Integration**: Use AR Foundation for more accurate plane detection and tracking.

## Notes

- The system uses OpenCV for wall detection, which can be resource-intensive on mobile devices.
- For better performance, it's recommended to use devices with a good camera and sufficient computing power.
- Wall detection quality depends on lighting and surface texture. 