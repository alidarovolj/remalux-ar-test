# Dulux Style Wall Visualizer

This system implements a Dulux Visualizer-like wall painting visualization system that works with the existing wall detection functionality. It allows users to:

1. Detect walls in the environment
2. Select colors from a palette
3. Paint walls with realistic color visualization
4. Toggle between detection and painting modes

## Usage Instructions

### Wall Detection Mode

1. Point your camera at walls in the environment
2. Press the "Capture" button to start detecting walls
3. The system will highlight detected walls with contours
4. Press the "MODE" button to switch to painting mode

### Painting Mode

1. Once walls are detected, switch to painting mode
2. Select a color from the palette at the bottom of the screen
3. Tap on any wall to paint it with the selected color
4. Press the "Apply" button to apply the color to all walls at once
5. Press "DETECT" to return to detection mode

## Features

- Realistic wall color visualization similar to Dulux Visualizer
- Preserves original wall texture details when painting
- Provides real-time highlighting of walls when hovering
- Shows visual feedback when painting walls
- Color palette with Dulux-like color options
- Wall contour detection and visualization system
- Toggle between detection and painting modes

## Technical Details

The system uses:
- Computer vision for wall detection
- Material modification for realistic painting
- Realistic texture blending to maintain surface details
- UI elements for color selection and mode switching
- Raycasting for wall selection and interaction

## Implementation Notes

This implementation keeps the existing wall detection and contour system intact while adding Dulux-style painting functionality. The painting mode overlays new textures on detected walls in a way that maintains lighting and texture details, similar to how the Dulux Visualizer app works. 