#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SERVER_DIR="$(dirname "$SCRIPT_DIR")"
PROJECT_DIR="$SERVER_DIR/SignalEngine.Functions"
OUTPUT_DIR="$SERVER_DIR/publish"

echo "=== Building SignalEngine.Functions ==="
dotnet build "$PROJECT_DIR/SignalEngine.Functions.csproj" -c Release

echo ""
echo "=== Publishing to $OUTPUT_DIR ==="
dotnet publish "$PROJECT_DIR/SignalEngine.Functions.csproj" -c Release -o "$OUTPUT_DIR"

echo ""
echo "Build complete. Output: $OUTPUT_DIR"
