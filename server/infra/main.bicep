@description('Base name for all resources')
param baseName string = 'signalengine'

@description('Azure region')
param location string = resourceGroup().location

@description('OpenAI API key')
@secure()
param openAiApiKey string

@description('OpenAI model name')
param openAiModel string = 'gpt-4o-mini'

@description('X.com bearer token (optional)')
@secure()
param xBearerToken string = ''

@description('Eleven Labs API key')
@secure()
param elevenLabsApiKey string

@description('Eleven Labs voice key')
@secure()
param elevenLabsVoiceKey string

@description('Signal container name (public read access for frontend)')
param signalContainerName string = 'signals'

var uniqueSuffix = uniqueString(resourceGroup().id, baseName)
var safeBaseName = replace(replace(baseName, '-', ''), '_', '')
var storageAccountName = '${take(safeBaseName, 10)}${take(uniqueSuffix, 14)}'
var functionAppName = '${baseName}-func-${take(uniqueSuffix, 6)}'
var appServicePlanName = '${baseName}-plan'
var appInsightsName = '${baseName}-insights'

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
  }
}

// Blob container for signals (public read access for frontend)
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource signalContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: signalContainerName
  properties: {
    publicAccess: 'Blob'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

// Consumption App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'OPENAI_API_KEY'
          value: openAiApiKey
        }
        {
          name: 'OPENAI_MODEL'
          value: openAiModel
        }
        {
          name: 'X_BEARER_TOKEN'
          value: xBearerToken
        }
        {
          name: 'SIGNAL_CONTAINER'
          value: signalContainerName
        }
        {
          name: 'ELEVEN_LABS_API_KEY'
          value: elevenLabsApiKey
        }
        {
          name: 'ELEVEN_LABS_VOICE_KEY'
          value: elevenLabsVoiceKey
        }
      ]
    }
    httpsOnly: true
  }
}

// Outputs
output functionAppName string = functionApp.name
output storageAccountName string = storageAccount.name
output signalsBlobUrl string = 'https://${storageAccount.name}.blob.${environment().suffixes.storage}/${signalContainerName}'
output appInsightsName string = appInsights.name
