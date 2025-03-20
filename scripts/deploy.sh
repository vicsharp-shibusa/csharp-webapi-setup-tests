#!/bin/bash

# Check if target directory argument is provided
if [ $# -ne 1 ]; then
    echo "Usage: $0 <target_directory>"
    exit 1
fi

TARGET_DIR="$1"
SOURCE_DIR="$(cd "$(dirname "$0")/.." && pwd)"  # Gets root directory by going up from scripts folder
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
declare -A BACKUPS  # Associative array to store backup mappings

# Function to build and publish a .NET project
build_and_publish() {
    local project_path="$1"
    local output_path="$2"
    
    echo "Building and publishing $project_path..."
    dotnet publish "$project_path" -c Release -o "$output_path"
    if [ $? -ne 0 ]; then
        echo "Error: Failed to publish $project_path"
        exit 1
    fi
}

# 1. Backup any existing secrets.json files
if [ -d "$TARGET_DIR" ]; then
    echo "Backing up secrets.json files..."
    while IFS= read -r secret_file; do
        if [ -n "$secret_file" ]; then
            backup_path="${secret_file}.backup_${TIMESTAMP}"
            cp "$secret_file" "$backup_path"
            # Store the mapping of original to backup
            BACKUPS["$secret_file"]="$backup_path"
            echo "Backed up $secret_file to $backup_path"
        fi
    done < <(find "$TARGET_DIR" -name "secrets.json")
fi

# 2. Remove and recreate target directory
echo "Recreating target directory: $TARGET_DIR"
rm -rf "$TARGET_DIR"
mkdir -p "$TARGET_DIR"
if [ $? -ne 0 ]; then
    echo "Error: Failed to create target directory"
    exit 1
fi

# 3. Deploy TestControl.Cli
echo "Deploying TestControl.Cli..."
build_and_publish "$SOURCE_DIR/src/apps/TestControl.Cli/TestControl.Cli.csproj" "$TARGET_DIR"

# 4. Create hosts directory
echo "Creating hosts directory..."
mkdir -p "$TARGET_DIR/hosts"
if [ $? -ne 0 ]; then
    echo "Error: Failed to create hosts directory"
    exit 1
fi

# 5. Deploy each host project
echo "Deploying host projects..."
for host_project in "$SOURCE_DIR"/src/hosts/*; do
    if [ -d "$host_project" ]; then
        host_name=$(basename "$host_project")
        target_host_dir="$TARGET_DIR/hosts/$host_name"
        mkdir -p "$target_host_dir"
        # Append the .csproj file with the same name as the directory
        project_file="$host_project/$host_name.csproj"
        if [ -f "$project_file" ]; then
            build_and_publish "$project_file" "$target_host_dir"
        else
            echo "Warning: No .csproj file found for $host_name"
        fi
    fi
done

# 6. Restore backed up secrets.json files
if [ ${#BACKUPS[@]} -gt 0 ]; then
    echo "Restoring secrets.json files..."
    for original_path in "${!BACKUPS[@]}"; do
        backup_path="${BACKUPS[$original_path]}"
        if [ -f "$backup_path" ]; then
            # Ensure the directory exists before copying
            mkdir -p "$(dirname "$original_path")"
            cp "$backup_path" "$original_path"
            echo "Restored $original_path from $backup_path"
        fi
    done
fi

echo "Deployment completed successfully!"