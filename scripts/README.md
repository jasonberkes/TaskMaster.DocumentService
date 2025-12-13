# DocumentService Scripts

## Overview

Scripts for managing DocumentService deployments and operations.

## Scripts

### deploy-bluegreen.sh

Blue-green deployment script for zero-downtime deployments to Azure Container Apps.

**Usage:**
```bash
./scripts/deploy-bluegreen.sh <image-tag> [options]
```

**Options:**
| Option | Description |
|--------|-------------|
| `--gradual` | Enable gradual rollout (10% → 50% → 100%) |
| `--skip-logging` | Skip deployment logging to Platform API |
| `--work-item <id>` | Associate deployment with WorkItem |
| `--traffic <percent>` | Final traffic percentage (default: 100) |

**Examples:**
```bash
# Standard deployment
./scripts/deploy-bluegreen.sh d84cb6a

# Gradual rollout for high-risk changes
./scripts/deploy-bluegreen.sh d84cb6a --gradual

# Associate with WorkItem
./scripts/deploy-bluegreen.sh d84cb6a --work-item 3539

# Canary deployment (10% traffic)
./scripts/deploy-bluegreen.sh d84cb6a --traffic 10
```

**Deployment Flow:**
1. Deploy new revision with 0% traffic
2. Wait for revision to be healthy
3. HTTP health check against /health
4. Switch traffic to new revision
5. 30-second connection drain
6. Cleanup old revisions (keep 3 most recent)
7. Log deployment to Platform API

**Rollback:**
```bash
az containerapp ingress traffic set \
  --name tm-documentservice-prod-eus2 \
  --resource-group tm-rg-prod-eus2 \
  --revision-weight <old-revision>=100
```

## GitHub Workflows

| Workflow | Trigger | Description |
|----------|---------|-------------|
| `cd-deploy-docservice-scheduled.yml` | Daily 2:15 AM CST | Auto-deploy if new image exists |
| `cd-deploy-docservice-manual.yml` | Manual | On-demand deployment |
| `ci-build-test.yml` | Push to main/develop | Build and test only |
| `cd-release.yml` | Tag v* | Create GitHub release |
| `compliance-tech-stack.yml` | PR/push to main | Tech stack validation |
| `utility-auto-rebase.yml` | Push to main | Auto-rebase open PRs |

## Environment

- **Resource Group:** tm-rg-prod-eus2
- **Container App:** tm-documentservice-prod-eus2
- **ACR:** tmcrprodeus2.azurecr.io/documentservice-api
- **URL:** https://tm-documentservice-prod-eus2.thankfulsand-8986c25c.eastus2.azurecontainerapps.io
