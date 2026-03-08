#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SERVER_DIR="$(dirname "$SCRIPT_DIR")"
PUBLISH_DIR="$SERVER_DIR/publish"
PROJECT_DIR="$SERVER_DIR/SignalEngine.Functions"

RESOURCE_GROUP="${1:?Usage: deploy-app.sh <resource-group>}"

echo "=== Getting function app name ==="
FUNC_APP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query "properties.outputs.functionAppName.value" \
  --output tsv)

echo "Function App: $FUNC_APP"

echo ""
echo "=== Publishing project ==="
dotnet publish "$PROJECT_DIR/SignalEngine.Functions.csproj" -c Release -o "$PUBLISH_DIR"

echo ""
echo "=== Deploying to Azure ==="
cd "$PUBLISH_DIR"
zip -r ../deploy.zip .
az functionapp deployment source config-zip \
  --resource-group "$RESOURCE_GROUP" \
  --name "$FUNC_APP" \
  --src "$SERVER_DIR/deploy.zip"

echo ""
echo "Deployment complete: https://$FUNC_APP.azurewebsites.net"
