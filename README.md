# CRISP — Code Repo Initialization & Scaffolding Platform

CRISP is an AI-powered autonomous agent that transforms developer requirements into fully configured, ready-to-use code repositories with zero manual steps.

## Overview

CRISP accepts developer requirements via a chat interface, validates the input, selects appropriate templates, scaffolds the project, creates a remote repository, sets up CI/CD pipelines, and delivers a working repository link with an "Open in VS Code" option.

### Supported Platforms

| Platform | Status | Description |
|----------|--------|-------------|
| **GitHub** | Primary | Default platform with full GitHub Actions support |
| **Azure DevOps Server** | Alternative | On-premises Azure DevOps with YAML/XAML pipelines |

### Highlights

- **AI Chat Interface** - Describe your project in plain English
- **Multiple Templates** - ASP.NET Core, FastAPI, Dart Shelf (more coming)
- **Auto CI/CD** - GitHub Actions or Azure Pipelines generated automatically
- **Docker Ready** - Every project includes Dockerfile and docker-compose
- **ADR Generation** - Architecture Decision Records created automatically
- **Enterprise Modules** - 10 production-ready modules (security, SBOM, compliance, observability, runbooks)
- **Theme Support** - Light, dark, and auto modes
- **Session History** - Resume previous conversations
- **Enterprise Auth** - OIDC/SSO support for corporate identity providers

## Architecture

CRISP is built with:
- **.NET 10** backend (API and agent orchestration)
- **React** frontend (TypeScript, Vite)
- **Model Context Protocol (MCP)** for tool communication
- **LLM integration** - Claude (default) or OpenAI-compatible APIs

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           crisp-web (React)                              │
│                    Chat UI / Plan Approval / Delivery                    │
├─────────────────────────────────────────────────────────────────────────┤
│                        CRISP.Api (REST .NET 10)                          │
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
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                      CRISP.Enterprise                            │  │
│  │  Security | SBOM | License | Ownership | Observability | Runbook │  │
│  └──────────────────────────────────────────────────────────────────┘  │
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
│   ├── CRISP.Api/               # REST API (ASP.NET Core 10)
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
│   ├── CRISP.Adr/               # Architecture Decision Records generation
│   ├── CRISP.Enterprise/        # Enterprise modules (security, compliance, docs)
│   └── crisp-web/               # React frontend (Vite + TypeScript)
├── tests/
│   ├── CRISP.Core.Tests/
│   ├── CRISP.Git.Tests/
│   ├── CRISP.Templates.Tests/
│   ├── CRISP.Pipelines.Tests/
│   ├── CRISP.Enterprise.Tests/
│   ├── CRISP.Adr.Tests/
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
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
  - [Node.js 22+](https://nodejs.org/) (for the React frontend)
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
| `api` | 5000 | CRISP REST API (.NET 10) |
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
| `LLM_PROVIDER` | LLM provider (`Claude` or `OpenAI`) | `Claude` |
| `CLAUDE_API_KEY` | Anthropic Claude API key | (required for Claude) |
| `CLAUDE_MODEL` | Claude model to use | `claude-sonnet-4-20250514` |
| `OPENAI_API_KEY` | OpenAI API key | (required for OpenAI) |
| `OPENAI_MODEL` | OpenAI model to use | `gpt-4o` |
| `OPENAI_BASE_URL` | Custom endpoint for OpenAI-compatible APIs | (optional) |
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
| `GET` | `/api/llm-info` | Get LLM provider and model info |

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
  "Llm": {
    "Provider": "Claude"
  },
  "Claude": {
    "ApiKey": "sk-ant-xxx",
    "Model": "claude-sonnet-4-20250514",
    "MaxTokens": 4096
  },
  "OpenAI": {
    "ApiKey": "sk-xxx",
    "Model": "gpt-4o",
    "MaxTokens": 4096,
    "BaseUrl": ""
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

### LLM Provider Configuration

CRISP supports multiple LLM providers. Set the `Llm:Provider` to choose:

| Provider | Description |
|----------|-------------|
| `Claude` | Anthropic Claude (default) |
| `OpenAI` | OpenAI or OpenAI-compatible APIs |

**Using Claude (default):**
```json
{
  "Llm": { "Provider": "Claude" },
  "Claude": {
    "ApiKey": "sk-ant-xxx",
    "Model": "claude-sonnet-4-20250514"
  }
}
```

**Using OpenAI:**
```json
{
  "Llm": { "Provider": "OpenAI" },
  "OpenAI": {
    "ApiKey": "sk-xxx",
    "Model": "gpt-4o"
  }
}
```

**Using OpenAI-compatible APIs (e.g., local LLMs, Azure OpenAI):**
```json
{
  "Llm": { "Provider": "OpenAI" },
  "OpenAI": {
    "ApiKey": "your-key",
    "Model": "your-model",
    "BaseUrl": "http://localhost:11434/v1"
  }
}
```

### SCM Platform Configuration

CRISP supports two SCM platforms. Set `Crisp:ScmPlatform` to choose between them:

| Platform | Value | Description |
|----------|-------|-------------|
| GitHub | `GitHub` | GitHub.com or GitHub Enterprise (default) |
| Azure DevOps | `AzureDevOps` | Azure DevOps Server (on-premises) |

**Using GitHub (default):**
```json
{
  "Crisp": {
    "ScmPlatform": "GitHub",
    "GitHub": {
      "Owner": "your-org-or-username",
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

**Using Azure DevOps (disables GitHub):**
```json
{
  "Crisp": {
    "ScmPlatform": "AzureDevOps",
    "AzureDevOps": {
      "ServerUrl": "https://azuredevops.contoso.local",
      "Collection": "DefaultCollection",
      "Project": "MyProject",
      "Token": "your-pat-token",
      "AgentPool": "Default",
      "PipelineFormat": "Yaml"
    },
    "Common": {
      "DefaultBranch": "main",
      "GenerateCiCd": true
    }
  }
}
```

**Azure DevOps Configuration Options:**

| Setting | Description | Default |
|---------|-------------|---------|
| `ServerUrl` | Azure DevOps Server URL | (required) |
| `Collection` | Collection name | `DefaultCollection` |
| `Project` | Team project name | (required) |
| `Token` | Personal Access Token (PAT) | (required) |
| `AgentPool` | Build agent pool name | (optional) |
| `PipelineFormat` | `Yaml` or `Xaml` | `Yaml` |
| `NuGetFeedUrl` | Internal NuGet feed URL | (optional) |

**Using Environment Variables (Docker):**

For GitHub:
```bash
SCM_PLATFORM=GitHub
GITHUB_OWNER=your-org
GITHUB_TOKEN=ghp_xxx
GITHUB_VISIBILITY=Private
```

For Azure DevOps:
```bash
SCM_PLATFORM=AzureDevOps
AZURE_DEVOPS_ORG=https://azuredevops.contoso.local
AZURE_DEVOPS_PROJECT=MyProject
AZURE_DEVOPS_PAT=your-pat-token
```

> **Note:** When `ScmPlatform` is set to `AzureDevOps`, GitHub configuration is ignored and vice versa. Only configure the platform you intend to use.

## Component Details

### CRISP.Api

**Purpose:** REST API for the chat interface and agent orchestration.

**Features:**
- Session management with in-memory storage
- Server-Sent Events (SSE) for real-time updates
- Multi-provider LLM integration (Claude, OpenAI, OpenAI-compatible)
- Automatic retry with exponential backoff for transient API failures
- Swagger/OpenAPI documentation
- JWT and OIDC authentication support

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
- **AspNetCoreWebApiGenerator** - ASP.NET Core 10 Web API with Swagger, xUnit tests
- **FastApiGenerator** - Python FastAPI with pytest, Ruff linting support
- **DartShelfGenerator** - Dart Shelf REST API with CRUD endpoints, health checks, Docker support

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

### CRISP.Adr

**Purpose:** Architecture Decision Records generation.

**Features:**
- Automatically generates ADRs during scaffolding
- MADR (Markdown Architectural Decision Records) format
- Full and short-form templates
- Index generation with decision table
- Configurable output directory and options

**Generated Files:**
- `docs/adr/README.md` - Index with table of all ADRs
- `docs/adr/template.md` - Blank template for future ADRs
- `docs/adr/0000-record-architecture-decisions.md` - Meta ADR
- `docs/adr/XXXX-decision-title.md` - Individual decisions

### CRISP.Enterprise

**Purpose:** Enterprise-grade modules that run during scaffolding to generate production-ready documentation, configuration, and compliance artifacts.

**Modules (10 total):**

| Module | Order | Description | Generated Files |
|--------|-------|-------------|-----------------|
| **Security Baseline** | 100 | Security policy, secrets management, gitignore | `SECURITY.md`, `.env.example`, `.gitignore` updates |
| **SBOM Configuration** | 200 | Software Bill of Materials CI integration | `sbom.json`, CI workflow updates |
| **License & Compliance** | 300 | License files and contribution guidelines | `LICENSE`, `CONTRIBUTING.md` |
| **Code Ownership** | 400 | GitHub/Azure DevOps code owners | `CODEOWNERS` or `azure-devops-codeowners.json` |
| **Branching Strategy** | 500 | Git branching documentation | `docs/BRANCHING.md` |
| **Observability** | 600 | Health checks, logging, tracing setup | Language-specific observability code |
| **README Generator** | 700 | Comprehensive project README | `README.md` with badges, setup, API docs |
| **Environment Config** | 800 | Environment-specific configuration | `docs/environments.md`, config files |
| **API Contract** | 900 | OpenAPI/AsyncAPI specs and API client | `openapi.yaml`, Bruno collection |
| **Runbook/Operations** | 1000 | Operational runbooks and troubleshooting | `docs/runbook.md`, `docs/troubleshooting.md` |

**Features:**
- **ADR Integration** - Each module records its decisions in the ADR system
- **Language-Aware** - Generates language-specific code (C#, Python, TypeScript, Dart)
- **Platform-Aware** - Adapts to GitHub vs Azure DevOps patterns
- **Secrets Manager Support** - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
- **Customizable** - Skip modules via configuration, customize templates

**Configuration (appsettings.json):**

```json
{
  "Enterprise": {
    "Enabled": true,
    "SkipModules": [],
    "Security": {
      "ContactEmail": "security@company.com"
    },
    "License": {
      "DefaultLicense": "MIT",
      "CopyrightHolder": "Your Company"
    },
    "Ownership": {
      "DefaultOwners": ["@team-lead", "@platform-team"]
    }
  }
}
```

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
- `ChatMessage` - Renders user/assistant messages with markdown support
- `ChatInput` - Input form for sending messages
- `PlanView` - Displays execution plan with approve/reject workflow
- `DeliveryCard` - Shows repository delivery result with VS Code links
- `ProjectHistory` - Sidebar showing previous sessions with quick access
- `ThemeToggle` - Auto/light/dark mode toggle

**Features:**
- **Theme Support** - Auto/light/dark mode with OS preference detection and localStorage persistence
- **Session Persistence** - Conversations are saved and restored across browser sessions
- **Project History** - Access previous scaffolded projects from the sidebar
- **Dual VS Code Links** - "Open in Browser" (vscode.dev) and "Clone to Desktop" (vscode:// protocol)
- **Real-time Updates** - Server-Sent Events for live status updates during scaffolding

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
| C# / .NET 10 | ASP.NET Core Web API | `aspnetcore-webapi` |
| Python 3.12 | FastAPI | `python-fastapi` |
| Dart 3.0 | Shelf | `dart-shelf` |

### Additional Tooling (configurable)

- **Linting:** Roslyn analyzers, Ruff, ESLint
- **Testing:** xUnit, pytest, Jest
- **Containers:** Dockerfile, docker-compose
- **CI/CD:** GitHub Actions, Azure Pipelines

## Key Features

### AI-Powered Scaffolding
- Natural language conversation to gather project requirements
- Intelligent clarifying questions for complete specifications
- Automatic technology selection based on requirements

### Multi-Platform Support
- **GitHub** - Full integration with GitHub Actions CI/CD
- **Azure DevOps** - On-premises support with YAML/XAML pipelines
- **Multiple LLM Providers** - Claude (default) or OpenAI-compatible APIs

### Project Templates
- **ASP.NET Core Web API** (.NET 10) - Full REST API with Swagger, xUnit tests
- **Python FastAPI** - Modern async API with pytest, Ruff linting
- **Dart Shelf** - REST API with CRUD endpoints, health checks, Docker

### Developer Experience
- **Theme Support** - Auto/light/dark mode with OS preference detection
- **Session Persistence** - Conversations saved across browser sessions
- **Project History** - Quick access to previously scaffolded projects
- **Dual VS Code Links** - Open in browser (vscode.dev) or clone to desktop

### Enterprise Features
- **10 Enterprise Modules** - Production-ready scaffolding with security, compliance, and ops
- **Architecture Decision Records** - Automatic ADR generation in MADR format
- **Security Baseline** - SECURITY.md, secrets patterns, environment templates
- **SBOM Generation** - Software Bill of Materials for supply chain security
- **License & Compliance** - LICENSE, CONTRIBUTING.md, code ownership
- **Observability Bootstrap** - Health checks, logging, tracing setup
- **Runbooks & Documentation** - Operations guides and troubleshooting docs
- **OIDC Authentication** - SSO with Azure AD, Okta, Auth0, Keycloak
- **Audit Logging** - Full trail of all agent actions
- **Policy Engine** - Organizational policy enforcement

### Reliability
- **Automatic Retry** - Exponential backoff for transient API failures
- **Real-time Updates** - SSE streaming for live scaffolding progress
- **Health Checks** - Built-in health endpoints for monitoring

## Design Principles

1. **Zero-touch delivery** - No manual steps between requirements and working repo
2. **Reproducible output** - Same inputs always produce identical projects
3. **Policy compliance** - All organizational policies evaluated before execution
4. **Full audit trail** - Every action logged with timestamps and outcomes
5. **Platform-agnostic** - Same logic works for GitHub and Azure DevOps

## Deployment

### Quick Deploy with Script

```bash
# Deploy to server (default: host-node-01)
./deploy.sh

# Deploy to specific host
./deploy.sh my-server
```

### Manual Deployment

1. **Clone and configure on server:**
   ```bash
   git clone https://github.com/achildrenmile/crisp.git /opt/crisp
   cd /opt/crisp
   cp .env.example .env
   ```

2. **Generate secrets and edit `.env`:**
   ```bash
   # Generate JWT secret
   openssl rand -base64 32

   # Generate API key
   openssl rand -base64 32 | tr -d '+=/\n' | head -c 32
   ```

3. **Configure `.env` with required values:**
   ```bash
   # Authentication (required)
   JWT_SECRET=<generated-jwt-secret>
   API_KEY=<generated-api-key>

   # Claude API (required)
   CLAUDE_API_KEY=sk-ant-your-key

   # GitHub (required)
   GITHUB_OWNER=your-org-or-username
   GITHUB_TOKEN=ghp_your_token
   ```

4. **Build and start:**
   ```bash
   docker compose up -d --build
   ```

5. **Access the application:**
   - URL: `http://localhost:5000`
   - Login with your configured API key

### Authentication

CRISP supports multiple authentication methods:

#### JWT with API Keys (Default)

1. User enters API key on login page
2. API validates key and issues JWT token
3. Token is used for subsequent requests
4. Token expires after 60 minutes (configurable)

**Environment Variables:**

| Variable | Description | Required |
|----------|-------------|----------|
| `JWT_SECRET` | Secret for signing JWTs (min 32 chars) | Yes |
| `API_KEY` | API key for authentication | Yes |
| `API_KEY_NAME` | Display name for the key | No |
| `AUTH_ENABLED` | Enable/disable auth (default: true) | No |

#### OIDC Authentication (Optional)

CRISP can integrate with external identity providers via OpenID Connect (OIDC). This allows SSO with providers like Azure AD, Okta, Auth0, Keycloak, etc.

**Configuration (appsettings.json):**

```json
{
  "Auth": {
    "Enabled": true,
    "Jwt": {
      "Secret": "your-jwt-secret-min-32-chars",
      "Issuer": "CRISP",
      "Audience": "CRISP-Web",
      "ExpirationMinutes": 60
    },
    "Oidc": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "ResponseType": "code",
      "Scopes": ["openid", "profile", "email"],
      "CallbackPath": "/signin-oidc",
      "SignedOutCallbackPath": "/signout-callback-oidc",
      "SaveTokens": true,
      "GetClaimsFromUserInfoEndpoint": true,
      "Cookie": {
        "Name": ".CRISP.Auth",
        "SameSite": "Lax",
        "SecurePolicy": true,
        "ExpirationMinutes": 60
      }
    }
  }
}
```

**OIDC Configuration Options:**

| Setting | Description | Default |
|---------|-------------|---------|
| `Enabled` | Enable OIDC authentication | `false` |
| `Authority` | OIDC provider URL | (required) |
| `ClientId` | Application client ID | (required) |
| `ClientSecret` | Application client secret | (required) |
| `ResponseType` | OAuth response type | `code` |
| `Scopes` | Requested scopes | `["openid", "profile", "email"]` |
| `CallbackPath` | Callback URL path | `/signin-oidc` |
| `SignedOutCallbackPath` | Signout callback path | `/signout-callback-oidc` |
| `SaveTokens` | Store tokens in auth properties | `true` |
| `GetClaimsFromUserInfoEndpoint` | Fetch claims from userinfo | `true` |
| `Cookie.Name` | Authentication cookie name | `.CRISP.Auth` |
| `Cookie.SameSite` | SameSite cookie policy | `Lax` |
| `Cookie.SecurePolicy` | Require HTTPS for cookies | `true` |
| `Cookie.ExpirationMinutes` | Session cookie expiration | `60` |

**Azure AD Example:**

```json
{
  "Auth": {
    "Oidc": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
      "ClientId": "00000000-0000-0000-0000-000000000000",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

**Keycloak Example:**

```json
{
  "Auth": {
    "Oidc": {
      "Enabled": true,
      "Authority": "https://keycloak.example.com/realms/your-realm",
      "ClientId": "crisp",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

> **Note:** When OIDC is enabled, both cookie-based (OIDC) and JWT Bearer authentication are supported. API calls can use JWT tokens while browser sessions use cookies. API Key authentication remains available for programmatic access.

### Cloudflare Tunnel Setup

1. Create a tunnel in Cloudflare Zero Trust dashboard
2. Configure the public hostname to point to `http://crisp:5000`
3. Add the container to the `cloudflared-tunnel` network

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
- Review API logs: `docker compose logs crisp`

**"401 Unauthorized" on login**
- Verify the API_KEY in .env matches what you're entering
- Check that AUTH_ENABLED is not set to false

### Viewing Logs

```bash
# Follow logs
docker compose logs -f crisp

# Last 100 lines
docker compose logs --tail 100 crisp
```

## License

MIT License - See LICENSE file for details.
