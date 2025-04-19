#!/bin/bash

# Script to safely remove symbolic links from a Unity project
# Run this script from the root of your Unity project

PROJECT_ROOT=$(pwd)
SYMLINKS_DIR="Assets/Scripts/Utils/venv/bin"

echo "Checking for symbolic links in $SYMLINKS_DIR..."

# Make sure the directory exists
if [ ! -d "$SYMLINKS_DIR" ]; then
    echo "Directory $SYMLINKS_DIR does not exist!"
    exit 1
fi

# Create backup directory
BACKUP_DIR="$PROJECT_ROOT/SymlinksBackup"
mkdir -p "$BACKUP_DIR"
echo "Created backup directory at $BACKUP_DIR"

# Handle the python symlinks
for SYMLINK in python python3 python3.13; do
    SYMLINK_PATH="$SYMLINKS_DIR/$SYMLINK"
    
    if [ -L "$SYMLINK_PATH" ]; then
        echo "Found symlink: $SYMLINK_PATH"
        
        # Get the target of the symlink
        TARGET=$(readlink "$SYMLINK_PATH")
        echo "  Target: $TARGET"
        
        # Save the target path to a file for reference
        echo "$TARGET" > "$BACKUP_DIR/$SYMLINK.target"
        
        # Remove the symlink
        rm "$SYMLINK_PATH"
        echo "  Removed symlink"
        
        # Create a placeholder text file instead
        echo "This file was a symlink to: $TARGET" > "$SYMLINK_PATH.txt"
        echo "The symlink was removed to avoid Unity warnings." >> "$SYMLINK_PATH.txt"
        echo "  Created placeholder .txt file"
    else
        echo "No symlink found at $SYMLINK_PATH"
    fi
done

echo "Done removing symlinks."
echo "Symbolic links have been removed and replaced with .txt placeholders."
echo "Original target paths are stored in the $BACKUP_DIR directory." 