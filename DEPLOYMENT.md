# TaskMaster Document Service - Deployment Guide

This guide provides instructions for deploying the TaskMaster Document Service to Azure Container Apps using the provided infrastructure templates and GitHub Actions workflows.

## Overview

The deployment process includes:

1. **Continuous Integration**: Build, test, and create Docker image
2. **Infrastructure as Code**: Bicep templates for Azure resources
3. **Continuous Deployment**: Automated deployment to Azure Container Apps
4. **Health Monitoring**: Health checks and Application Insights

## Prerequisites

### Azure Resources

1. **Azure Subscription**: Active subscription with appropriate permissions
2. **Azure Container Registry**: For storing Docker images
3. **Azure Key Vault**: For storing sensitive credentials
4. **Resource Groups**: Separate groups for each environment (dev, staging, prod)

### Required Tools

- Azure CLI (v2.50+)
- .NET 9 SDK
- Docker Desktop (for local testing)
- Git

## GitHub Secrets Configuration

Configure the following secrets in your GitHub repository settings:

### Container Registry Secrets

- `AZURE_CONTAINER_REGISTRY_NAME`: Name of your Azure Container Registry (without .azurecr.io)
- `AZURE_CONTAINER_REGISTRY_USERNAME`: ACR admin username
- `AZURE_CONTAINER_REGISTRY_PASSWORD`: ACR admin password

### Development Environment

- `AZURE_CREDENTIALS_DEV`: Azure service principal credentials (JSON format)
- `AZURE_RESOURCE_GROUP_DEV`: Resource group name for dev environment
- `SQL_ADMIN_LOGIN_DEV`: SQL Server admin username
- `SQL_ADMIN_PASSWORD_DEV`: SQL Server admin password (strong password required)

### Staging Environment

- `AZURE_CREDENTIALS_STAGING`: Azure service principal credentials (JSON format)
- `AZURE_RESOURCE_GROUP_STAGING`: Resource group name for staging environment
- `SQL_ADMIN_LOGIN_STAGING`: SQL Server admin username
- `SQL_ADMIN_PASSWORD_STAGING`: SQL Server admin password

### Production Environment

- `AZURE_CREDENTIALS_PROD`: Azure service principal credentials (JSON format)
- `AZURE_RESOURCE_GROUP_PROD`: Resource group name for production environment
- `SQL_ADMIN_LOGIN_PROD`: SQL Server admin username
- `SQL_ADMIN_PASSWORD_PROD`: SQL Server admin password

### Creating Azure Credentials

Create a service principal for GitHub Actions:

```bash
az ad sp create-for-rbac \
  --name "github-actions-document-service" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group-name} \
  --sdk-auth
```

Store the entire JSON output as the `AZURE_CREDENTIALS_*` secret.

## Initial Setup

### 1. Create Resource Groups

```bash
# Development
az group create \
  --name rg-taskmaster-document-service-dev \
  --location eastus

# Staging
az group create \
  --name rg-taskmaster-document-service-staging \
  --location eastus

# Production
az group create \
  --name rg-taskmaster-document-service-prod \
  --location eastus
```

### 2. Create Container Registry (if not exists)

```bash
az acr create \
  --name <your-registry-name> \
  --resource-group rg-shared-services \
  --sku Standard \
  --admin-enabled true
```

### 3. Update Bicep Parameters

Edit the parameter files in `infrastructure/bicep/`:

- `main.parameters.dev.json`
- `main.parameters.prod.json`

Update the following placeholders:
- `CONTAINER_REGISTRY_NAME`: Your ACR name
- `SUBSCRIPTION_ID`: Your Azure subscription ID
- `RESOURCE_GROUP`: Your Key Vault resource group
- `KEY_VAULT_NAME`: Your Key Vault name (if using Key Vault for secrets)

## Deployment Workflows

### Automatic Deployment (CI/CD)

Pushes to the `main` branch automatically trigger:

1. **Build and Test**: Compile code, run tests
2. **Docker Build**: Create container image and push to ACR
3. **Deploy to Dev**: Deploy to development environment

### Manual Deployment

Trigger deployments manually via GitHub Actions:

1. Go to **Actions** tab in GitHub
2. Select **Deploy to Azure Container Apps** workflow
3. Click **Run workflow**
4. Choose environment (dev, staging, prod)
5. Click **Run workflow**

## Local Testing

### Build Docker Image Locally

```bash
# From repository root
docker build -f src/TaskMaster.DocumentService.Api/Dockerfile -t taskmaster-document-service:local .
```

### Run Container Locally

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Server=localhost;Database=DocumentServiceDB;Trusted_Connection=True;" \
  -e ConnectionStrings__BlobStorage="UseDevelopmentStorage=true" \
  taskmaster-document-service:local
```

### Test Health Endpoint

```bash
curl http://localhost:8080/health
```

## Post-Deployment Tasks

### 1. Verify Deployment

```bash
# Get Container App URL
az deployment group show \
  --resource-group rg-taskmaster-document-service-dev \
  --name main \
  --query properties.outputs.containerAppUrl.value \
  --output tsv

# Test health endpoint
curl https://<container-app-url>/health
```

### 2. Run Database Migrations

```bash
# Set connection string environment variable
export ConnectionStrings__DefaultConnection="Server=<sql-server>.database.windows.net;Database=DocumentServiceDB;User ID=<username>;Password=<password>;"

# Run migrations
cd src/TaskMaster.DocumentService.Data
dotnet ef database update
```

### 3. Monitor Application

#### Application Insights

Access metrics and logs:

```bash
# Get Application Insights instrumentation key
az deployment group show \
  --resource-group rg-taskmaster-document-service-dev \
  --name main \
  --query properties.outputs.appInsightsInstrumentationKey.value
```

#### Container Logs

```bash
# Stream logs
az containerapp logs show \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev \
  --follow
```

## Scaling Configuration

### Auto-scaling

The Container App automatically scales based on:

- **HTTP concurrent requests**: 50 requests per instance
- **Min replicas**: 1 (dev), 2 (prod)
- **Max replicas**: 5 (dev), 10 (prod)

### Manual Scaling

```bash
# Update replica count
az containerapp update \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev \
  --min-replicas 2 \
  --max-replicas 5
```

## Troubleshooting

### Build Failures

1. Check GitHub Actions logs for specific errors
2. Verify all NuGet packages are available
3. Ensure .NET SDK version matches (9.0)

### Deployment Failures

1. Verify Azure credentials are valid
2. Check resource group exists
3. Ensure Container Registry is accessible
4. Review Bicep template validation errors

### Runtime Issues

1. Check Container App logs
2. Verify environment variables are set correctly
3. Test database connectivity
4. Verify Blob Storage connection string

### Health Check Failures

```bash
# Check health endpoint directly
curl https://<container-app-url>/health

# View health check logs
az containerapp logs show \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev \
  --type console
```

## Rollback Procedures

### Rollback to Previous Revision

```bash
# List revisions
az containerapp revision list \
  --name taskmaster-document-service-dev \
  --resource-group rg-taskmaster-document-service-dev

# Activate previous revision
az containerapp revision activate \
  --name <revision-name> \
  --resource-group rg-taskmaster-document-service-dev
```

### Rollback via GitHub Actions

1. Navigate to the successful previous workflow run
2. Click **Re-run all jobs**
3. This will redeploy the previous version

## Security Best Practices

1. **Managed Identity**: Use system-assigned managed identity for Azure resource access
2. **Key Vault**: Store sensitive values in Azure Key Vault
3. **Network Security**: Use private endpoints for production environments
4. **RBAC**: Apply least-privilege access control
5. **TLS**: All traffic encrypted with TLS 1.2+
6. **Secrets**: Never commit secrets to source control

## Cost Management

### Development
- Basic SQL Database: ~$5/month
- Container Apps: Consumption-based
- Storage: Standard LRS ~$1/month
- **Estimated**: $10-20/month

### Production
- S1 SQL Database: ~$30/month
- Container Apps: Consumption-based with higher limits
- Storage: Standard LRS with replication
- **Estimated**: $100-200/month (varies with usage)

### Cost Optimization Tips

1. Scale down non-production environments after hours
2. Use auto-pause for SQL Database in dev
3. Enable container app scale-to-zero for dev environments
4. Set appropriate retention policies for logs

## Maintenance

### Regular Tasks

1. **Weekly**: Review Application Insights for errors
2. **Monthly**: Update container images with security patches
3. **Quarterly**: Review and optimize resource sizing
4. **As Needed**: Update Bicep templates and workflows

### Updating the Service

1. Make code changes on feature branch
2. Create pull request to main branch
3. Merge after review and tests pass
4. Automatic deployment to dev occurs
5. Manual promotion to staging/prod

## Support and Documentation

- **Azure Container Apps**: https://learn.microsoft.com/azure/container-apps/
- **Bicep**: https://learn.microsoft.com/azure/azure-resource-manager/bicep/
- **GitHub Actions**: https://docs.github.com/actions
- **TaskMaster Project**: Internal documentation

For issues or questions, contact the DevOps team or open an issue in the project repository.
