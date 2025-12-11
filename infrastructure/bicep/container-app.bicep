// =============================================================================
// DocumentService Container App - Uses Shared Infrastructure
// =============================================================================
// Deploys DocumentService API to existing tm-cae-prod-eus2 environment
// Uses existing SQL Server, Storage Account, and Meilisearch
// Supports blue/green deployment via traffic splitting
// =============================================================================

targetScope = 'resourceGroup'

// -----------------------------------------------------------------------------
// Parameters
// -----------------------------------------------------------------------------

@description('Environment suffix (e.g., prod, staging)')
param environment string = 'prod'

@description('Azure region')
param location string = resourceGroup().location

@description('Container image with tag')
param containerImage string

@description('Revision suffix for blue/green deployment')
param revisionSuffix string = ''

@description('Traffic weight for new revision (0-100)')
@minValue(0)
@maxValue(100)
param newRevisionTrafficWeight int = 100

@description('Minimum replicas')
@minValue(0)
@maxValue(30)
param minReplicas int = 1

@description('Maximum replicas')
@minValue(1)
@maxValue(30)
param maxReplicas int = 5

@description('CPU cores')
param cpu string = '0.5'

@description('Memory')
param memory string = '1Gi'

// -----------------------------------------------------------------------------
// Existing Resources (Shared Infrastructure)
// -----------------------------------------------------------------------------

// Existing Container Apps Environment
resource containerEnv 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: 'tm-cae-prod-eus2'
}

// Existing Container Registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: 'tmcrprodeus2'
}

// Existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: 'tm-kv-prod-eus2'
}

// -----------------------------------------------------------------------------
// Container App
// -----------------------------------------------------------------------------

resource documentServiceApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'tm-documentservice-${environment}-eus2'
  location: location
  tags: {
    Environment: environment
    Service: 'DocumentService'
    ManagedBy: 'Bicep'
    Project: 'TaskMaster'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      activeRevisionsMode: 'Multiple'  // Enable blue/green
      maxInactiveRevisions: 3
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        traffic: newRevisionTrafficWeight == 100 ? [
          {
            latestRevision: true
            weight: 100
          }
        ] : [
          {
            latestRevision: true
            weight: newRevisionTrafficWeight
          }
          {
            latestRevision: false
            weight: 100 - newRevisionTrafficWeight
          }
        ]
        corsPolicy: {
          allowedOrigins: [
            'https://taskmaster-platform.thankfulsand-8986c25c.eastus2.azurecontainerapps.io'
            'https://localhost:5001'
          ]
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
          allowCredentials: true
        }
      }
      registries: [
        {
          server: '${acr.name}.azurecr.io'
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'sql-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/SQL-CONNECTION-STRING'
          identity: 'system'
        }
        {
          name: 'blob-connection-string'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/BLOB-CONNECTION-STRING'
          identity: 'system'
        }
        {
          name: 'meilisearch-api-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/MEILISEARCH-API-KEY'
          identity: 'system'
        }
        {
          name: 'documentservice-api-key'
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/DOCUMENTSERVICE-API-KEY'
          identity: 'system'
        }
      ]
    }
    template: {
      revisionSuffix: empty(revisionSuffix) ? null : revisionSuffix
      containers: [
        {
          name: 'documentservice-api'
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ConnectionStrings__BlobStorage'
              secretRef: 'blob-connection-string'
            }
            {
              name: 'BlobStorage__ContainerName'
              value: 'taskmaster-documents'
            }
            {
              name: 'Meilisearch__Url'
              value: 'http://tm-meilisearch-prod-eus2'  // Internal service name
            }
            {
              name: 'Meilisearch__ApiKey'
              secretRef: 'meilisearch-api-key'
            }
            {
              name: 'Authentication__ApiKey'
              secretRef: 'documentservice-api-key'
            }
            {
              name: 'Logging__LogLevel__Default'
              value: 'Information'
            }
            {
              name: 'Logging__LogLevel__Microsoft.AspNetCore'
              value: 'Warning'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// -----------------------------------------------------------------------------
// Role Assignments
// -----------------------------------------------------------------------------

// ACR Pull permission
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, documentServiceApp.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: documentServiceApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault Secrets User permission
resource keyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, documentServiceApp.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: documentServiceApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// -----------------------------------------------------------------------------
// Outputs
// -----------------------------------------------------------------------------

output containerAppName string = documentServiceApp.name
output containerAppFqdn string = documentServiceApp.properties.configuration.ingress.fqdn
output containerAppUrl string = 'https://${documentServiceApp.properties.configuration.ingress.fqdn}'
output latestRevisionName string = documentServiceApp.properties.latestRevisionName
output principalId string = documentServiceApp.identity.principalId
