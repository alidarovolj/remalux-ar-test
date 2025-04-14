# WallPainting Setup Instructions

This README explains how to fix the common issues with the Wall Painting scene in the project.

## Issues Fixed

1. **Camera Tracked Pose Driver Warning**
   - "Camera 'Main Camera' does not use a Tracked Pose Driver (Input System)" warning
   - **Solution**: The XRSetupManager now correctly adds and configures a Tracked Pose Driver (Input System) component to the camera

2. **Camera Preview Missing Warning**
   - "RealWallPaintingController: Camera preview RawImage is not assigned!" warning
   - **Solution**: The new setup scripts automatically create and assign the camera preview RawImage

3. **Missing WallDetector Warning**
   - "WallDetector не назначен! Обнаружение стен не будет работать." warning
   - **Solution**: WallDetector is now automatically created and assigned to the RealWallPaintingController

## How to Fix Your Scene

There are two ways to fix these issues:

### Method 1: Using the Setup Script

1. Add a new GameObject to your scene
2. Add the `WallPaintingSceneSetup` component to it
3. Click "Play" or use the context menu option "Setup Scene Automatically"

This will automatically:
- Set up the XR Origin with proper camera offset
- Create the camera preview UI
- Create and configure the WallDetector and RealWallPaintingController
- Connect all components correctly

### Method 2: Manual Setup

If you prefer to set up the components manually:

1. **XR Setup:**
   - Add `XROrigin` component to an empty GameObject
   - Create a child GameObject "Camera Offset" and assign it to XROrigin.CameraFloorOffsetObject
   - Make your Main Camera a child of Camera Offset
   - Add `UnityEngine.InputSystem.XR.TrackedPoseDriver` to the Main Camera
   - Configure the position action to '<XRHMD>/centerEyePosition'
   - Configure the rotation action to '<XRHMD>/centerEyeRotation'

2. **Camera Preview:**
   - Create a RawImage in your Canvas UI
   - Assign this RawImage to the `cameraPreview` field in RealWallPaintingController

3. **WallDetector:**
   - Add a WallDetector component to your controller GameObject
   - Assign the Main Camera to its `mainCamera` field
   - Assign this WallDetector to the `wallDetector` field in RealWallPaintingController

## Known Issues

- The XR Simulation may override the Camera Y Offset. This is normal behavior and only affects simulation mode.
- If using the XRSetupManager and WallPaintingSceneSetup in the same scene, make sure only one is active to avoid conflicts.

## Support

If you encounter any issues with the setup, please contact the development team. 