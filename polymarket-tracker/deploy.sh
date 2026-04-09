#!/usr/bin/env bash
set -euo pipefail

PROJECT_NAME="polymarket-tracker"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log()  { echo -e "${GREEN}[deploy]${NC} $*"; }
warn() { echo -e "${YELLOW}[deploy]${NC} $*"; }
err()  { echo -e "${RED}[deploy]${NC} $*" >&2; }

# Parse flags
BUILD_ONLY=false
NO_CACHE=""
TAIL_LOGS=false
FIX_IPV6=false

for arg in "$@"; do
  case $arg in
    --build-only) BUILD_ONLY=true ;;
    --no-cache)   NO_CACHE="--no-cache" ;;
    --logs)       TAIL_LOGS=true ;;
    --fix-ipv6)   FIX_IPV6=true ;;
    --help)
      echo "Usage: ./deploy.sh [--build-only] [--no-cache] [--logs] [--fix-ipv6]"
      echo "  --build-only   Build containers without starting them"
      echo "  --no-cache     Force rebuild without Docker cache"
      echo "  --logs         Tail logs after deployment"
      echo "  --fix-ipv6     Fix IPv6 connectivity issues with container registries (requires sudo)"
      exit 0
      ;;
    *) err "Unknown flag: $arg"; exit 1 ;;
  esac
done

# Check prerequisites
command -v docker >/dev/null 2>&1 || { err "docker is required but not installed"; exit 1; }
docker compose version >/dev/null 2>&1 || { err "docker compose is required"; exit 1; }

# ---------------------------------------------------------------------------
# IPv6 fix: Docker image pulls from Microsoft/Docker Hub CDNs can fail over
# IPv6 when the connection is unstable. This temporarily disables IPv6 on the
# Docker daemon so pulls go over IPv4, then re-enables it after the build.
# ---------------------------------------------------------------------------
IPV6_WAS_FIXED=false

fix_ipv6_for_docker() {
  log "Applying IPv6 fix for Docker image pulls..."

  # Method 1: Configure Docker daemon to disable IPv6 networking
  local DAEMON_JSON="/etc/docker/daemon.json"
  local BACKUP="/etc/docker/daemon.json.bak"

  sudo mkdir -p /etc/docker

  if [ -f "$DAEMON_JSON" ]; then
    sudo cp "$DAEMON_JSON" "$BACKUP"
    # Merge ip6tables:false into existing config
    sudo python3 -c "
import json
try:
    with open('$DAEMON_JSON') as f:
        cfg = json.load(f)
except:
    cfg = {}
cfg['ip6tables'] = False
cfg['fixed-cidr-v6'] = ''
with open('$DAEMON_JSON','w') as f:
    json.dump(cfg, f, indent=2)
"
  else
    echo '{"ip6tables": false}' | sudo tee "$DAEMON_JSON" > /dev/null
  fi

  sudo systemctl restart docker
  # Wait for Docker to come back
  local attempts=0
  until docker info >/dev/null 2>&1; do
    attempts=$((attempts + 1))
    if [ $attempts -ge 15 ]; then
      err "Docker failed to restart after IPv6 fix"
      restore_ipv6
      exit 1
    fi
    sleep 1
  done

  IPV6_WAS_FIXED=true
  log "Docker restarted with IPv6 disabled for pulls"
}

restore_ipv6() {
  if $IPV6_WAS_FIXED; then
    log "Restoring Docker daemon config..."
    local DAEMON_JSON="/etc/docker/daemon.json"
    local BACKUP="/etc/docker/daemon.json.bak"

    if [ -f "$BACKUP" ]; then
      sudo mv "$BACKUP" "$DAEMON_JSON"
    else
      sudo rm -f "$DAEMON_JSON"
    fi
    sudo systemctl restart docker 2>/dev/null || true
  fi
}

# Auto-detect IPv6 issues by testing connectivity to MCR
detect_ipv6_issue() {
  # Quick test: try to reach Microsoft Container Registry over IPv6
  # If it fails but IPv4 works, we have an IPv6 routing problem
  if curl -4 -sf --max-time 5 https://mcr.microsoft.com/v2/ >/dev/null 2>&1; then
    if ! curl -6 -sf --max-time 5 https://mcr.microsoft.com/v2/ >/dev/null 2>&1; then
      return 0 # IPv6 is broken
    fi
  fi
  return 1 # IPv6 is fine (or both are broken)
}

# Try to fix IPv6 automatically or on request
if $FIX_IPV6; then
  fix_ipv6_for_docker
elif detect_ipv6_issue; then
  warn "IPv6 connectivity to container registries appears broken"
  warn "Attempting automatic fix (will require sudo)..."
  fix_ipv6_for_docker
fi

# Ensure cleanup on exit
trap restore_ipv6 EXIT

# Build with retry logic for transient network failures
build_with_retry() {
  local max_retries=3
  local attempt=1

  while [ $attempt -le $max_retries ]; do
    log "Building containers (attempt $attempt/$max_retries)..."
    if docker compose -p "$PROJECT_NAME" build $NO_CACHE 2>&1; then
      log "Build succeeded!"
      return 0
    fi

    if [ $attempt -lt $max_retries ]; then
      warn "Build failed, retrying in 5 seconds..."
      sleep 5
    fi
    attempt=$((attempt + 1))
  done

  err "Build failed after $max_retries attempts"
  err "If this is a network issue, try: ./deploy.sh --fix-ipv6"
  return 1
}

build_with_retry

if $BUILD_ONLY; then
  log "Build complete (--build-only flag set, not starting)"
  exit 0
fi

# Stop existing
log "Stopping existing containers..."
docker compose -p "$PROJECT_NAME" down --remove-orphans 2>/dev/null || true

# Start
log "Starting containers..."
docker compose -p "$PROJECT_NAME" up -d

# Wait for health
log "Waiting for backend health check..."
ATTEMPTS=0
MAX_ATTEMPTS=30
until curl -sf http://localhost:5000/api/health >/dev/null 2>&1; do
  ATTEMPTS=$((ATTEMPTS + 1))
  if [ $ATTEMPTS -ge $MAX_ATTEMPTS ]; then
    err "Backend failed to become healthy after ${MAX_ATTEMPTS} attempts"
    docker compose -p "$PROJECT_NAME" logs backend --tail 50
    exit 1
  fi
  sleep 2
done

log "Backend is healthy!"

# Status
echo ""
docker compose -p "$PROJECT_NAME" ps
echo ""
log "Access:"
log "  Frontend:  http://localhost:3000"
log "  Backend:   http://localhost:5000"
log "  API:       http://localhost:5000/api/markets"
log "  Health:    http://localhost:5000/api/health/ready"
echo ""

if $TAIL_LOGS; then
  log "Tailing logs (Ctrl+C to stop)..."
  docker compose -p "$PROJECT_NAME" logs -f
fi
