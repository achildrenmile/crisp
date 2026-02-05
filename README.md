# CRISP — Code Repo Initialization & Scaffolding Platform

CRISP is an AI-powered autonomous agent that transforms developer requirements into fully configured, ready-to-use code repositories with zero manual steps.

## Overview

CRISP accepts developer requirements via a chat interface, validates the input, selects appropriate templates, scaffolds the project, creates a remote repository, sets up CI/CD pipelines, and delivers a working repository link with an "Open in VS Code" option.

### Supported Platforms

| Platform | Status | Description |
|----------|--------|-------------|
| **GitHub** | Primary | Default platform with full GitHub Actions support |
| **Azure DevOps Server** | Alternative | On-premises Azure DevOps with YAML/XAML pipelines |

## Architecture

CRISP is built with:
- **.NET 8** backend (API and agent orchestration)
- **React** frontend (TypeScript, Vite)
- **Model Context Protocol (MCP)** for tool communication
- **Claude AI** for natural language understanding

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           crisp-web (React)                              │
│                    Chat UI / Plan Approval / Delivery                    │
├─────────────────────────────────────────────────────────────────────────┤
│                           CRISP.Api (REST)                               │
│              Sessions / Messages / SSE Events / Claude                   │
├─────────────────────────────────────────────────────────────────────────┤
│                          CRISP.Agent                                     │
│                      (Main Orchestrator)                                 │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                     │
│  │ CRISP.Core  │  │CRISP.Audit  │  │ CRISP.Git   │                     │
│  │  (Models)   │  │ (Logging)   │  │(LibGit2Sharp)│                    │
│  └─────────────┘  └─────────────┘  └─────────────┘                     │
│  ┌─────────────┐  ┌──────────────────────────────┐                     │
│  │  CRISP.     │  │       CRISP.Pipelines        │                     │
│  │  Templates  │  │  (GitHub Actions / Azure)    │                     │
│  │ (Scriban)   │  └──────────────────────────────┘                     │
│  └─────────────┘                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                          MCP Servers                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                 │
│  │ Mcp.GitHub   │  │Mcp.AzureDevOps│  │Mcp.Filesystem│                │
│  └──────────────┘  └──────────────┘  └──────────────┘                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                 │
│  │   Mcp.Git    │  │Mcp.AuditLog  │  │Mcp.PolicyEngine│               │
│  └──────────────┘  └──────────────┘  └──────────────┘                 │
└─────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
crisp/
├── src/
│   ├── CRISP.Api/               # REST API (ASP.NET Core 8)
│   ├── CRISP.Core/              # Domain models, interfaces, configuration
│   ├── CRISP.Audit/             # Structured audit logging with Serilog
│   ├── CRISP.Git/               # Local Git operations via LibGit2Sharp
│   ├── CRISP.Templates/         # Scriban-based project scaffolding
│   ├── CRISP.Pipelines/         # CI/CD pipeline generation (YAML)
│   ├── CRISP.Agent/             # Main orchestrator entry point
│   ├── CRISP.Mcp.GitHub/        # GitHub MCP server (Octokit)
│   ├── CRISP.Mcp.AzureDevOps/   # Azure DevOps MCP server
│   ├── CRISP.Mcp.Filesystem/    # Filesystem operations MCP server
│   ├── CRISP.Mcp.Git/           # Local Git MCP server
│   ├── CRISP.Mcp.AuditLog/      # Audit logging MCP server
│   ├── CRISP.Mcp.PolicyEngine/  # Policy enforcement MCP server
│   └── crisp-web/               # React frontend (Vite + TypeScript)
├── tests/
│   ├── CRISP.Core.Tests/
│   ├── CRISP.Git.Tests/
│   ├── CRISP.Templates.Tests/
│   ├── CRISP.Pipelines.Tests/
│   └── CRISP.Agent.Tests/
├── .github/workflows/ci.yml     # GitHub Actions CI
├── docker-compose.yml           # Docker orchestration
├── .env.example                 # Environment variables template
├── Directory.Build.props        # Central MSBuild configuration
├── Directory.Packages.props     # Central package management
├── global.json                  # .NET SDK version
└── CRISP.sln                    # Solution file
```

## How It Works

CRISP uses Claude AI to have a natural conversation with developers to understand their project requirements. When enough information is gathered, Claude outputs a structured JSON action that triggers the actual scaffolding process.

### Conversation Flow

1. **Developer describes project** - "I want to create a new Python API called order-service"
2. **Claude asks clarifying questions** - Framework preference, visibility, Docker support, etc.
3. **Claude confirms and creates** - Once requirements are clear, outputs a JSON action block
4. **CRISP executes** - Creates the repository, scaffolds code, pushes to GitHub/Azure DevOps
5. **Delivery** - Returns repository URL, clone command, and VS Code link

### Example Conversation

```
User: I need a new Python API for handling customer orders

Claude: I can help you create that! Let me gather a few details:
- What would you like to name the project? (e.g., "order-service")
- Should I include Docker support?

User: Call it customer-orders-api, and yes include Docker

Claude: Great! I'll create customer-orders-api with:
- Language: Python
- Framework: FastAPI
- Docker: Yes
- Visibility: Private

Ready to create?

User: yes

[CRISP executes scaffolding and creates the repository]

Claude: ✅ Repository Created Successfully!
📦 Repository: https://github.com/crispsorg/customer-orders-api
🚀 Clone: git clone https://github.com/crispsorg/customer-orders-api.git
```

## Getting Started

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) (recommended for deployment)
- Or for local development:
  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - [Node.js 18+](https://nodejs.org/) (for the React frontend)
  - Git

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/achildrenmile/crisp.git
   cd crisp
   ```

2. **Build the .NET solution**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Configure the API**

   Create `src/CRISP.Api/appsettings.Development.json`:
   ```json
   {
     "Claude": {
       "ApiKey": "sk-ant-your-api-key"
     },
     "Crisp": {
       "ScmPlatform": "GitHub",
       "GitHub": {
         "Owner": "your-username-or-org",
         "Token": "ghp_your_token"
       }
     }
   }
   ```

4. **Start the API** (runs on http://localhost:5000)
   ```bash
   cd src/CRISP.Api
   dotnet run
   ```

5. **Start the React frontend** (runs on http://localhost:3000)
   ```bash
   cd src/crisp-web
   npm install
   npm run dev
   ```

6. **Open your browser** to http://localhost:3000

### Running Tests

```bash
dotnet test
```

## Docker Deployment

CRISP can be run using Docker for easy deployment.

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)

### Quick Start with Docker

1. **Clone and configure**
   ```bash
   git clone https://github.com/achildrenmile/crisp.git
   cd crisp
   cp .env.example .env
   ```

2. **Edit `.env`** with your credentials:
   ```bash
   CLAUDE_API_KEY=sk-ant-your-api-key
   GITHUB_OWNER=your-username-or-org
   GITHUB_TOKEN=ghp_your_token
   ```

3. **Build and run**
   ```bash
   docker-compose up --build
   ```

4. **Access the application**
   - Web UI: http://localhost:3000
   - API: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

### Docker Services

| Service | Port | Description |
|---------|------|-------------|
| `api` | 5000 | CRISP REST API (.NET 8) |
| `web` | 3000 | React frontend (nginx) |

### Docker Commands

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Environment Variables for Docker

| Variable | Description | Default |
|----------|-------------|---------|
| `CLAUDE_API_KEY` | Anthropic Claude API key | (required) |
| `CLAUDE_MODEL` | Claude model to use | `claude-sonnet-4-20250514` |
| `SCM_PLATFORM` | `GitHub` or `AzureDevOps` | `GitHub` |
| `GITHUB_OWNER` | GitHub username or org | (required for GitHub) |
| `GITHUB_TOKEN` | GitHub personal access token | (required for GitHub) |
| `GITHUB_VISIBILITY` | Repository visibility | `Private` |
| `AZURE_DEVOPS_ORG` | Azure DevOps org URL | (for Azure DevOps) |
| `AZURE_DEVOPS_PROJECT` | Azure DevOps project | (for Azure DevOps) |
| `AZURE_DEVOPS_PAT` | Azure DevOps PAT | (for Azure DevOps) |
| `DEFAULT_BRANCH` | Default branch name | `main` |
| `GENERATE_CICD` | Generate CI/CD pipelines | `true` |
| `BASIC_AUTH_USER` | HTTP Basic Auth username | (optional) |
| `BASIC_AUTH_PASS` | HTTP Basic Auth password | (optional) |

### Securing with Basic Auth

The web frontend supports HTTP Basic Authentication for demo/staging deployments:

```bash
# In .env file
BASIC_AUTH_USER=myusername
BASIC_AUTH_PASS=mystrongpassword
```

When these variables are set, nginx will require authentication to access the web UI. API endpoints are excluded from auth to allow programmatic access.

### Building Individual Images

```bash
# Build API image only
docker build -t crisp-api -f src/CRISP.Api/Dockerfile .

# Build web image only
docker build -t crisp-web src/crisp-web
```

## API Endpoints

The CRISP API provides the following endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat/sessions` | Create a new chat session |
| `POST` | `/api/chat/sessions/{id}/messages` | Send a message to the agent |
| `GET` | `/api/chat/sessions/{id}/messages` | Get message history |
| `GET` | `/api/chat/sessions/{id}/events` | SSE stream for real-time updates |
| `POST` | `/api/chat/sessions/{id}/approve` | Approve or reject execution plan |
| `GET` | `/api/chat/sessions/{id}/status` | Get session status |
| `GET` | `/api/chat/sessions/{id}/result` | Get delivery result |
| `GET` | `/api/health` | Health check |

API documentation is available at http://localhost:5000/swagger

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `Claude__ApiKey` | Anthropic Claude API key | (required) |
| `Claude__Model` | Claude model to use | `claude-sonnet-4-20250514` |
| `Crisp__ScmPlatform` | `GitHub` or `AzureDevOps` | `GitHub` |
| `Crisp__GitHub__Owner` | GitHub username or org | (required for GitHub) |
| `Crisp__GitHub__Token` | GitHub personal access token | (required for GitHub) |
| `Crisp__AzureDevOps__Organization` | Azure DevOps org URL | (required for Azure DevOps) |
| `Crisp__AzureDevOps__Project` | Azure DevOps project name | (required for Azure DevOps) |
| `Crisp__AzureDevOps__Pat` | Azure DevOps PAT | (required for Azure DevOps) |

### appsettings.json

```json
{
  "Claude": {
    "ApiKey": "sk-ant-xxx",
    "Model": "claude-sonnet-4-20250514",
    "MaxTokens": 4096
  },
  "Crisp": {
    "ScmPlatform": "GitHub",
    "GitHub": {
      "Owner": "your-org",
      "Visibility": "Private",
      "Token": "ghp_xxx"
    },
    "Common": {
      "DefaultBranch": "main",
      "GenerateCiCd": true
    }
  }
}
```

## Component Details

### CRISP.Api

**Purpose:** REST API for the chat interface and agent orchestration.

**Features:**
- Session management with in-memory storage
- Server-Sent Events (SSE) for real-time updates
- Claude API integration for LLM-powered conversations
- Swagger/OpenAPI documentation

### CRISP.Core

**Purpose:** Core domain models, enums, interfaces, and configuration types.

**Key Files:**
- `Enums/` - ScmPlatform, ProjectLanguage, ProjectFramework, ExecutionPhase, etc.
- `Models/` - ProjectRequirements, ExecutionPlan, AuditLogEntry, DeliveryResult
- `Interfaces/` - ISourceControlProvider, ITemplateEngine, IPipelineGenerator, IGitOperations, etc.
- `Configuration/` - CrispConfiguration with GitHub and AzureDevOps settings

### CRISP.Audit

**Purpose:** Structured audit logging for tracking all agent actions.

**Features:**
- Session-based logging with unique session IDs
- Action logging with timestamps, parameters, and outcomes
- Export to JSON or CSV formats
- Integration with Serilog for structured logging

### CRISP.Git

**Purpose:** Local Git operations using LibGit2Sharp.

**Operations:**
- Repository initialization with custom default branch
- Staging and committing files
- Adding remotes and pushing changes
- Amending commits and force pushing (for remediation)

### CRISP.Templates

**Purpose:** Project scaffolding using Scriban templates.

**Built-in Generators:**
- **AspNetCoreWebApiGenerator** - ASP.NET Core 8 Web API with Swagger, xUnit tests
- **FastApiGenerator** - Python FastAPI with pytest, Ruff linting support

**Features:**
- Deterministic output (same inputs = same files)
- Container support (Dockerfile, docker-compose)
- Editor configuration (.editorconfig, VS Code settings)

### CRISP.Pipelines

**Purpose:** CI/CD pipeline generation.

**Generators:**
- **GitHubActionsGenerator** - `.github/workflows/ci.yml`
- **AzurePipelinesGenerator** - `azure-pipelines.yml`

**Features:**
- Language-specific build steps
- Code coverage configuration
- Docker image building (optional)
- YAML validation

### CRISP.Agent

**Purpose:** Main orchestrator that coordinates all components.

**Execution Flow:**
1. **Phase 1 - Intake:** Parse and validate requirements
2. **Phase 2 - Planning:** Generate execution plan, validate policies
3. **Phase 3 - Execution:** Scaffold, create repo, push, trigger CI
4. **Phase 4 - Delivery:** Return results with VS Code link

### crisp-web (React Frontend)

**Purpose:** Chat-based UI for interacting with CRISP.

**Tech Stack:**
- React 18 with TypeScript
- Vite for build tooling
- react-router-dom for routing
- react-markdown for rendering agent responses
- Server-Sent Events for real-time updates

**Components:**
- `ChatMessage` - Renders user/assistant messages
- `ChatInput` - Input form for sending messages
- `PlanView` - Displays execution plan with approve/reject
- `DeliveryCard` - Shows repository delivery result

### MCP Servers

All MCP servers implement the Model Context Protocol and expose tools via stdio transport.

| Server | Tools |
|--------|-------|
| **Mcp.GitHub** | create_repository, configure_branch_protection, trigger_workflow, get_workflow_status, validate_connection |
| **Mcp.AzureDevOps** | create_repository, configure_branch_policies, create_pipeline, trigger_pipeline, get_build_status, get_server_info |
| **Mcp.Filesystem** | create_workspace, create_directory, write_file, read_file, list_files, delete, exists, cleanup_workspace |
| **Mcp.Git** | init, stage_all, commit, add_remote, push, get_current_branch, get_latest_commit |
| **Mcp.AuditLog** | log_action, get_session_logs, export_logs, get_session_id, set_agent_id |
| **Mcp.PolicyEngine** | get_policies, load_policies, check_policies_passed |

## Supported Technologies

### Languages & Frameworks

| Language | Frameworks | Template |
|----------|-----------|----------|
| C# / .NET 8 | ASP.NET Core Web API | `aspnetcore-webapi` |
| Python 3.12 | FastAPI | `python-fastapi` |

### Additional Tooling (configurable)

- **Linting:** Roslyn analyzers, Ruff, ESLint
- **Testing:** xUnit, pytest, Jest
- **Containers:** Dockerfile, docker-compose
- **CI/CD:** GitHub Actions, Azure Pipelines

## Design Principles

1. **Zero-touch delivery** - No manual steps between requirements and working repo
2. **Reproducible output** - Same inputs always produce identical projects
3. **Policy compliance** - All organizational policies evaluated before execution
4. **Full audit trail** - Every action logged with timestamps and outcomes
5. **Platform-agnostic** - Same logic works for GitHub and Azure DevOps

## Deployment

### Production Deployment with Docker Compose

For a production deployment, you'll typically:

1. **Set up a server** with Docker installed
2. **Clone the repository** and configure `.env`
3. **Run behind a reverse proxy** (nginx, Caddy, Traefik) with HTTPS

Example with Cloudflare Tunnel:

```bash
# On your server
git clone https://github.com/achildrenmile/crisp.git /opt/crisp
cd /opt/crisp
cp .env.example .env

# Edit .env with your credentials
nano .env

# Start services
docker-compose up -d

# Services are now running on:
# - API: localhost:5000
# - Web: localhost:3000
```

### GitHub Token Requirements

Your GitHub Personal Access Token needs these scopes:

- `repo` - Full control of private repositories
- `workflow` - Update GitHub Action workflows
- `admin:org` - (if using an organization) Manage organization settings

For organization repositories, you must also authorize the token for the organization in GitHub settings.

## Troubleshooting

### Common Issues

**"Failed to create repository"**
- Check that your GitHub token has `repo` scope
- For organizations, ensure the token is authorized for that org
- Verify the repository name doesn't already exist

**"Session not found" errors**
- Sessions are stored in-memory and reset when the API restarts
- Create a new session after API restarts

**"Scaffolding appears to work but no repo created"**
- Ensure the API can reach GitHub (check network/firewall)
- Review API logs: `docker-compose logs api`

### Viewing Logs

```bash
# All services
docker-compose logs -f

# API only
docker-compose logs -f api

# Web only
docker-compose logs -f web
```

## License

MIT License - See LICENSE file for details.
