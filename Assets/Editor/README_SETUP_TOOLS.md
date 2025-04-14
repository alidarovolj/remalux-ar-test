# Wall Painting System Setup Tools

This document explains the available setup tools for the Wall Painting system and how to use them.

## Available Setup Options

The project now provides multiple ways to set up the Wall Painting system in your scene:

1. **Quick Setup** - Menu: `Window > Remalux > Quick Wall Painting Setup`
   - Creates all necessary components with default settings
   - Uses the new dependency-free setup method
   - No configuration required

2. **Setup Window** - Menu: `Window > Remalux > Wall Painting Setup Window`
   - Opens a configuration window where you can customize the setup
   - Allows choosing which components to create
   - Provides multiple setup methods:
     - "Setup Using New Components" - Uses the reflection-based approach
     - "Setup Using New Scene Setup Script" - Uses the WallPaintingSceneSetup component

3. **Legacy Setup** - Menu: `Window > Remalux > Legacy Setup Tools > Legacy Wall Painting Setup`
   - The previous setup tool (for backward compatibility)
   - Not recommended for new projects

## How to Use

### Quick Setup

For most cases, this is the recommended approach:

1. Open your scene
2. Click `Window > Remalux > Quick Wall Painting Setup`
3. The system will automatically create and configure all necessary components

### Using the Setup Window

For more control over the setup process:

1. Click `Window > Remalux > Wall Painting Setup Window`
2. Configure the desired options:
   - Toggle XR setup, Wall Painting system, and UI components
   - Adjust camera Y offset if needed
3. Click one of the setup buttons:
   - "Setup Using New Components" - Creates components directly
   - "Setup Using New Scene Setup Script" - Adds the setup script to a GameObject

### Fixing Menu Conflicts

If you encounter the error:
```
Cannot add menu item 'Window/Remalux/Setup Wall Painting System' for method 'Remalux.Editor.RealWallPaintingSetupMenu.SetupRealWallPaintingSystem' because a menu item with the same name already exists.
```

The cause was duplicate menu items in different scripts. This has been fixed by:

1. Creating a unified setup tool
2. Moving the legacy setup tool to a different menu path
3. Removing the duplicate menu item

## Troubleshooting

If you experience issues with the setup:

1. Make sure all required assemblies are compiled
2. Check the Console for detailed error messages
3. Try the legacy setup tool if the new tools are not working

## Technical Notes

The new setup tools use reflection to avoid direct dependencies on specific packages, making them more resilient to missing components or different Unity versions. 