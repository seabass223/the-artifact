#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")/SignalEngine.Functions"

echo "=== Starting SignalEngine locally ==="
echo "Make sure Azurite is running (npx azurite) for local blob storage."
echo "Make sure local.settings.json has your OPENAI_API_KEY set."
echo ""

cd "$PROJECT_DIR"
func start
