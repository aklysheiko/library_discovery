targetScope = 'resourceGroup'

@description('Base name for all resources (lowercase, hyphens OK, max 17 chars).')
@maxLength(17)
param appName string

@description('Azure region for Container Apps resources.')
param location string = resourceGroup().location

@description('Azure region for Static Web App.')
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

@description('Gemini API Key (optional).')
@secure()
param geminiApiKey string = ''

@description('Gemini model name.')
param geminiModel string = 'models/gemini-2.5-flash'

@description('Gemini base URL.')
param geminiBaseUrl string = 'https://generativelanguage.googleapis.com/v1beta'

@description('Allowed CORS origin (set to Static Web App URL by CI/CD).')
param allowedOrigin string = '*'

@description('Full container image reference e.g. ghcr.io/user/repo:sha.')
param containerImage string

@description('Container registry server (e.g. ghcr.io).')
param registryServer string = 'ghcr.io'

@description('Container registry username.')
param registryUsername string

@description('Container registry password / token.')
@secure()
param registryPassword string

// ---------------------------------------------------------------------------
// Log Analytics — required by Container Apps Environment
// ---------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ---------------------------------------------------------------------------
// Container Apps Environment — Consumption (serverless, scale to zero)
// ---------------------------------------------------------------------------
resource containerEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${appName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ---------------------------------------------------------------------------
// Container App — .NET 8 API
// ---------------------------------------------------------------------------
resource api 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${appName}-api'
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        {
          name: 'registry-password'
          value: registryPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
            { name: 'GEMINI_API_KEY', value: geminiApiKey }
            { name: 'GEMINI_MODEL', value: geminiModel }
            { name: 'GEMINI_BASE_URL', value: geminiBaseUrl }
            { name: 'AllowedOrigins__0', value: allowedOrigin }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
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
// Outputs
// ---------------------------------------------------------------------------
output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output staticWebAppUrl string = 'https://${swa.properties.defaultHostname}'
output staticWebAppHostname string = swa.properties.defaultHostname
