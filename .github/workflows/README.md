# DocumentService GitHub Actions Workflows

## Overview

DocumentService uses GitHub Actions for CI/CD automation with a **build-on-push, deploy-on-schedule** pattern.

## Naming Convention

All workflows follow the pattern: `<category>-<action>-<target>.yml`

| Prefix | Category | Purpose |
|--------|----------|---------|
| `ci-` | Continuous Integration | Build, test |
| `cd-` | Continuous Deployment | Deploy, release |
| `compliance-` | Quality Standards | Tech stack checks |
| `utility-` | Automation Helpers | Auto-rebase |

## Active Workflows

### CI Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci-build-test.yml` | Push/PR to main/develop | Build and test .NET solution |

### CD Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `cd-deploy-docservice-scheduled.yml` | Daily 2:15 AM CST | Auto-deploy if new image |
| `cd-deploy-docservice-manual.yml` | Manual | On-demand deployment |
| `cd-release.yml` | Tag `v*` | Create GitHub release |

### Compliance Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `compliance-tech-stack.yml` | PR/push to main | Verify C#-only backend |

### Utility Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `utility-auto-rebase.yml` | Push to main | Auto-rebase open PRs |

## How to Deploy Immediately

### Option 1: GitHub Actions UI
1. Go to **Actions** → **"Manual Deploy DocumentService"**
2. Click **"Run workflow"**
3. Configure options (image_tag, gradual_rollout, traffic_percentage, work_item_id)
4. Click **"Run workflow"**

### Option 2: GitHub CLI
```bash
gh workflow run cd-deploy-docservice-manual.yml --repo jasonberkes/TaskMaster.DocumentService
```

### Option 3: Direct Script
```bash
cd ~/Dropbox/Dev/TaskMaster.DocumentService
./scripts/deploy-bluegreen.sh <image-tag>

# With options
./scripts/deploy-bluegreen.sh d84cb6a --gradual
./scripts/deploy-bluegreen.sh d84cb6a --work-item 3539
```

## Blue-Green Deployment Pattern

See `scripts/README.md` for full deployment script documentation.

**Deployment Flow:**
1. Deploy new revision (0% traffic)
2. Wait for healthy
3. Health check /health endpoint
4. Switch traffic (direct or gradual)
5. Connection drain (30 seconds)
6. Cleanup old revisions (keep 3)
7. Log to Platform API

## Deployment Schedule

| App | Time (CST) | Workflow |
|-----|------------|----------|
| DocumentService | 2:15 AM | cd-deploy-docservice-scheduled.yml |
| Platform | 2:00 AM | cd-deploy-platform-scheduled.yml |

## Environment

- **Resource Group:** tm-rg-prod-eus2
- **Container App:** tm-documentservice-prod-eus2
- **ACR:** tmcrprodeus2.azurecr.io/documentservice-api
- **URL:** https://tm-documentservice-prod-eus2.thankfulsand-8986c25c.eastus2.azurecontainerapps.io

---

**Last Updated:** December 13, 2025
**Maintained By:** Jason Berkes / Super Easy Software
