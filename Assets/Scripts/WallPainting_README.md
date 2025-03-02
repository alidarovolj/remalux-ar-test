# Wall Painting System Setup Guide

This document provides detailed instructions for setting up the Wall Painting System in your Unity project.

## Overview

The Wall Painting System allows users to:
- Detect walls in AR
- Select from multiple paint colors
- Paint walls by tapping on them
- Reset walls to their original appearance
- See a preview of the selected color before painting

## Setup Instructions

### 1. Create a Setup GameObject

1. In your Unity scene, right-click in the Hierarchy panel
2. Select **Create Empty**
3. Rename it to "WallPaintingSetup"
4. Add the **WallPaintingSetupGuide** component to this GameObject

### 2. Assign Required References

In the Inspector panel for the WallPaintingSetupGuide component:

#### Step 1: Assign Prefabs
- **PaintingUI Prefab**: Assign `Assets/Prefabs/UI/PaintingUI.prefab`
- **ColorPreview Prefab**: Assign `Assets/Prefabs/UI/ColorPreview.prefab`

#### Step 2: Assign Materials
- **Default Wall Material**: Assign `Assets/Materials/Paints/DefaultWallMaterial.mat`
- **Paint Materials**: Add all paint materials from `Assets/Materials/Paints/` folder:
  - BluePaint.mat
  - GreenPaint.mat
  - RedPaint.mat
  - YellowPaint.mat
  - PurplePaint.mat

#### Step 3: Assign Scene References
- **Main Camera**: Assign your AR camera (or use the "Find Main Camera" button)
- **Main Canvas**: Assign your UI canvas (or use the "Find Canvas" button)

#### Step 4: Configure Wall Layer
- Set the **Wall Layer Mask** to the layer you want to use for wall detection
- By default, it uses the "Default" layer (layer 0)
- For better results, create a dedicated "Wall" layer in your project settings

### 3. Complete Setup

Once all references are assigned, click the **Setup Wall Painting System** button.

This will:
1. Create a new GameObject with the SceneSetup component
2. Configure all components automatically
3. Set up the wall painting system in your scene

### 4. Testing the System

After setup:
1. Enter Play mode
2. Point your camera at a wall
3. Select a color from the UI
4. Tap on a wall to paint it
5. Use the Reset button to restore original wall colors

## Troubleshooting

- **No walls detected**: Make sure your wall objects are on the correct layer
- **Cannot paint walls**: Check that the WallPainter component is enabled
- **UI not showing**: Verify that your Canvas is properly configured

## Advanced Configuration

For advanced users who want to modify the system:

- **WallPainter.cs**: Controls the painting functionality
- **PaintColorSelector.cs**: Manages the color selection UI
- **WallPaintingManager.cs**: Coordinates the overall system
- **SceneSetup.cs**: Handles the initial setup of components

## Support

If you encounter any issues, please contact support or refer to the documentation. 