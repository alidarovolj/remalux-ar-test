# Upgrading to ARFoundation 6.0.5

This document outlines the changes made to support ARFoundation 6.0.5 in the Remalux AR project.

## Key Changes

1. **Assembly References**
   - Added ARFoundation 6.0.5 assembly references to `Remalux.AR.asmdef` and `Remalux.WallDetection.asmdef`
   - Created new `Remalux.AR.Core.asmdef` with essential AR references
   - Added missing references to ARSubsystems

2. **Namespace Updates**
   - Updated `using Unity.XR.CoreUtils` namespace references
   - Added `using UnityEngine.XR.ARSubsystems` where needed
   - Ensured consistent use of `XROrigin` vs `Unity.XR.CoreUtils.XROrigin`

3. **ARKit Subsystem Changes**
   - Updated casting of ARKit subsystems from `(ARKitSessionSubsystem)` to `as ARKitSessionSubsystem`
   - Added proper exception handling for subsystem access

4. **Barracuda Model Integration**
   - Updated `BarracudaModelChecker.cs` to use improved tensor shape access methods
   - Added null checks and exception handling for model loading and worker creation
   - Created helper methods for accessing tensor dimensions correctly

## Manual Fixes Required

Some issues might still need manual attention:

1. **Interface Implementations**: Check any custom implementations of ARFoundation interfaces
2. **Event Handlers**: Verify event handlers are using the correct signature for ARFoundation 6.0.5
3. **XROrigin References**: Ensure all XROrigin references are properly updated

## Troubleshooting

If you encounter compilation errors:

1. **Missing Types**: Check assembly references in .asmdef files
2. **Namespace Issues**: Use the ARFoundationReferenceUpdater tool under Tools > AR > Update ARFoundation References
3. **Runtime Errors**: Look for missing subsystem initialization or incorrect casting

## Reference Documentation

- [ARFoundation 6.0.5 Documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/index.html)
- [ARFoundation 6.0.5 Migration Guide](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/migration-guide-5x.html)
- [Barracuda API Changes](https://docs.unity3d.com/Packages/com.unity.barracuda@3.0/manual/index.html) 