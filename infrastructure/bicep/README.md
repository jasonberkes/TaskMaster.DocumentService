# TaskMaster Document Service - Azure Infrastructure

This directory contains Bicep templates for deploying the TaskMaster Document Service to Azure Container Apps.

## Architecture

The deployment includes:

- **Azure Container Apps**: Hosts the Document Service API with auto-scaling
- **Azure SQL Database**: Stores document metadata and relationships
- **Azure Blob Storage**: Stores document files
- **Log Analytics Workspace**: Centralized logging and monitoring
- **Application Insights**: Application performance monitoring and telemetry
- **Azure Container Registry**: Container image storage (must exist before deployment)

## Prerequisites

1. Azure CLI installed and authenticated
2. Azure subscription with appropriate permissions
3. Existing Azure Container Registry
4. Azure Key Vault with SQL admin credentials stored as secrets

## Configuration

### Update Parameter Files

Before deployment, update the parameter files (`main.parameters.dev.json`, `main.parameters.prod.json`) with your values:

1. Replace `CONTAINER_REGISTRY_NAME` with your Azure Container Registry name
2. Replace `SUBSCRIPTION_ID` with your Azure subscription ID
3. Replace `RESOURCE_GROUP` with your Key Vault resource group
4. Replace `KEY_VAULT_NAME` with your Azure Key Vault name

### Key Vault Secrets

Ensure the following secrets exist in your Key Vault:

- `sql-admin-login`: SQL Server administrator username
- `sql-admin-password`: SQL Server administrator password (strong password required)

## Deployment

### 1. Create Resource Group

```bash
az group create \
  --name rg-taskmaster-document-service-dev \
  --location eastus
```

### 2. Deploy Infrastructure

#### Development Environment

```bash
az deployment group create \
  --resource-group rg-taskmaster-document-service-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json
```

#### Production Environment

```bash
az deployment group create \
  --resource-group rg-taskmaster-document-service-prod \
  --template-file main.bicep \
  --parameters main.parameters.prod.json
```

### 3. Verify Deployment

```bash
# Get the Container App URL
az deployment group show \
  --resource-group rg-taskmaster-document-service-dev \
  --name main \
  --query properties.outputs.containerAppUrl.value \
  --output tsv
```

## Resource Naming Convention

Resources are named using the pattern: `{serviceName}-{resourceType}-{uniqueSuffix}`

- `serviceName`: `taskmaster-document-service`
- `resourceType`: `sql`, `logs`, `env`, `appinsights`, etc.
- `uniqueSuffix`: Generated from resource group ID to ensure global uniqueness

## Security Features

1. **Managed Identity**: Container App uses System-Assigned Managed Identity
2. **RBAC**: Least-privilege access using role assignments
   - AcrPull for Container Registry access
   - Storage Blob Data Contributor for Blob Storage
3. **TLS**: All traffic encrypted with TLS 1.2+
4. **Private Secrets**: SQL and Storage credentials stored as Container App secrets
5. **Key Vault Integration**: Sensitive parameters retrieved from Key Vault
6. **Firewall**: SQL Server configured to allow Azure services only

## Scaling Configuration

### Development
- Min Replicas: 1
- Max Replicas: 5
- CPU: 0.5 cores
- Memory: 1Gi

### Production
- Min Replicas: 2
- Max Replicas: 10
- CPU: 1.0 cores
- Memory: 2Gi

Auto-scaling triggers on:
- HTTP concurrent requests (50 per instance)

## Monitoring

### Application Insights

Access Application Insights for:
- Performance metrics
- Request tracking
- Exception monitoring
- Custom telemetry

### Log Analytics

Query logs using Kusto Query Language (KQL):

```kql
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "taskmaster-document-service-dev"
| order by TimeGenerated desc
| limit 100
```

## Health Checks

The Container App includes:
- **Liveness Probe**: `/health` endpoint every 10s
- **Readiness Probe**: `/health` endpoint every 10s

## Database Migrations

Run Entity Framework Core migrations after deployment:

```bash
# Get SQL Server FQDN
SQL_SERVER=$(az deployment group show \
  --resource-group rg-taskmaster-document-service-dev \
  --name main \
  --query properties.outputs.sqlServerFqdn.value \
  --output tsv)

# Update connection string in appsettings and run migrations
dotnet ef database update --project src/TaskMaster.DocumentService.Data
```

## Troubleshooting

### View Container Logs

```bash
az containerapp logs show \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev \
  --follow
```

### Check Container App Status

```bash
az containerapp show \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev \
  --query properties.runningStatus
```

### Restart Container App

```bash
az containerapp revision restart \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev
```

## Cost Optimization

- **Development**: Uses Basic SQL SKU and minimal replicas
- **Production**: Uses S1 SQL SKU with higher availability
- **Storage**: Standard LRS with 7-day soft delete
- **Container Apps**: Consumption-based pricing with auto-scaling

## Clean Up

To delete all resources:

```bash
az group delete \
  --name rg-taskmaster-document-service-dev \
  --yes --no-wait
```

## Support

For issues or questions, please refer to:
- Azure Container Apps documentation
- TaskMaster project documentation
- Internal DevOps team
