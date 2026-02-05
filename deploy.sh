#!/bin/bash

#===============================================================================
# CRISP - Code Repo Initialization & Scaffolding Platform
# Docker Deployment Script
#===============================================================================

set -e

# Configuration
APP_NAME="crisp"
COMPOSE_PROJECT_NAME="crisp"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is installed and running
check_docker() {
    log_info "Checking Docker installation..."

    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi

    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running. Please start Docker."
        exit 1
    fi

    # Check for docker compose (v2)
    if ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not available. Please install Docker Compose v2."
        exit 1
    fi

    log_success "Docker and Docker Compose are installed and running."
}

# Check for .env file
check_env() {
    log_info "Checking environment configuration..."

    if [ ! -f .env ]; then
        if [ -f .env.example ]; then
            log_warning ".env file not found. Creating from .env.example..."
            cp .env.example .env
            log_warning "Please edit .env file with your credentials before deploying."
            log_warning "Required: CLAUDE_API_KEY, GITHUB_OWNER, GITHUB_TOKEN"
            exit 1
        else
            log_error ".env file not found and no .env.example available."
            exit 1
        fi
    fi

    # Check required variables
    source .env
    local missing=""

    if [ -z "$CLAUDE_API_KEY" ] || [ "$CLAUDE_API_KEY" = "sk-ant-your-api-key-here" ]; then
        missing="$missing CLAUDE_API_KEY"
    fi

    if [ -z "$GITHUB_OWNER" ] || [ "$GITHUB_OWNER" = "your-username-or-org" ]; then
        missing="$missing GITHUB_OWNER"
    fi

    if [ -z "$GITHUB_TOKEN" ] || [ "$GITHUB_TOKEN" = "ghp_your_personal_access_token" ]; then
        missing="$missing GITHUB_TOKEN"
    fi

    if [ -n "$missing" ]; then
        log_error "Missing or unconfigured environment variables:$missing"
        log_error "Please edit .env file with your credentials."
        exit 1
    fi

    log_success "Environment configuration is valid."
}

# Create cloudflared network if it doesn't exist
check_network() {
    log_info "Checking Docker network..."

    if ! docker network ls | grep -q "cloudflared-tunnel"; then
        log_info "Creating cloudflared-tunnel network..."
        docker network create cloudflared-tunnel
    fi

    log_success "Docker network is ready."
}

# Stop and remove existing containers
cleanup() {
    log_info "Cleaning up existing containers..."

    docker compose -p ${COMPOSE_PROJECT_NAME} down --remove-orphans 2>/dev/null || true

    log_success "Cleanup completed."
}

# Pull latest code
pull() {
    log_info "Pulling latest code from repository..."

    git pull origin main

    log_success "Code updated."
}

# Build Docker images
build() {
    log_info "Building Docker images..."

    docker compose -p ${COMPOSE_PROJECT_NAME} build

    log_success "Docker images built successfully."
}

# Start containers
start() {
    log_info "Starting containers..."

    docker compose -p ${COMPOSE_PROJECT_NAME} up -d

    log_success "Containers started."
}

# Show status
status() {
    echo ""
    log_info "Container Status:"
    echo "----------------------------------------"

    docker compose -p ${COMPOSE_PROJECT_NAME} ps

    echo ""
    echo "----------------------------------------"
    log_info "Service URLs:"
    echo "  - Web UI:  http://localhost:3000"
    echo "  - API:     http://localhost:5000"
    echo "  - Swagger: http://localhost:5000/swagger"
    echo ""
}

# Show logs
logs() {
    local service="${1:-}"
    log_info "Showing logs..."

    if [ -n "$service" ]; then
        docker compose -p ${COMPOSE_PROJECT_NAME} logs -f "$service"
    else
        docker compose -p ${COMPOSE_PROJECT_NAME} logs -f
    fi
}

# Stop containers
stop() {
    log_info "Stopping containers..."
    docker compose -p ${COMPOSE_PROJECT_NAME} stop
    log_success "Containers stopped."
}

# Restart containers
restart() {
    log_info "Restarting containers..."
    docker compose -p ${COMPOSE_PROJECT_NAME} restart
    log_success "Containers restarted."
    status
}

# Full deployment
deploy() {
    echo ""
    echo "=========================================="
    echo "  CRISP - Docker Deployment"
    echo "=========================================="
    echo ""

    check_docker
    check_env
    check_network
    cleanup
    build
    start

    # Wait for services to be healthy
    log_info "Waiting for services to be healthy..."
    sleep 10

    status

    echo ""
    echo "=========================================="
    log_success "Deployment completed!"
    echo "=========================================="
}

# Update deployment (pull + rebuild + restart)
update() {
    echo ""
    echo "=========================================="
    echo "  CRISP - Update Deployment"
    echo "=========================================="
    echo ""

    check_docker
    pull
    build

    log_info "Recreating containers..."
    docker compose -p ${COMPOSE_PROJECT_NAME} up -d --force-recreate

    sleep 10
    status

    echo ""
    echo "=========================================="
    log_success "Update completed!"
    echo "=========================================="
}

# Show help
show_help() {
    echo ""
    echo "CRISP - Docker Deployment Script"
    echo ""
    echo "Usage: ./deploy.sh [command]"
    echo ""
    echo "Commands:"
    echo "  deploy    Full deployment (default) - build and run"
    echo "  update    Pull latest code, rebuild and restart"
    echo "  build     Build Docker images only"
    echo "  start     Start the containers"
    echo "  stop      Stop the containers"
    echo "  restart   Restart the containers"
    echo "  status    Show container status"
    echo "  logs      Show container logs (follow mode)"
    echo "  logs api  Show API logs only"
    echo "  logs web  Show web logs only"
    echo "  cleanup   Stop and remove containers"
    echo "  pull      Pull latest code from repository"
    echo "  help      Show this help message"
    echo ""
}

# Main script
case "${1:-deploy}" in
    deploy)
        deploy
        ;;
    update)
        update
        ;;
    build)
        check_docker
        build
        ;;
    start)
        check_docker
        check_env
        check_network
        start
        status
        ;;
    stop)
        stop
        ;;
    restart)
        restart
        ;;
    status)
        status
        ;;
    logs)
        logs "$2"
        ;;
    cleanup)
        cleanup
        ;;
    pull)
        pull
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        log_error "Unknown command: $1"
        show_help
        exit 1
        ;;
esac
