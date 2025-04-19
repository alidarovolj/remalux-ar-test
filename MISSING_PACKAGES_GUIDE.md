# Resolving Missing Package References in Remalux AR Test

This guide will help you resolve the compilation errors related to missing package references in the project.

## Required Packages

Based on the error messages, your project needs the following packages:

1. Unity Barracuda (Neural Network package)
2. Unity UGUI (which now includes TextMeshPro)
3. AR Foundation and related packages

## Solution Steps

### 1. Open the Package Manager

- In Unity, go to `Window > Package Manager`

### 2. Install or Update Barracuda

- Click the "+" button in the Package Manager
- Select "Add package by name"
- Enter `com.unity.barracuda` with version `3.0.0` or later
- Click "Add"

### 3. Install or Update UGUI

- Click the "+" button in the Package Manager
- Select "Add package by name"
- Enter `com.unity.ugui` with version `2.0.0` or later
- Click "Add"

**Note:** TextMeshPro is now part of the UGUI package, so you no longer need to install it separately.

### 4. Install AR Foundation and Required AR Packages

- Click the "+" button in the Package Manager
- Select "Add package by name"
- Enter `com.unity.xr.arfoundation` with version `6.0.5` or later
- Click "Add"
- Repeat for:
  - `com.unity.xr.arcore` (version `6.0.5` or later)
  - `com.unity.xr.arkit` (version `6.0.5` or later)
  - `com.unity.xr.management` (version `4.5.0` or later)

### 5. Resolve Assembly Definitions

The project has been set up with proper assembly definition files. After installing the packages, these should automatically link to the correct references.

Assembly definition files created:
- `Assets/Scripts/WallDetection/Remalux.WallDetection.asmdef`
- `Assets/Scripts/Settings/Remalux.Settings.asmdef`
- `Assets/Scripts/UI/Remalux.UI.asmdef`
- `Assets/Scripts/WallPainting/Remalux.WallPainting.asmdef`
- `Assets/Scripts/AR/Remalux.AR.asmdef`
- `Assets/Scripts/WallDetection/Editor/Remalux.WallDetection.Editor.asmdef`

**Important:** The duplicate `Assets/Scripts/Remalux.AR.asmdef` has been removed to fix the duplicate assembly error.

### 6. Import Unity UI Essential Resources

If you still see TextMeshPro errors after installing the UGUI package:
1. Go to `Window > UI Toolkit > Download TMP Essentials` (or similar menu option)
2. Click "Import" on the dialog that appears

### 7. Reload/Restart Unity

- After installing all packages, reload the project or restart Unity
- This will ensure all package references are properly updated

### 8. Update any Deprecated APIs

Some warnings were shown about using deprecated APIs:
- `ARSessionOrigin` is deprecated; use `XROrigin` instead in `ARWorldMapController.cs`

### 9. Remove or Fix Symbolic Links

Unity has detected symbolic links in your project at:
- `Assets/Scripts/Utils/venv/bin/python`
- `Assets/Scripts/Utils/venv/bin/python3.13`
- `Assets/Scripts/Utils/venv/bin/python3`

To fix this:
1. Either remove these symbolic links
2. Or replace them with actual files
3. Or move the Python virtual environment outside the Assets folder

Symbolic links can cause project corruption issues in Unity.

### 10. Common GUID References

The following GUIDs are used in assembly definitions:
- `GUID:6055be8ebefd69e48b49212b09b47b2f` = Unity.TextMeshPro (via Unity.UGUI)
- `GUID:75469ad4d38634e559750d17036d5f7c` = Unity.InputSystem
- `GUID:5c2b5ba89f9e74e418232e154bc5cc7a` = Unity.Barracuda
- `GUID:a9420e37d7990b54abdef6688edbe313` = Unity.Barracuda.Editor
- `GUID:f9fe0089ec81f4079af78eb2287a6163` = Unity.XR.ARFoundation
- `GUID:30c8cca262b5718458d1fd2f13cbb489` = Unity.XR.Management
- `GUID:2bafac87e7f4b9b418d9448d219b01ab` = Unity.UGUI

## Additional NuGet Packages

Your project is using NuGetForUnity with the following packages:
- OpenCvSharp4 (version 4.10.0.20241108)
- OpenCvSharp4.runtime.osx (version 4.6.0.20230105)
- System.Runtime.CompilerServices.Unsafe (version 6.0.0)

These should continue to work correctly once the Unity packages are resolved.

## Testing Your Fix

After completing the steps above, try building the project again. The compilation errors related to missing namespaces and types should be resolved. 