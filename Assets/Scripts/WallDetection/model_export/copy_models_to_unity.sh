#!/bin/bash
# Script to copy exported models to Unity Resources folder

# Define paths
MODELS_DIR="models"
RESOURCES_DIR="../../../Resources/Models"
TIMESTAMP=$(date "+%Y%m%d_%H%M%S")

# Create Resources directory if it doesn't exist
mkdir -p "$RESOURCES_DIR"

# Check if models exist
if [ ! -d "$MODELS_DIR" ] || [ -z "$(ls -A $MODELS_DIR/*.pt 2>/dev/null)" ]; then
    echo "No exported models found in $MODELS_DIR"
    exit 1
fi

echo "===== Copying models to Unity Resources folder ====="
echo "Source: $MODELS_DIR"
echo "Destination: $RESOURCES_DIR"
echo ""

# Create backup of existing models if any
if [ -n "$(ls -A $RESOURCES_DIR/*.pt 2>/dev/null)" ]; then
    BACKUP_DIR="$RESOURCES_DIR/backup_$TIMESTAMP"
    mkdir -p "$BACKUP_DIR"
    echo "Creating backup of existing models to $BACKUP_DIR"
    mv "$RESOURCES_DIR"/*.pt "$BACKUP_DIR"/ 2>/dev/null
    echo "Backup created successfully"
fi

# Copy all .pt files (TorchScript models)
echo "Copying models..."
cp "$MODELS_DIR"/*.pt "$RESOURCES_DIR"/

# List copied models
echo ""
echo "Copied models:"
for model in "$RESOURCES_DIR"/*.pt; do
    echo "- $(basename "$model")"
done

echo ""
echo "===== Copy completed ====="
echo "Models are now available in the Unity Resources folder"
echo "You can now use them with the DeepLabDecoder component" 