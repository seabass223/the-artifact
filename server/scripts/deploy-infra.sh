#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
INFRA_DIR="$(dirname "$SCRIPT_DIR")/infra"

RESOURCE_GROUP="${1:?Usage: deploy-infra.sh <resource-group> [location]}"
LOCATION="${2:-eastus}"

echo "=== Deploying Azure infrastructure ==="
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo ""

# Create resource group if it doesn't exist
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none 2>/dev/null || true

# Deploy Bicep template
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$INFRA_DIR/main.bicep" \
  --parameters "$INFRA_DIR/parameters.json" \
  --output table

echo ""
echo "Infrastructure deployment complete."
echo "Blob URL for frontend:"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query "properties.outputs.signalsBlobUrl.value" \
  --output tsv
