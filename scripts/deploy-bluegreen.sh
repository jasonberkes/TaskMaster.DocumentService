#!/bin/bash
# ============================================================================
# Blue-Green Deployment Script for DocumentService
# ============================================================================
# Automated zero-downtime deployments with health checks and deployment logging.
#
# FEATURES:
# - Deploy new revision with 0% traffic
# - Wait for revision to be healthy
# - Health check new revision
# - Traffic shift (direct or gradual)
# - Connection drain before deactivation
# - Log deployment to TaskMaster Platform API
# - Cleanup old revisions (keep 3 most recent)
# - Rollback on failure
#
# USAGE:
#   ./scripts/deploy-bluegreen.sh <image-tag> [options]
#
# OPTIONS:
#   --gradual              Enable gradual rollout (10% → 50% → 100%)
#   --skip-logging         Skip deployment logging to Platform API
#   --work-item <id>       Associate deployment with work item
#   --traffic <percent>    Final traffic percentage (default: 100)
#
# EXAMPLES:
#   ./scripts/deploy-bluegreen.sh d84cb6a
#   ./scripts/deploy-bluegreen.sh d84cb6a --gradual
#   ./scripts/deploy-bluegreen.sh d84cb6a --work-item 3539
# ============================================================================

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

# Azure Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-tm-rg-prod-eus2}"
CONTAINER_APP_NAME="${CONTAINER_APP_NAME:-tm-documentservice-prod-eus2}"
ACR_REGISTRY="${ACR_REGISTRY:-tmcrprodeus2.azurecr.io}"
REPOSITORY_NAME="${REPOSITORY_NAME:-documentservice-api}"

# Deployment Configuration
HEALTH_CHECK_RETRIES=12
HEALTH_CHECK_INTERVAL=10
CONNECTION_DRAIN_WAIT=30
GRADUAL_ROLLOUT=false
TRAFFIC_SHIFT_WAIT=120
REVISIONS_TO_KEEP=3
SKIP_LOGGING=false

# API Configuration
APP_URL="${APP_URL:-https://tm-documentservice-prod-eus2.thankfulsand-8986c25c.eastus2.azurecontainerapps.io}"
PLATFORM_API_URL="${PLATFORM_API_URL:-https://taskmaster-platform.thankfulsand-8986c25c.eastus2.azurecontainerapps.io}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

print_header() {
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "$1"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    command -v az &> /dev/null || { log_error "Azure CLI not found"; exit 1; }
    command -v curl &> /dev/null || { log_error "curl not found"; exit 1; }
    command -v jq &> /dev/null || { log_error "jq not found"; exit 1; }
    az account show &> /dev/null || { log_error "Not authenticated with Azure"; exit 1; }
    
    log_success "Prerequisites met"
}

get_current_revision() {
    az containerapp ingress traffic show \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "[?weight > 0].revisionName | [0]" -o tsv 2>/dev/null || echo ""
}

get_newest_revision() {
    az containerapp revision list \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "sort_by(@, &properties.createdTime) | reverse(@) | [0].name" -o tsv 2>/dev/null || echo ""
}

wait_for_revision_healthy() {
    local revision=$1
    local max_wait=120
    local waited=0
    
    log_info "Waiting for revision to be healthy..."
    
    while [ $waited -lt $max_wait ]; do
        local health=$(az containerapp revision show \
            --name "$CONTAINER_APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision "$revision" \
            --query "properties.healthState" -o tsv 2>/dev/null || echo "Unknown")
        
        local running=$(az containerapp revision show \
            --name "$CONTAINER_APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision "$revision" \
            --query "properties.runningState" -o tsv 2>/dev/null || echo "Unknown")
        
        log_info "  Health: $health, Running: $running"
        
        if [ "$health" = "Healthy" ] && [ "$running" = "Running" ]; then
            log_success "Revision is healthy!"
            return 0
        fi
        
        if [ "$health" = "Unhealthy" ] || [ "$running" = "Failed" ]; then
            log_error "Revision failed to start"
            return 1
        fi
        
        sleep 10
        waited=$((waited + 10))
    done
    
    log_error "Timeout waiting for revision"
    return 1
}

health_check() {
    local url=$1
    local max_retries=${2:-$HEALTH_CHECK_RETRIES}
    
    log_info "Running health check against: $url"
    
    for i in $(seq 1 $max_retries); do
        if curl -sf --max-time 10 "$url" > /dev/null 2>&1; then
            log_success "Health check passed!"
            return 0
        fi
        
        log_warning "Attempt $i/$max_retries failed, retrying..."
        sleep $HEALTH_CHECK_INTERVAL
    done
    
    log_error "Health check failed after $max_retries attempts"
    return 1
}

set_traffic() {
    local revision=$1
    local weight=$2
    
    log_info "Setting traffic: $revision = $weight%"
    
    az containerapp ingress traffic set \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --revision-weight "$revision=$weight" \
        --output none 2>/dev/null
    
    log_success "Traffic updated"
}

log_deployment() {
    local image_tag=$1
    local status=$2
    local health_status=$3
    local notes=$4
    local work_item_id=${5:-}
    
    if [ "$SKIP_LOGGING" = true ]; then
        log_info "Skipping deployment logging"
        return 0
    fi
    
    log_info "Logging deployment to Platform API..."
    
    local payload=$(jq -n \
        --arg app "$CONTAINER_APP_NAME" \
        --arg commit "$image_tag" \
        --arg tag "$image_tag" \
        --arg status "$status" \
        --arg health "$health_status" \
        --arg notes "$notes" \
        '{
            appName: $app,
            commitSha: $commit,
            imageTag: $tag,
            deployedBy: "Blue-Green Deploy Script",
            status: $status,
            healthCheckStatus: $health,
            notes: $notes
        }')
    
    if [ -n "$work_item_id" ]; then
        payload=$(echo "$payload" | jq --argjson wi "$work_item_id" '. + {workItemId: $wi}')
    fi
    
    local response=$(curl -s -w "%{http_code}" -o /dev/null \
        -X POST "$PLATFORM_API_URL/api/v1/deploymentlog" \
        -H "Content-Type: application/json" \
        -H "X-API-Key: ${PLATFORM_API_KEY:-}" \
        -d "$payload" 2>/dev/null || echo "000")
    
    if [ "$response" = "200" ] || [ "$response" = "201" ]; then
        log_success "Deployment logged"
    else
        log_warning "Failed to log deployment (HTTP $response)"
    fi
}

cleanup_old_revisions() {
    log_info "Cleaning up old revisions (keeping $REVISIONS_TO_KEEP most recent)..."
    
    local old_revisions=$(az containerapp revision list \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "sort_by(@, &properties.createdTime) | reverse(@) | [$REVISIONS_TO_KEEP:].name" -o tsv 2>/dev/null)
    
    if [ -z "$old_revisions" ]; then
        log_info "No old revisions to clean up"
        return 0
    fi
    
    local count=0
    for rev in $old_revisions; do
        log_info "Deactivating: $rev"
        az containerapp revision deactivate \
            --name "$CONTAINER_APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision "$rev" 2>/dev/null && count=$((count + 1)) || true
    done
    
    log_success "Deactivated $count revisions"
}

rollback() {
    local old_revision=$1
    
    log_error "Rolling back to: $old_revision"
    
    az containerapp ingress traffic set \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --revision-weight "$old_revision=100" \
        --output none 2>/dev/null
    
    log_success "Rolled back"
}

# ============================================================================
# MAIN
# ============================================================================

main() {
    local IMAGE_TAG="${1:-}"
    local WORK_ITEM_ID=""
    local FINAL_TRAFFIC=100
    
    # Parse arguments
    shift || true
    while [[ $# -gt 0 ]]; do
        case $1 in
            --gradual) GRADUAL_ROLLOUT=true; shift ;;
            --skip-logging) SKIP_LOGGING=true; shift ;;
            --work-item) WORK_ITEM_ID="$2"; shift 2 ;;
            --traffic) FINAL_TRAFFIC="$2"; shift 2 ;;
            *) log_error "Unknown option: $1"; exit 1 ;;
        esac
    done
    
    if [ -z "$IMAGE_TAG" ]; then
        echo "Usage: $0 <image-tag> [options]"
        echo ""
        echo "Options:"
        echo "  --gradual          Enable gradual rollout (10% → 50% → 100%)"
        echo "  --skip-logging     Skip deployment logging"
        echo "  --work-item <id>   Associate with work item"
        echo "  --traffic <pct>    Final traffic percentage (default: 100)"
        exit 1
    fi
    
    local FULL_IMAGE="${ACR_REGISTRY}/${REPOSITORY_NAME}:${IMAGE_TAG}"
    local REVISION_SUFFIX="deploy-$(date +%Y%m%d%H%M%S)"
    
    print_header "🚀 BLUE-GREEN DEPLOYMENT: DocumentService"
    
    echo "Configuration:"
    echo "  Image:            $FULL_IMAGE"
    echo "  Revision:         $REVISION_SUFFIX"
    echo "  Gradual Rollout:  $GRADUAL_ROLLOUT"
    echo "  Final Traffic:    $FINAL_TRAFFIC%"
    [ -n "$WORK_ITEM_ID" ] && echo "  Work Item:        #$WORK_ITEM_ID"
    echo ""
    
    check_prerequisites
    
    # Step 1: Get current state
    print_header "📊 STEP 1: Current State"
    
    local OLD_REVISION=$(get_current_revision)
    log_info "Current revision: ${OLD_REVISION:-none}"
    
    # Step 2: Deploy new revision
    print_header "🟢 STEP 2: Deploy New Revision"
    
    az containerapp update \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --image "$FULL_IMAGE" \
        --revision-suffix "$REVISION_SUFFIX" \
        --output none 2>&1 || {
            log_error "Failed to deploy"
            log_deployment "$IMAGE_TAG" "Failed" "N/A" "Deployment failed" "$WORK_ITEM_ID"
            exit 1
        }
    
    sleep 15
    local NEW_REVISION=$(get_newest_revision)
    log_success "New revision: $NEW_REVISION"
    
    # Step 3: Wait for healthy
    print_header "⏳ STEP 3: Wait for Healthy"
    
    if ! wait_for_revision_healthy "$NEW_REVISION"; then
        log_deployment "$IMAGE_TAG" "Failed" "Unhealthy" "Revision failed to start" "$WORK_ITEM_ID"
        [ -n "$OLD_REVISION" ] && rollback "$OLD_REVISION"
        exit 1
    fi
    
    # Step 4: Health check
    print_header "🏥 STEP 4: Health Check"
    
    if ! health_check "$APP_URL/health"; then
        log_deployment "$IMAGE_TAG" "Failed" "Failed" "Health check failed" "$WORK_ITEM_ID"
        [ -n "$OLD_REVISION" ] && rollback "$OLD_REVISION"
        exit 1
    fi
    
    # Step 5: Traffic shift
    print_header "🔄 STEP 5: Traffic Shift"
    
    if [ "$GRADUAL_ROLLOUT" = true ] && [ "$FINAL_TRAFFIC" = "100" ]; then
        log_info "Gradual rollout: 10% → 50% → 100%"
        
        set_traffic "$NEW_REVISION" 10
        [ -n "$OLD_REVISION" ] && az containerapp ingress traffic set \
            --name "$CONTAINER_APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision-weight "$NEW_REVISION=10" "$OLD_REVISION=90" --output none 2>/dev/null
        
        log_info "Monitoring at 10% for ${TRAFFIC_SHIFT_WAIT}s..."
        sleep $TRAFFIC_SHIFT_WAIT
        
        az containerapp ingress traffic set \
            --name "$CONTAINER_APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --revision-weight "$NEW_REVISION=50" "$OLD_REVISION=50" --output none 2>/dev/null
        
        log_info "Monitoring at 50% for ${TRAFFIC_SHIFT_WAIT}s..."
        sleep $TRAFFIC_SHIFT_WAIT
        
        set_traffic "$NEW_REVISION" 100
    else
        set_traffic "$NEW_REVISION" "$FINAL_TRAFFIC"
    fi
    
    log_success "Traffic shifted to new revision"
    
    # Step 6: Connection drain
    print_header "⏳ STEP 6: Connection Drain"
    
    log_info "Waiting ${CONNECTION_DRAIN_WAIT}s for connections to drain..."
    sleep $CONNECTION_DRAIN_WAIT
    
    # Step 7: Cleanup
    print_header "🧹 STEP 7: Cleanup"
    
    cleanup_old_revisions
    
    # Step 8: Log deployment
    print_header "📝 STEP 8: Log Deployment"
    
    log_deployment "$IMAGE_TAG" "Success" "Passed" "Blue-green deployment completed" "$WORK_ITEM_ID"
    
    # Summary
    print_header "🎉 DEPLOYMENT COMPLETE!"
    
    echo "✅ Deployment successful"
    echo ""
    echo "Summary:"
    echo "  New Revision:  $NEW_REVISION"
    echo "  Old Revision:  ${OLD_REVISION:-none}"
    echo "  Traffic:       ${FINAL_TRAFFIC}%"
    echo ""
    
    if [ -n "$OLD_REVISION" ]; then
        echo "Rollback command:"
        echo "  az containerapp ingress traffic set \\"
        echo "    --name $CONTAINER_APP_NAME \\"
        echo "    --resource-group $RESOURCE_GROUP \\"
        echo "    --revision-weight $OLD_REVISION=100"
    fi
}

trap 'log_error "Script failed at line $LINENO"' ERR
main "$@"
