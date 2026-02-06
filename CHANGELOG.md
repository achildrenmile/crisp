# Changelog

All notable changes to CRISP will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
