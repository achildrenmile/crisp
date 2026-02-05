using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Go Gin projects.
/// </summary>
public sealed class GinGenerator : IProjectGenerator
{
    private readonly ILogger<GinGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public GinGenerator(
        ILogger<GinGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "go-gin";
    public string TemplateName => "Go Gin";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Go &&
               requirements.Framework == ProjectFramework.GinGonic;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Go Gin project: {ProjectName}", requirements.ProjectName);

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

	""github.com/gin-gonic/gin""
	""github.com/example/{projectName}/handlers""
	""github.com/example/{projectName}/middleware""
)

func main() {{
	// Set Gin mode
	if os.Getenv(""GIN_MODE"") == """" {{
		gin.SetMode(gin.DebugMode)
	}}

	router := gin.New()

	// Middleware
	router.Use(gin.Recovery())
	router.Use(middleware.Logger())

	// Routes
	router.GET(""/"", func(c *gin.Context) {{
		c.JSON(http.StatusOK, gin.H{{
			""message"": ""Welcome to {projectName}"",
		}})
	}})

	router.GET(""/health"", func(c *gin.Context) {{
		c.JSON(http.StatusOK, gin.H{{
			""status"": ""healthy"",
		}})
	}})

	// Item routes
	items := router.Group(""/items"")
	{{
		items.GET("""", handlers.GetItems)
		items.GET(""/:id"", handlers.GetItem)
		items.POST("""", handlers.CreateItem)
		items.DELETE(""/:id"", handlers.DeleteItem)
	}}

	// Start server
	port := os.Getenv(""PORT"")
	if port == """" {{
		port = ""8080""
	}}

	log.Printf(""Server starting on port %s"", port)
	if err := router.Run("":"" + port); err != nil {{
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

	""github.com/gin-gonic/gin""
	""github.com/example/myapp/models""
)

var (
	items     = make(map[int64]*models.Item)
	itemsLock sync.RWMutex
	idCounter int64
)

// GetItems returns all items
func GetItems(c *gin.Context) {
	itemsLock.RLock()
	defer itemsLock.RUnlock()

	result := make([]*models.Item, 0, len(items))
	for _, item := range items {
		result = append(result, item)
	}

	c.JSON(http.StatusOK, result)
}

// GetItem returns a single item by ID
func GetItem(c *gin.Context) {
	id, err := strconv.ParseInt(c.Param(""id""), 10, 64)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{""error"": ""invalid id""})
		return
	}

	itemsLock.RLock()
	item, exists := items[id]
	itemsLock.RUnlock()

	if !exists {
		c.JSON(http.StatusNotFound, gin.H{""error"": ""item not found""})
		return
	}

	c.JSON(http.StatusOK, item)
}

// CreateItem creates a new item
func CreateItem(c *gin.Context) {
	var input models.CreateItemInput
	if err := c.ShouldBindJSON(&input); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{""error"": err.Error()})
		return
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

	c.JSON(http.StatusCreated, item)
}

// DeleteItem deletes an item by ID
func DeleteItem(c *gin.Context) {
	id, err := strconv.ParseInt(c.Param(""id""), 10, 64)
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{""error"": ""invalid id""})
		return
	}

	itemsLock.Lock()
	_, exists := items[id]
	if exists {
		delete(items, id)
	}
	itemsLock.Unlock()

	if !exists {
		c.JSON(http.StatusNotFound, gin.H{""error"": ""item not found""})
		return
	}

	c.Status(http.StatusNoContent)
}
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
	Name        string  `json:""name"" binding:""required""`
	Description string  `json:""description""`
	Price       float64 `json:""price"" binding:""required,gt=0""`
}
";
    }

    private static string GenerateLoggerMiddleware()
    {
        return @"package middleware

import (
	""log""
	""time""

	""github.com/gin-gonic/gin""
)

// Logger returns a middleware that logs requests
func Logger() gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()
		path := c.Request.URL.Path
		method := c.Request.Method

		c.Next()

		latency := time.Since(start)
		status := c.Writer.Status()

		log.Printf(""%s %s %d %v"", method, path, status, latency)
	}
}
";
    }

    private static string GenerateGoMod(string moduleName)
    {
        return $@"module {moduleName}

go 1.22

require github.com/gin-gonic/gin v1.9.1
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

{requirements.Description ?? "A Go Gin REST API application."}

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

	""github.com/gin-gonic/gin""
)

func setupRouter() *gin.Engine {
	gin.SetMode(gin.TestMode)
	router := gin.New()

	router.GET(""/"", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{""message"": ""Welcome""})
	})

	router.GET(""/health"", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{""status"": ""healthy""})
	})

	return router
}

func TestHealthEndpoint(t *testing.T) {
	router := setupRouter()

	w := httptest.NewRecorder()
	req, _ := http.NewRequest(""GET"", ""/health"", nil)
	router.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Errorf(""Expected status %d, got %d"", http.StatusOK, w.Code)
	}

	var response map[string]string
	json.Unmarshal(w.Body.Bytes(), &response)

	if response[""status""] != ""healthy"" {
		t.Errorf(""Expected status 'healthy', got '%s'"", response[""status""])
	}
}

func TestRootEndpoint(t *testing.T) {
	router := setupRouter()

	w := httptest.NewRecorder()
	req, _ := http.NewRequest(""GET"", ""/"", nil)
	router.ServeHTTP(w, req)

	if w.Code != http.StatusOK {
		t.Errorf(""Expected status %d, got %d"", http.StatusOK, w.Code)
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
      - GIN_MODE=release
      - PORT=8080
";
    }
}
