targetScope = 'resourceGroup'

@description('Base name for all resources (lowercase letters and hyphens only, max 17 chars). Must be globally unique.')
@maxLength(17)
param appName string

@description('Azure region for App Service resources.')
param location string = resourceGroup().location

@description('Azure region for Static Web App (limited availability).')
@allowed([
  'eastus2'
  'centralus'
  'westus2'
  'westeurope'
  'eastasia'
  'eastus'
  'westus'
])
param staticWebAppLocation string = 'eastus2'

@description('Gemini API Key. Leave empty to use the heuristic fallback parser.')
@secure()
param geminiApiKey string = ''

@description('Gemini model name.')
param geminiModel string = 'models/gemini-2.5-flash'

@description('Gemini base URL.')
param geminiBaseUrl string = 'https://generativelanguage.googleapis.com/v1beta'

@description('Allowed CORS origin for the API (Static Web App URL). Set automatically by CI/CD after first deploy.')
param allowedOrigin string = '*'

// ---------------------------------------------------------------------------
// App Service Plan — F1 (Free)
// ---------------------------------------------------------------------------
resource plan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {}
}

// ---------------------------------------------------------------------------
// App Service — .NET 8 API
// ---------------------------------------------------------------------------
resource api 'Microsoft.Web/sites@2023-01-01' = {
  name: '${appName}-api'
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'GEMINI_API_KEY', value: geminiApiKey }
        { name: 'GEMINI_MODEL', value: geminiModel }
        { name: 'GEMINI_BASE_URL', value: geminiBaseUrl }
        { name: 'AllowedOrigins__0', value: allowedOrigin }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Static Web App — React frontend (Free tier)
// ---------------------------------------------------------------------------
resource swa 'Microsoft.Web/staticSites@2023-01-01' = {
  name: '${appName}-frontend'
  location: staticWebAppLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

// ---------------------------------------------------------------------------
// Outputs (consumed by CI/CD workflow)
// ---------------------------------------------------------------------------
output apiUrl string = 'https://${api.properties.defaultHostName}'
output staticWebAppUrl string = 'https://${swa.properties.defaultHostname}'
output staticWebAppHostname string = swa.properties.defaultHostname
