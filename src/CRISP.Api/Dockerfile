# CRISP API Dockerfile
# Multi-stage build for .NET 8 API + React Frontend

# Build stage for React frontend
FROM node:20-alpine AS frontend-build
WORKDIR /web
COPY src/crisp-web/package*.json ./
RUN npm install
COPY src/crisp-web/ ./
RUN npm run build

# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY CRISP.sln Directory.Build.props Directory.Packages.props global.json ./
COPY src/CRISP.Core/CRISP.Core.csproj src/CRISP.Core/
COPY src/CRISP.Audit/CRISP.Audit.csproj src/CRISP.Audit/
COPY src/CRISP.Git/CRISP.Git.csproj src/CRISP.Git/
COPY src/CRISP.Templates/CRISP.Templates.csproj src/CRISP.Templates/
COPY src/CRISP.Pipelines/CRISP.Pipelines.csproj src/CRISP.Pipelines/
COPY src/CRISP.Agent/CRISP.Agent.csproj src/CRISP.Agent/
COPY src/CRISP.Api/CRISP.Api.csproj src/CRISP.Api/
COPY src/CRISP.Mcp.GitHub/CRISP.Mcp.GitHub.csproj src/CRISP.Mcp.GitHub/
COPY src/CRISP.Mcp.AzureDevOps/CRISP.Mcp.AzureDevOps.csproj src/CRISP.Mcp.AzureDevOps/
COPY src/CRISP.Mcp.Filesystem/CRISP.Mcp.Filesystem.csproj src/CRISP.Mcp.Filesystem/
COPY src/CRISP.Mcp.Git/CRISP.Mcp.Git.csproj src/CRISP.Mcp.Git/
COPY src/CRISP.Mcp.AuditLog/CRISP.Mcp.AuditLog.csproj src/CRISP.Mcp.AuditLog/
COPY src/CRISP.Mcp.PolicyEngine/CRISP.Mcp.PolicyEngine.csproj src/CRISP.Mcp.PolicyEngine/

# Restore dependencies
RUN dotnet restore src/CRISP.Api/CRISP.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/CRISP.Api/CRISP.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install git for LibGit2Sharp operations and curl for health checks
RUN apt-get update && apt-get install -y git curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -s /bin/bash crisp

# Copy published API
COPY --from=api-build /app/publish .

# Copy frontend build to wwwroot
COPY --from=frontend-build /web/dist ./wwwroot

# Create directories for logs, workspaces, and sessions
RUN mkdir -p /home/crisp/logs /home/crisp/workspaces /home/crisp/.crisp/sessions && \
    chown -R crisp:crisp /home/crisp /app/wwwroot

USER crisp

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "CRISP.Api.dll"]
