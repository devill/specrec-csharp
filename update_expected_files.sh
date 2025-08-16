#!/bin/bash

# Script to update expected directories based on .expected.out files

set -e

for out_file in $(find SpecRec.CLI.Tests/fixtures -name "*.expected.out"); do
    # Get the corresponding .expected and .received directories
    base_name="${out_file%.expected.out}"
    expected_dir="${base_name}.expected"
    received_dir="${base_name}.received"
    
    if [[ ! -d "$received_dir" ]]; then
        echo "Skipping $base_name - no received directory found"
        continue
    fi
    
    echo "Processing: $base_name"
    
    # Extract filenames from the .expected.out file
    expected_files=$(grep -E "Generated (wrapper class|interface):" "$out_file" | sed -E 's/.*: (.*)$/\1/')
    
    if [[ -z "$expected_files" ]]; then
        echo "  No files found in $out_file"
        continue
    fi
    
    # Create expected directory if it doesn't exist
    mkdir -p "$expected_dir"
    
    # Remove all existing .cs files from expected directory
    rm -f "$expected_dir"/*.cs
    
    # Copy the expected files from received to expected
    for file in $expected_files; do
        if [[ -f "$received_dir/$file" ]]; then
            echo "  Copying $file"
            cp "$received_dir/$file" "$expected_dir/"
        else
            echo "  WARNING: $file not found in $received_dir"
        fi
    done
done

echo "Done updating expected files!"