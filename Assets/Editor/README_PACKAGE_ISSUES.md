# Fixing Build Issues in Remalux AR

This document provides instructions for addressing common build and package reference issues in the Remalux AR project.

## Common Errors

1. **FileNotFoundException for DLL Files**
```
System.IO.FileNotFoundException: Could not find file '/Users/olzhasfront/Documents/Projects/remalux-ar-test/Unity.XR.ARSubsystems.dll'
```

2. **Build System Circular Reference Error**
```
Internal build system error. BuildProgram exited with code 134.
Unhandled exception. System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.ArgumentException: Action "CopyFiles Library/ScriptAssemblies/Unity.Barracuda.dll" outputs...
```

3. **Broken Material References**
```
Could not extract GUID in text file Assets/Materials/WallPaintMaterial.mat
Broken text PPtr. GUID 00000000000000000000000000000000 fileID -6465566751694194690 is invalid!
```

4. **Type Loading Exceptions**
```
TypeLoadException: Could not load type 'UnityEngine.InputSystem.InputActionAsset' from assembly 'Unity.InputSystem'.
```

## Quick Fix (Recommended)

1. In Unity, go to **Tools > Fix All Build Issues**
   - This will automatically fix all common build issues:
     - Clean up DLLs from project root
     - Fix broken material references
     - Delete problematic build artifacts

2. If you're still having issues after using the comprehensive fix:
   - Close Unity completely
   - Delete the entire `Library` folder
   - Restart Unity and let it regenerate everything
   - Run **Tools > Fix All Build Issues** again before building

## Manual Fixes

If the automatic tool doesn't work, you can manually apply these fixes:

### For DLL Reference Issues:
- Use **Tools > Clean Up API Updater DLLs** to remove DLLs from project root
- Make sure all necessary packages are installed in Package Manager

### For Circular Reference Errors:
- Close Unity
- Delete the Library/Bee and Library/BuildCache folders
- Restart Unity

### For Broken Material References:
- Open the material in the Inspector
- Change the shader to a Standard shader
- Save the asset

### For Input System Issues:
- Make sure the Input System package is properly installed
- Add the ENABLE_INPUT_SYSTEM define symbol in Project Settings

## Technical Details

The build issues are primarily caused by:

1. **Circular References**: Unity's build system can't handle dependencies that are both inputs and outputs.
2. **Missing References**: Some packages need to be explicitly referenced for the build to succeed.
3. **Broken Asset References**: After deleting the Library folder, some assets may have broken references.

The fix scripts address these by:
- Ensuring clean build artifacts before starting the build
- Fixing broken references in materials and other assets
- Creating explicit references to problematic assemblies

## Reference

If you run into other issues, the following files may be helpful:
- `Assets/Scripts/BarracudaFix.cs` - Helps with Barracuda references
- `Assets/Scripts/InputSystemFix.cs` - Helps with InputSystem references
- `Assets/Editor/BuildFixer.cs` - Contains the comprehensive fix tool 