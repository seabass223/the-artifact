#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

RESOURCE_GROUP="${1:?Usage: deploy-frontend.sh <resource-group>}"

echo "=== Getting function app name ==="
FUNC_APP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query "properties.outputs.functionAppName.value" \
  --output tsv)

ORCHESTRATE_ENDPOINT="https://$FUNC_APP.azurewebsites.net/api/orchestrate/flow1"
echo "Orchestrate endpoint: $ORCHESTRATE_ENDPOINT"

echo ""
echo "=== Getting storage account name ==="
STORAGE_ACCOUNT=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name main \
  --query "properties.outputs.storageAccountName.value" \
  --output tsv)

echo "Storage Account: $STORAGE_ACCOUNT"

echo ""
echo "=== Getting storage account key ==="
STORAGE_KEY=$(az storage account keys list \
  --resource-group "$RESOURCE_GROUP" \
  --account-name "$STORAGE_ACCOUNT" \
  --query "[0].value" \
  --output tsv)

echo ""
echo "=== Enabling static website hosting ==="
az storage blob service-properties update \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --static-website \
  --index-document index.html \
  --output none

echo ""
echo "=== Uploading frontend files ==="

az storage blob upload \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --container-name '$web' \
  --file "$ROOT_DIR/index.html" \
  --name "index.html" \
  --overwrite \
  --output none

az storage blob upload-batch \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --destination '$web' \
  --source "$ROOT_DIR/css" \
  --destination-path "css" \
  --pattern "*.css" \
  --overwrite \
  --output none

SETTINGS_TMP=$(mktemp /tmp/settings.XXXXXX.js)
sed "s|ORCHESTRATE_ENDPOINT:.*|ORCHESTRATE_ENDPOINT: '$ORCHESTRATE_ENDPOINT',|" \
  "$ROOT_DIR/js/settings.js" > "$SETTINGS_TMP"

az storage blob upload \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --container-name '$web' \
  --file "$SETTINGS_TMP" \
  --name "js/settings.js" \
  --overwrite \
  --output none
rm "$SETTINGS_TMP"

for f in "$ROOT_DIR/js/"*.js; do
  name=$(basename "$f")
  [ "$name" = "settings.js" ] && continue
  az storage blob upload \
    --account-name "$STORAGE_ACCOUNT" \
    --account-key "$STORAGE_KEY" \
    --container-name '$web' \
    --file "$f" \
    --name "js/$name" \
    --overwrite \
    --output none
done

PNG_FILES=(og.png icon.png)
for png in "${PNG_FILES[@]}"; do
  az storage blob upload \
    --account-name "$STORAGE_ACCOUNT" \
    --account-key "$STORAGE_KEY" \
    --container-name '$web' \
    --file "$ROOT_DIR/images/$png" \
    --name "images/$png" \
    --overwrite \
    --output none
done

az storage blob upload-batch \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --destination '$web' \
  --source "$ROOT_DIR/images" \
  --destination-path "images" \
  --pattern "*.ico" \
  --overwrite \
  --output none

az storage blob upload-batch \
  --account-name "$STORAGE_ACCOUNT" \
  --account-key "$STORAGE_KEY" \
  --destination '$web' \
  --source "$ROOT_DIR/images" \
  --destination-path "images" \
  --pattern "*.jpg" \
  --overwrite \
  --output none

echo ""
echo "=== Deployment complete ==="
az storage account show \
  --name "$STORAGE_ACCOUNT" \
  --query "primaryEndpoints.web" \
  --output tsv
