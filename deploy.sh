#!/bin/bash
# CRISP Deployment Script
# Usage: ./deploy.sh [host]

set -e

HOST="${1:-host-node-01}"
REMOTE_PATH="/home/achildrenmile/apps/crisp"

echo "=== CRISP Deployment ==="
echo "Target: $HOST"
echo ""

# Pull latest code on remote
echo "[1/3] Pulling latest code..."
ssh "$HOST" "cd $REMOTE_PATH && git pull origin main"

# Rebuild and restart container
echo "[2/3] Rebuilding container..."
ssh "$HOST" "cd $REMOTE_PATH && docker compose build"

# Restart services
echo "[3/3] Restarting services..."
ssh "$HOST" "cd $REMOTE_PATH && docker compose down && docker compose up -d"

# Wait for health check
echo ""
echo "Waiting for health check..."
sleep 10

# Check status
ssh "$HOST" "docker ps --filter name=crisp --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"

echo ""
echo "=== Deployment Complete ==="
