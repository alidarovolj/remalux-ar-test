#!/bin/bash
# Shell script to fix Barracuda dependencies in Unity projects
# This addresses circular reference issues with Barracuda in Unity

echo "Fixing Barracuda dependencies for Mac/Linux..."

# Create necessary directories
if [ ! -d "Assets/Plugins/BarracudaFix" ]; then
    mkdir -p "Assets/Plugins/BarracudaFix"
    echo "Created BarracudaFix plugins directory"
fi

# Create assembly reference file
echo '{"reference": "Unity.Barracuda"}' > "Assets/asmref-barracuda-fix.asmref"
echo "Created assembly reference file"

# Add define symbols by creating a marker file
mkdir -p "ProjectSettings"
echo "USE_BARRACUDA_INDIRECTLY=1" > "ProjectSettings/BarracudaFixApplied.txt"
echo "FIXED_BUILD_ORDER=1" >> "ProjectSettings/BarracudaFixApplied.txt"

# Attempt to find and copy Barracuda DLL
FOUND_DLL=false
for DLL_PATH in $(find "Library" -name "Unity.Barracuda.dll" 2>/dev/null); do
    echo "Found Barracuda DLL: $DLL_PATH"
    cp "$DLL_PATH" "Assets/Plugins/BarracudaFix/"
    FOUND_DLL=true
    break
done

if [ "$FOUND_DLL" = false ]; then
    echo "WARNING: Could not find Unity.Barracuda.dll in Library folder"
    echo "Please run Unity once before running this script"
fi

# Delete temporary Unity files to force recompilation
if [ -d "Library/ScriptAssemblies" ]; then
    rm -rf "Library/ScriptAssemblies"
    echo "Removed ScriptAssemblies to force recompilation"
fi

echo
echo "Barracuda fix applied. Please restart Unity for changes to take full effect."
echo "After restarting, run the \"Tools/Fix All Barracuda Issues\" menu command in Unity."
echo 