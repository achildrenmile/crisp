using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Go Echo projects.
/// </summary>
public sealed class EchoGenerator : IProjectGenerator
{
    private readonly ILogger<EchoGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public EchoGenerator(
        ILogger<EchoGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "go-echo";
    public string TemplateName => "Go Echo";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Go &&
               requirements.Framework == ProjectFramework.Echo;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Go Echo project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var moduleName = $"github.com/example/{requirements.ProjectName}";

        // Create directories
        var handlersPath = Path.Combine(outputPath, "handlers");
        var modelsPath = Path.Combine(outputPath, "models");
        var middlewarePath = Path.Combine(outputPath, "middleware");

        await _filesystem.CreateDirectoryAsync(handlersPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(modelsPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(middlewarePath, cancellationToken);

        // main.go
        var mainContent = GenerateMain(requirements.ProjectName);
        var mainPath = Path.Combine(outputPath, "main.go");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // handlers/item.go
        var handlerContent = GenerateHandler();
        var handlerPath = Path.Combine(handlersPath, "item.go");
        await _filesystem.WriteFileAsync(handlerPath, handlerContent, cancellationToken);
        createdFiles.Add(handlerPath);

        // handlers/health.go
        var healthContent = GenerateHealthHandler(requirements.ProjectName);
        var healthPath = Path.Combine(handlersPath, "health.go");
        await _filesystem.WriteFileAsync(healthPath, healthContent, cancellationToken);
        createdFiles.Add(healthPath);

        // models/item.go
        var modelContent = GenerateModel();
        var modelPath = Path.Combine(modelsPath, "item.go");
        await _filesystem.WriteFileAsync(modelPath, modelContent, cancellationToken);
        createdFiles.Add(modelPath);

        // middleware/logger.go
        var loggerContent = GenerateLoggerMiddleware();
        var loggerPath = Path.Combine(middlewarePath, "logger.go");
        await _filesystem.WriteFileAsync(loggerPath, loggerContent, cancellationToken);
        createdFiles.Add(loggerPath);

        // go.mod
        var goModContent = GenerateGoMod(moduleName);
        var goModPath = Path.Combine(outputPath, "go.mod");
        await _filesystem.WriteFileAsync(goModPath, goModContent, cancellationToken);
        createdFiles.Add(goModPath);

        // .gitignore
        var gitignoreContent = GenerateGitignore(requirements.ProjectName);
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // README.md
        var readmeContent = GenerateReadme(requirements.ProjectName, requirements);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // Makefile
        var makefileContent = GenerateMakefile(requirements.ProjectName);
        var makefilePath = Path.Combine(outputPath, "Makefile");
        await _filesystem.WriteFileAsync(makefilePath, makefileContent, cancellationToken);
        createdFiles.Add(makefilePath);

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testContent = GenerateMainTest();
            var testPath = Path.Combine(outputPath, "main_test.go");
            await _filesystem.WriteFileAsync(testPath, testContent, cancellationToken);
            createdFiles.Add(testPath);
        }

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile(requirements.ProjectName);
            var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
            await _filesystem.WriteFileAsync(dockerfilePath, dockerfileContent, cancellationToken);
            createdFiles.Add(dockerfilePath);

            var dockerComposeContent = GenerateDockerCompose(requirements.ProjectName);
            var dockerComposePath = Path.Combine(outputPath, "docker-compose.yml");
            await _filesystem.WriteFileAsync(dockerComposePath, dockerComposeContent, cancellationToken);
            createdFiles.Add(dockerComposePath);
        }

        _logger.LogInformation("Generated {Count} files", createdFiles.Count);
        return createdFiles;
    }

    public Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var files = new List<PlannedFile>
        {
            new() { RelativePath = "main.go", Description = "Application entry point" },
            new() { RelativePath = "handlers", IsDirectory = true, Description = "HTTP handlers" },
            new() { RelativePath = "handlers/item.go", Description = "Item handlers" },
            new() { RelativePath = "handlers/health.go", Description = "Health handlers" },
            new() { RelativePath = "models", IsDirectory = true, Description = "Data models" },
            new() { RelativePath = "models/item.go", Description = "Item model" },
            new() { RelativePath = "middleware", IsDirectory = true, Description = "Middleware" },
            new() { RelativePath = "middleware/logger.go", Description = "Logger middleware" },
            new() { RelativePath = "go.mod", Description = "Go module file" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = "Makefile", Description = "Build automation" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "main_test.go", Description = "Main tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateMain(string projectName)
    {
        return $@"package main

import (
	""log""
	""net/http""
	""os""

	""github.com/labstack/echo/v4""
	echoMiddleware ""github.com/labstack/echo/v4/middleware""
	""github.com/example/{projectName}/handlers""
	""github.com/example/{projectName}/middleware""
)

func main() {{
	e := echo.New()

	// Middleware
	e.Use(echoMiddleware.Recover())
	e.Use(middleware.Logger())
	e.Use(echoMiddleware.CORS())

	// Routes
	e.GET(""/"", handlers.Root)
	e.GET(""/health"", handlers.HealthCheck)

	// Item routes
	items := e.Group(""/items"")
	items.GET("""", handlers.GetItems)
	items.GET(""/:id"", handlers.GetItem)
	items.POST("""", handlers.CreateItem)
	items.DELETE(""/:id"", handlers.DeleteItem)

	// Start server
	port := os.Getenv(""PORT"")
	if port == """" {{
		port = ""8080""
	}}

	log.Printf(""Server starting on port %s"", port)
	if err := e.Start("":"" + port); err != nil && err != http.ErrServerClosed {{
		log.Fatalf(""Failed to start server: %v"", err)
	}}
}}
";
    }

    private static string GenerateHandler()
    {
        return @"package handlers

import (
	""net/http""
	""strconv""
	""sync""
	""sync/atomic""

	""github.com/labstack/echo/v4""
	""github.com/example/myapp/models""
)

var (
	items     = make(map[int64]*models.Item)
	itemsLock sync.RWMutex
	idCounter int64
)

// GetItems returns all items
func GetItems(c echo.Context) error {
	itemsLock.RLock()
	defer itemsLock.RUnlock()

	result := make([]*models.Item, 0, len(items))
	for _, item := range items {
		result = append(result, item)
	}

	return c.JSON(http.StatusOK, result)
}

// GetItem returns a single item by ID
func GetItem(c echo.Context) error {
	id, err := strconv.ParseInt(c.Param(""id""), 10, 64)
	if err != nil {
		return c.JSON(http.StatusBadRequest, map[string]string{""error"": ""invalid id""})
	}

	itemsLock.RLock()
	item, exists := items[id]
	itemsLock.RUnlock()

	if !exists {
		return c.JSON(http.StatusNotFound, map[string]string{""error"": ""item not found""})
	}

	return c.JSON(http.StatusOK, item)
}

// CreateItem creates a new item
func CreateItem(c echo.Context) error {
	var input models.CreateItemInput
	if err := c.Bind(&input); err != nil {
		return c.JSON(http.StatusBadRequest, map[string]string{""error"": err.Error()})
	}

	if input.Name == """" || input.Price <= 0 {
		return c.JSON(http.StatusBadRequest, map[string]string{""error"": ""name and price are required""})
	}

	id := atomic.AddInt64(&idCounter, 1)
	item := &models.Item{
		ID:          id,
		Name:        input.Name,
		Description: input.Description,
		Price:       input.Price,
	}

	itemsLock.Lock()
	items[id] = item
	itemsLock.Unlock()

	return c.JSON(http.StatusCreated, item)
}

// DeleteItem deletes an item by ID
func DeleteItem(c echo.Context) error {
	id, err := strconv.ParseInt(c.Param(""id""), 10, 64)
	if err != nil {
		return c.JSON(http.StatusBadRequest, map[string]string{""error"": ""invalid id""})
	}

	itemsLock.Lock()
	_, exists := items[id]
	if exists {
		delete(items, id)
	}
	itemsLock.Unlock()

	if !exists {
		return c.JSON(http.StatusNotFound, map[string]string{""error"": ""item not found""})
	}

	return c.NoContent(http.StatusNoContent)
}
";
    }

    private static string GenerateHealthHandler(string projectName)
    {
        return $@"package handlers

import (
	""net/http""

	""github.com/labstack/echo/v4""
)

// Root returns welcome message
func Root(c echo.Context) error {{
	return c.JSON(http.StatusOK, map[string]string{{
		""message"": ""Welcome to {projectName}"",
		""version"": ""1.0.0"",
	}})
}}

// HealthCheck returns health status
func HealthCheck(c echo.Context) error {{
	return c.JSON(http.StatusOK, map[string]string{{
		""status"": ""healthy"",
	}})
}}
";
    }

    private static string GenerateModel()
    {
        return @"package models

// Item represents an item in the system
type Item struct {
	ID          int64   `json:""id""`
	Name        string  `json:""name""`
	Description string  `json:""description,omitempty""`
	Price       float64 `json:""price""`
}

// CreateItemInput is the input for creating an item
type CreateItemInput struct {
	Name        string  `json:""name""`
	Description string  `json:""description""`
	Price       float64 `json:""price""`
}
";
    }

    private static string GenerateLoggerMiddleware()
    {
        return @"package middleware

import (
	""log""
	""time""

	""github.com/labstack/echo/v4""
)

// Logger returns a middleware that logs requests
func Logger() echo.MiddlewareFunc {
	return func(next echo.HandlerFunc) echo.HandlerFunc {
		return func(c echo.Context) error {
			start := time.Now()

			err := next(c)

			latency := time.Since(start)
			method := c.Request().Method
			path := c.Request().URL.Path
			status := c.Response().Status

			log.Printf(""%s %s %d %v"", method, path, status, latency)

			return err
		}
	}
}
";
    }

    private static string GenerateGoMod(string moduleName)
    {
        return $@"module {moduleName}

go 1.22

require github.com/labstack/echo/v4 v4.11.4
";
    }

    private static string GenerateGitignore(string projectName)
    {
        return $@"# Binaries
{projectName}
*.exe
*.exe~
*.dll
*.so
*.dylib

# Test binary
*.test

# Output of the go coverage tool
*.out

# Dependency directories
vendor/

# IDE
.idea/
.vscode/
*.swp

# OS
.DS_Store

# Environment
.env
.env.local
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Go Echo REST API application."}

## Getting Started

### Prerequisites

- Go 1.22+

### Installation

```bash
go mod download
```

### Run

```bash
go run main.go
```

Or with make:

```bash
make run
```

The API will be available at `http://localhost:8080`.

### Build

```bash
make build
```

### Test

```bash
make test
```

## API Endpoints

- `GET /` - Welcome message
- `GET /health` - Health check
- `GET /items` - List all items
- `GET /items/:id` - Get item by ID
- `POST /items` - Create item
- `DELETE /items/:id` - Delete item

## Project Structure

```
{projectName}/
├── handlers/
│   ├── health.go
│   └── item.go
├── models/
│   └── item.go
├── middleware/
│   └── logger.go
├── main.go
├── main_test.go
├── go.mod
├── Makefile
└── README.md
```

## Features

- Echo v4 high-performance HTTP framework
- Custom middleware support
- CORS enabled
- Structured JSON responses
- Request logging

## License

MIT
";
    }

    private static string GenerateMakefile(string projectName)
    {
        return $@".PHONY: build run test clean

build:
	go build -o {projectName} .

run:
	go run main.go

test:
	go test -v ./...

clean:
	rm -f {projectName}

lint:
	golangci-lint run

docker-build:
	docker build -t {projectName} .
";
    }

    private static string GenerateMainTest()
    {
        return @"package main

import (
	""encoding/json""
	""net/http""
	""net/http/httptest""
	""testing""

	""github.com/labstack/echo/v4""
	""github.com/example/myapp/handlers""
)

func TestHealthEndpoint(t *testing.T) {
	e := echo.New()
	req := httptest.NewRequest(http.MethodGet, ""/health"", nil)
	rec := httptest.NewRecorder()
	c := e.NewContext(req, rec)

	if err := handlers.HealthCheck(c); err != nil {
		t.Errorf(""Handler returned error: %v"", err)
	}

	if rec.Code != http.StatusOK {
		t.Errorf(""Expected status %d, got %d"", http.StatusOK, rec.Code)
	}

	var response map[string]string
	json.Unmarshal(rec.Body.Bytes(), &response)

	if response[""status""] != ""healthy"" {
		t.Errorf(""Expected status 'healthy', got '%s'"", response[""status""])
	}
}

func TestRootEndpoint(t *testing.T) {
	e := echo.New()
	req := httptest.NewRequest(http.MethodGet, ""/"", nil)
	rec := httptest.NewRecorder()
	c := e.NewContext(req, rec)

	if err := handlers.Root(c); err != nil {
		t.Errorf(""Handler returned error: %v"", err)
	}

	if rec.Code != http.StatusOK {
		t.Errorf(""Expected status %d, got %d"", http.StatusOK, rec.Code)
	}
}
";
    }

    private static string GenerateDockerfile(string projectName)
    {
        return $@"FROM golang:1.22-alpine AS builder

WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o {projectName} .

FROM alpine:latest

RUN apk --no-cache add ca-certificates
WORKDIR /app
COPY --from=builder /app/{projectName} .

RUN adduser -D -g '' appuser
USER appuser

EXPOSE 8080
CMD [""./{projectName}""]
";
    }

    private static string GenerateDockerCompose(string projectName)
    {
        return $@"version: '3.8'

services:
  app:
    build: .
    container_name: {projectName}
    ports:
      - ""8080:8080""
    environment:
      - PORT=8080
";
    }
}
