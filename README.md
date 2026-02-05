# CRISP — Code Repo Initialization & Scaffolding Platform

CRISP is an AI-powered autonomous agent that transforms developer requirements into fully configured, ready-to-use code repositories with zero manual steps.

## Overview

CRISP accepts developer requirements (via chat or structured form), validates the input, selects appropriate templates, scaffolds the project, creates a remote repository, sets up CI/CD pipelines, and delivers a working repository link with an "Open in VS Code" option.

### Supported Platforms

| Platform | Status | Description |
|----------|--------|-------------|
| **GitHub** | Primary | Default platform with full GitHub Actions support |
| **Azure DevOps Server** | Alternative | On-premises Azure DevOps with YAML/XAML pipelines |

## Architecture

CRISP is built entirely in .NET 8 and follows the Model Context Protocol (MCP) for tool communication.

```
┌──────────────────────────────────────────────────────────────────┐
│                        CRISP.Agent                                │
│                    (Main Orchestrator)                            │
├──────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ CRISP.Core  │  │CRISP.Audit  │  │ CRISP.Git   │              │
│  │  (Models)   │  │ (Logging)   │  │(LibGit2Sharp)│             │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│  ┌─────────────┐  ┌──────────────────────────────┐              │
│  │  CRISP.     │  │       CRISP.Pipelines        │              │
│  │  Templates  │  │  (GitHub Actions / Azure)    │              │
│  │ (Scriban)   │  └──────────────────────────────┘              │
│  └─────────────┘                                                 │
├──────────────────────────────────────────────────────────────────┤
│                        MCP Servers                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Mcp.GitHub   │  │Mcp.AzureDevOps│  │Mcp.Filesystem│         │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Mcp.Git    │  │Mcp.AuditLog  │  │Mcp.PolicyEngine│        │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└──────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
crisp/
├── src/
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
│   └── CRISP.Mcp.PolicyEngine/  # Policy enforcement MCP server
├── tests/
│   ├── CRISP.Core.Tests/
│   └── CRISP.Pipelines.Tests/
├── .github/workflows/ci.yml     # GitHub Actions CI
├── Directory.Build.props        # Central MSBuild configuration
├── Directory.Packages.props     # Central package management
├── global.json                  # .NET SDK version
└── CRISP.sln                    # Solution file
```

## Component Details

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

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Building

```bash
# Clone the repository
git clone https://github.com/achildrenmile/crisp.git
cd crisp

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

### Configuration

Configure CRISP via `appsettings.json` or environment variables:

```json
{
  "Crisp": {
    "ScmPlatform": "GitHub",
    "GitHub": {
      "Owner": "your-username-or-org",
      "Visibility": "Private",
      "Token": "ghp_your_token"
    },
    "Common": {
      "DefaultBranch": "main",
      "GenerateCiCd": true
    }
  }
}
```

Or via environment variables:
```bash
export CRISP_Crisp__ScmPlatform=GitHub
export CRISP_Crisp__GitHub__Owner=your-org
export CRISP_Crisp__GitHub__Token=ghp_xxx
```

### Running the Agent

```bash
cd src/CRISP.Agent
dotnet run
```

## Supported Technologies

### Languages & Frameworks

| Language | Frameworks | Template |
|----------|-----------|----------|
| C# / .NET 8 | ASP.NET Core Web API | `aspnetcore-webapi` |
| Python 3.12 | FastAPI | `python-fastapi` |

### Additional tooling (configurable)

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
