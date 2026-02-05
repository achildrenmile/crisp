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
├── Directory.Build.props        # Central MSBuild configuration
├── Directory.Packages.props     # Central package management
├── global.json                  # .NET SDK version
└── CRISP.sln                    # Solution file
```

## Getting Started

### Prerequisites

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

## License

MIT License - See LICENSE file for details.
