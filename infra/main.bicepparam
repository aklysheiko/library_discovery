using 'main.bicep'

// ⚠️  Change appName to something globally unique (lowercase, hyphens OK, max 17 chars).
//     It becomes the prefix for: <appName>-api.azurewebsites.net and <appName>-frontend.*
param appName = 'libdiscovery'

param location = 'eastus'
param staticWebAppLocation = 'eastus2'

param geminiModel = 'models/gemini-2.5-flash'
param geminiBaseUrl = 'https://generativelanguage.googleapis.com/v1beta'

// geminiApiKey and allowedOrigin are injected at deploy time by the CI/CD workflow.
