# Changelog

All notable changes to CRISP will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
  - ASP.NET Core 8 Web API
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
