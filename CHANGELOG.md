# Changelog

All notable changes to CRISP will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.8.0] - 2026-02-08

### Added
- **Real-time Action Status Indicators**: Dynamic status messages showing exactly what CRISP is doing during project creation
  - Replaces generic loading dots with descriptive action states
  - Shows contextual icons for each action phase:
    - 🤔 Thinking... (LLM processing)
    - 🔧 Preparing to scaffold...
    - 📋 Creating execution plan...
    - 📁 Generating project files...
    - 🏢 Running enterprise modules...
    - 🚀 Creating remote repository...
    - 📦 Initializing Git repository...
    - ⬆️ Pushing to remote repository...
    - ⚙️ Setting up CI/CD pipeline...
    - ⏳ Waiting for initial build...
    - ✨ Finalizing delivery...
  - Spinning loader animation alongside status text

### Changed
- Removed redundant DeliveryCard component - success message now contains all repository information
- SSE events now use polymorphic JSON serialization for proper derived type handling
- Sidebar automatically refreshes when project creation completes

### Fixed
- **SSE Event Serialization**: Fixed polymorphic event serialization so derived type properties (actionKey, description) are included in JSON
- **Session State Reset**: Fixed delivery result persisting when switching between sessions
- **SSE Parse Errors**: Improved error handling for undefined/empty SSE data

### Technical Details
- New `ActionStatusEvent` SSE event type with `actionKey` and `description` properties
- Added `[JsonDerivedType]` attributes to `AgentEvent` base class for polymorphic serialization
- New `ActionIndicator` React component replaces `TypingIndicator`
- Progress callback pattern in `ICrispAgent.ExecutePlanAsync` for real-time status updates
- Frontend SSE handling now listens for named events via `addEventListener`

## [2.7.0] - 2026-02-08

### Added
- **Responsive Design**: Fully responsive UI that works seamlessly across desktop, tablet (iPad), and mobile devices
  - Tablet breakpoint (1024px) with adjusted spacing and narrower sidebar
  - Mobile breakpoint (768px) with stacked layouts and full-width buttons
  - Extra small device support (380px) for compact screens

### Changed
- Message boxes now use 90% width (up from 80%) for better content display
- Delivery card buttons wrap properly on smaller screens
- Sidebar is hidden on mobile to maximize chat space
- Header hides tagline and username on mobile for cleaner layout

### Fixed
- **Flask Template Syntax**: Fixed Python files generating with invalid syntax
  - Dict literals `{}` now render correctly (was outputting `{{}}`)
  - Docstrings `"""` now render correctly (was outputting escaped quotes)
  - Uses C# raw string literals with 4-quote delimiters for proper embedding
- **Delivery Card Overflow**: Fixed horizontal scrolling issues on delivery cards
  - URLs now break properly with `overflow-wrap: anywhere`
  - Buttons flex and wrap instead of causing overflow
  - Added `overflow-x: hidden` to chat messages container
- **Message Content Overflow**: Fixed long content causing horizontal scrollbars
  - Added proper word-break rules to message content
  - Links break at any character to prevent overflow

### Technical Details
- New CSS breakpoints: 1024px (tablet), 768px (mobile), 380px (small)
- Delivery actions use `flex-wrap: wrap` with `flex: 1 1 auto`
- Message content uses `overflow: hidden` with `word-break: break-word`

## [2.6.0] - 2026-02-08

### Added
- **Enterprise Features Opt-In**: Users are now asked during project creation whether they want enterprise features included
  - Lists all 9 enterprise modules with descriptions when asking
  - Default is `false` (opt-in rather than opt-out)
  - Progress message shows enterprise features status

### Changed
- Enterprise modules now only run when explicitly requested by the user
- Updated system prompt to ask about enterprise features after gathering basic requirements
- Added `IncludeEnterpriseFeatures` property to `ProjectRequirements` model

### Fixed
- **Node.js Version Parsing**: Fixed CI pipeline generators incorrectly outputting `Node.js 20` instead of `20` for the node-version field
  - GitHub Actions workflows now correctly set `node-version: 20`
  - Azure Pipelines now correctly set `versionSpec: 20`
  - Issue affected all JavaScript/TypeScript projects

### Enterprise Features (when enabled)
When users choose to include enterprise features, the following are generated:
| Feature | Description |
|---------|-------------|
| **Security Baseline** | SECURITY.md, .env.example, secrets gitignore |
| **SBOM** | Software Bill of Materials for supply chain compliance |
| **License & Compliance** | LICENSE file, CONTRIBUTING.md guidelines |
| **Code Ownership** | CODEOWNERS file for automatic PR reviewers |
| **Branching Strategy** | Documentation for trunk-based/GitHub flow |
| **Observability** | Health checks, structured logging, tracing stubs |
| **Environment Config** | Environment documentation and config files |
| **API Contract** | OpenAPI spec stub (for API projects) |
| **Runbook** | Deployment and operations documentation |

## [2.5.0] - 2026-02-07

### Added
- **Enterprise Modules**: 10 production-ready modules that run during scaffolding to generate enterprise-grade documentation, configuration, and compliance artifacts

#### Enterprise Modules
| Module | Description |
|--------|-------------|
| **Security Baseline** | Generates `SECURITY.md`, `.env.example`, updates `.gitignore` with secrets patterns |
| **SBOM Configuration** | Software Bill of Materials with CI pipeline integration for supply chain security |
| **License & Compliance** | Generates `LICENSE` and `CONTRIBUTING.md` files |
| **Code Ownership** | Creates `CODEOWNERS` (GitHub) or code ownership config (Azure DevOps) |
| **Branching Strategy** | Documents branching strategy in `docs/BRANCHING.md` |
| **Observability** | Scaffolds health checks, structured logging, and tracing for each language |
| **README Generator** | Generates comprehensive project README with badges, setup instructions, API docs |
| **Environment Config** | Creates environment documentation and language-specific config files |
| **API Contract** | Generates OpenAPI/AsyncAPI specs and Bruno API client collections |
| **Runbook/Operations** | Creates operational runbooks and troubleshooting guides |

#### Module Features
- **ADR Integration** - Each module records its decisions in the ADR system
- **Language-Aware** - Generates language-specific code (C#, Python, TypeScript, Dart)
- **Platform-Aware** - Adapts to GitHub vs Azure DevOps patterns
- **Secrets Manager Support** - Azure Key Vault, AWS Secrets Manager, HashiCorp Vault integration
- **Configurable** - Skip modules, customize templates via configuration

### New Files Generated
- `SECURITY.md` - Security policy and vulnerability reporting
- `.env.example` - Environment variable template
- `LICENSE` - Project license file
- `CONTRIBUTING.md` - Contribution guidelines
- `CODEOWNERS` - Code ownership for GitHub
- `docs/BRANCHING.md` - Branching strategy documentation
- `docs/environments.md` - Environment configuration guide
- `docs/runbook.md` - Operational runbook
- `docs/troubleshooting.md` - Troubleshooting guide
- `openapi.yaml` - OpenAPI specification (for API projects)
- `bruno/` - Bruno API client collection

### Configuration
New `Enterprise` section in appsettings.json:
```json
{
  "Enterprise": {
    "Enabled": true,
    "SkipModules": [],
    "Security": { "ContactEmail": "security@company.com" },
    "License": { "DefaultLicense": "MIT", "CopyrightHolder": "Your Company" },
    "Ownership": { "DefaultOwners": ["@team-lead"] }
  }
}
```

### Technical Details
- New `CRISP.Enterprise` project with modular architecture
- `IEnterpriseModule` interface for consistent module implementation
- `EnterpriseModuleOrchestrator` runs modules in priority order
- `ProjectContext` carries all scaffolding information to modules
- Full test coverage in `CRISP.Enterprise.Tests`
- Integrated into `CrispAgent` orchestration pipeline

## [2.4.0] - 2026-02-07

### Added
- **Architecture Decision Records (ADR) Module**: Automatically generates ADRs for scaffolded projects
  - Documents all architectural decisions made during scaffolding in MADR format
  - Generates `docs/adr/` directory with decision records
  - Creates meta-ADR (0000) explaining the use of ADRs
  - Generates index README.md with table of all decisions
  - Includes blank template for future manual ADRs
  - Records decisions for: language, framework, testing, CI/CD, containerization, SCM, code style

### New Files Generated
- `docs/adr/README.md` - Index with table of all ADRs
- `docs/adr/template.md` - Blank template for future ADRs
- `docs/adr/0000-record-architecture-decisions.md` - Meta ADR
- `docs/adr/XXXX-decision-title.md` - Individual decision records

### Configuration
New `Adr` section in appsettings.json:
- `OutputDirectory` - Output path (default: `docs/adr`)
- `GenerateIndex` - Generate README.md (default: `true`)
- `IncludeTemplate` - Include blank template (default: `true`)
- `GenerateMetaAdr` - Generate ADR-0000 (default: `true`)
- `OrganizationName` - Custom organization name for deciders

### Technical Details
- New `CRISP.Adr` project with full test coverage
- `AdrGenerator` orchestrates ADR file generation
- `AdrTemplateEngine` renders MADR markdown (full and short forms)
- `AdrIndexGenerator` creates the index/README
- `DecisionCollector` gathers decisions during scaffolding
- `DecisionRecorder` records standard decisions from execution plan
- Integrated into `CrispAgent` orchestration pipeline

## [2.3.0] - 2026-02-07

### Added
- **Dart Shelf Template**: New project generator for Dart Shelf REST API framework
  - Full CRUD API scaffold with items endpoint
  - Health check endpoint
  - CORS middleware configuration
  - Proper Dart project structure (bin/, lib/src/, test/)
  - Docker support with multi-stage builds
  - Makefile for common development tasks
  - Unit testing with `dart test`
- **API Retry Logic**: Added automatic retry with exponential backoff for Claude API calls
  - 3 retries with 2s, 5s, 10s delays
  - Handles transient errors: timeouts, rate limits, API overload

### Changed
- Extended `ProjectLanguage` enum with `Dart`
- Extended `ProjectFramework` enum with `DartShelf` and `DartFrog`
- Updated ChatAgent system prompt to include Dart Shelf as a supported framework

### Fixed
- Dart generator C# string escaping issues using raw string literals

## [2.2.0] - 2026-02-06

### Added
- **OIDC Authentication**: Added optional OpenID Connect (OIDC) authentication support for SSO with external identity providers (Azure AD, Okta, Auth0, Keycloak, etc.)
- **Theme Support**: Added auto/light/dark mode toggle in the header with OS preference detection
- Theme context and provider for React components
- Theme persistence in localStorage
- Clickable version link in footer to GitHub releases
- Azure DevOps SCM platform configuration documentation

### Changed
- Improved dark mode colors for better visibility of links and buttons
- Updated CSS to use theme-aware CSS custom properties
- ProjectHistory component updated to support dark mode

### Technical Details
- New `OidcConfiguration` and `OidcCookieConfiguration` classes for OIDC settings
- `OidcEvents.cs` for handling AJAX requests without redirects
- `ThemeContext.tsx` for theme state management
- `ThemeToggle.tsx` component for cycling through themes
- Multiple authentication schemes supported simultaneously (Cookie, JWT Bearer, API Key)
- Cookie-based sessions for OIDC with configurable SameSite policies

## [2.1.0] - 2026-02-06

### Added
- **OpenAI Integration**: Added support for OpenAI and OpenAI-compatible APIs as an alternative LLM provider
- New `ILlmClient` abstraction interface for pluggable LLM providers
- `OpenAiClient` implementation with streaming support
- LLM provider configuration via `Llm:Provider` setting (`Claude` or `OpenAI`)
- New `/api/llm-info` endpoint to query the configured LLM provider and model
- LLM model information displayed in the application footer
- Support for custom base URLs for OpenAI-compatible APIs (local LLMs, Azure OpenAI, etc.)

### Changed
- Refactored `ClaudeClient` to implement the new `ILlmClient` interface
- Renamed internal references from Claude-specific to generic LLM terminology
- Health endpoint now includes LLM provider information

### Technical Details
- Added `OpenAI` NuGet package (v2.1.0) for OpenAI API integration
- New configuration classes: `LlmConfiguration`, `OpenAiOptions`
- Provider selection happens at startup based on configuration
- Frontend fetches LLM info on load and displays in footer with tooltip

## [2.0.0] - 2026-02-06

### Breaking Changes
- **Upgraded to .NET 10**: The entire backend is now built on .NET 10 (from .NET 8)
- Scaffolded ASP.NET Core projects now target .NET 10 by default
- Docker images now use .NET 10 runtime

### Changed
- Updated all Microsoft.Extensions.* packages to version 10.0.0
- Updated ASP.NET Core packages to version 10.0.0
- Updated Serilog packages to latest .NET 10 compatible versions
- Updated test packages (xUnit 2.9.3, FluentAssertions 7.0.0)
- Updated third-party packages to latest versions:
  - Swashbuckle.AspNetCore 7.2.0
  - LibGit2Sharp 0.31.0
  - YamlDotNet 16.3.0
  - Octokit 14.0.0
  - Anthropic.SDK 4.0.0
- Node.js version bumped to 22 in Docker builds
- CI workflows updated to use .NET 10.x
- Pipeline generators (GitHub Actions, Azure Pipelines) now generate .NET 10 configurations

### Technical Details
- Updated `global.json` to require .NET 10 SDK
- Updated `Directory.Build.props` to target `net10.0`
- Updated `Directory.Packages.props` with all new package versions
- Updated `Dockerfile` to use `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`
- Updated all template generators to produce .NET 10 compatible code

## [1.1.0] - 2026-02-06

### Added
- Version display in application footer with link to GitHub repository
- Dual VS Code link support: "Open in Browser" (vscode.dev) and "Clone to Desktop" (vscode:// protocol)
- Styled buttons for VS Code links in chat messages with distinct visual styling
- Session data loading for completed sessions on page refresh
- Backward compatibility migration for existing session data with old VS Code link format

### Fixed
- **VS Code protocol links not rendering**: ReactMarkdown was sanitizing `vscode://` protocol URLs. Added `urlTransform={(url) => url}` to preserve all URL protocols
- **Delivery card not showing for existing sessions**: Added useEffect to load session status and delivery result from API when page loads, not just from SSE events
- **SSE event parsing for delivery_ready and plan_ready**: Fixed event data extraction to properly handle nested `deliveryCard` and `plan` properties
- **Session status parsing**: Fixed handling of numeric status values returned from API
- **Excessive whitespace in migrated message content**: Fixed migration code that was causing markdown to render VS Code links as code blocks due to improper indentation

### Changed
- VS Code button order in DeliveryCard: "Clone to Desktop" is now the primary button, "Open in Browser" is secondary
- Improved VS Code link styling in chat with distinct colors (blue for browser, purple for clone)
- Session persistence migration now properly formats multi-line content with correct indentation

### Technical Details
- Added `urlTransform` prop to ReactMarkdown in `ChatMessage.tsx` to allow custom protocol URLs
- Updated `useSession.ts` hook to load initial session data on mount for completed sessions
- Fixed `SessionPersistence.cs` migration methods to use explicit newlines instead of verbatim strings
- Updated `DeliveryCard.tsx` button layout and CSS classes

## [1.0.0] - Initial Release

### Features
- AI-powered chat interface for describing project requirements
- Automatic project scaffolding with templates:
  - ASP.NET Core Web API
  - Python FastAPI
- GitHub and Azure DevOps integration
- CI/CD pipeline generation (GitHub Actions, Azure Pipelines)
- Docker support for all templates
- Session persistence with automatic recovery
- Real-time updates via Server-Sent Events (SSE)
- Execution plan approval workflow
- Delivery card with repository details and quick actions
- JWT-based authentication with API keys
- Docker Compose deployment support
