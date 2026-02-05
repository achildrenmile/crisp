using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Python FastAPI projects.
/// </summary>
public sealed class FastApiGenerator : IProjectGenerator
{
    private readonly ILogger<FastApiGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public FastApiGenerator(
        ILogger<FastApiGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "python-fastapi";
    public string TemplateName => "Python FastAPI";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Python &&
               requirements.Framework == ProjectFramework.FastApi;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating FastAPI project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName.Replace("-", "_");

        // Create app directory
        var appPath = Path.Combine(outputPath, "app");
        await _filesystem.CreateDirectoryAsync(appPath, cancellationToken);

        // Create main.py
        var mainContent = GenerateMain(projectName);
        var mainPath = Path.Combine(appPath, "main.py");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // Create __init__.py
        var initPath = Path.Combine(appPath, "__init__.py");
        await _filesystem.WriteFileAsync(initPath, "", cancellationToken);
        createdFiles.Add(initPath);

        // Create routers directory
        var routersPath = Path.Combine(appPath, "routers");
        await _filesystem.CreateDirectoryAsync(routersPath, cancellationToken);

        var routersInitPath = Path.Combine(routersPath, "__init__.py");
        await _filesystem.WriteFileAsync(routersInitPath, "", cancellationToken);
        createdFiles.Add(routersInitPath);

        var itemsRouterContent = GenerateItemsRouter();
        var itemsRouterPath = Path.Combine(routersPath, "items.py");
        await _filesystem.WriteFileAsync(itemsRouterPath, itemsRouterContent, cancellationToken);
        createdFiles.Add(itemsRouterPath);

        // Create models directory
        var modelsPath = Path.Combine(appPath, "models");
        await _filesystem.CreateDirectoryAsync(modelsPath, cancellationToken);

        var modelsInitPath = Path.Combine(modelsPath, "__init__.py");
        await _filesystem.WriteFileAsync(modelsInitPath, "", cancellationToken);
        createdFiles.Add(modelsInitPath);

        var itemModelContent = GenerateItemModel();
        var itemModelPath = Path.Combine(modelsPath, "item.py");
        await _filesystem.WriteFileAsync(itemModelPath, itemModelContent, cancellationToken);
        createdFiles.Add(itemModelPath);

        // Create requirements.txt
        var requirementsContent = GenerateRequirements(requirements);
        var requirementsPath = Path.Combine(outputPath, "requirements.txt");
        await _filesystem.WriteFileAsync(requirementsPath, requirementsContent, cancellationToken);
        createdFiles.Add(requirementsPath);

        // Create pyproject.toml
        var pyprojectContent = GeneratePyproject(requirements.ProjectName);
        var pyprojectPath = Path.Combine(outputPath, "pyproject.toml");
        await _filesystem.WriteFileAsync(pyprojectPath, pyprojectContent, cancellationToken);
        createdFiles.Add(pyprojectPath);

        // Create .gitignore
        var gitignoreContent = GenerateGitignore();
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // Create README.md
        var readmeContent = GenerateReadme(requirements.ProjectName, requirements);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // Create tests if requested
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testsPath = Path.Combine(outputPath, "tests");
            await _filesystem.CreateDirectoryAsync(testsPath, cancellationToken);

            var testsInitPath = Path.Combine(testsPath, "__init__.py");
            await _filesystem.WriteFileAsync(testsInitPath, "", cancellationToken);
            createdFiles.Add(testsInitPath);

            var testMainContent = GenerateTestMain();
            var testMainPath = Path.Combine(testsPath, "test_main.py");
            await _filesystem.WriteFileAsync(testMainPath, testMainContent, cancellationToken);
            createdFiles.Add(testMainPath);
        }

        // Create Dockerfile if container support requested
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile();
            var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
            await _filesystem.WriteFileAsync(dockerfilePath, dockerfileContent, cancellationToken);
            createdFiles.Add(dockerfilePath);

            var dockerComposeContent = GenerateDockerCompose(requirements.ProjectName);
            var dockerComposePath = Path.Combine(outputPath, "docker-compose.yml");
            await _filesystem.WriteFileAsync(dockerComposePath, dockerComposeContent, cancellationToken);
            createdFiles.Add(dockerComposePath);
        }

        // Create ruff.toml if Ruff linting requested
        if (requirements.LintingTools.Contains("Ruff"))
        {
            var ruffContent = GenerateRuffConfig();
            var ruffPath = Path.Combine(outputPath, "ruff.toml");
            await _filesystem.WriteFileAsync(ruffPath, ruffContent, cancellationToken);
            createdFiles.Add(ruffPath);
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
            new() { RelativePath = "app", IsDirectory = true, Description = "Application package" },
            new() { RelativePath = "app/__init__.py", Description = "Package init" },
            new() { RelativePath = "app/main.py", Description = "FastAPI application entry point" },
            new() { RelativePath = "app/routers", IsDirectory = true, Description = "API routers" },
            new() { RelativePath = "app/routers/__init__.py", Description = "Routers package init" },
            new() { RelativePath = "app/routers/items.py", Description = "Items router" },
            new() { RelativePath = "app/models", IsDirectory = true, Description = "Pydantic models" },
            new() { RelativePath = "app/models/__init__.py", Description = "Models package init" },
            new() { RelativePath = "app/models/item.py", Description = "Item model" },
            new() { RelativePath = "requirements.txt", Description = "Python dependencies" },
            new() { RelativePath = "pyproject.toml", Description = "Project configuration" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "tests", IsDirectory = true, Description = "Test directory" });
            files.Add(new PlannedFile { RelativePath = "tests/__init__.py", Description = "Tests package init" });
            files.Add(new PlannedFile { RelativePath = "tests/test_main.py", Description = "Main tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        if (requirements.LintingTools.Contains("Ruff"))
        {
            files.Add(new PlannedFile { RelativePath = "ruff.toml", Description = "Ruff linter configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateMain(string projectName)
    {
        return $@"""""""
{projectName} - FastAPI Application
""""""
from fastapi import FastAPI
from app.routers import items

app = FastAPI(
    title=""{projectName}"",
    description=""A FastAPI application"",
    version=""0.1.0"",
)

app.include_router(items.router, prefix=""/items"", tags=[""items""])


@app.get(""/"")
async def root():
    """"""Root endpoint.""""""
    return {{""message"": ""Welcome to {projectName}""}}


@app.get(""/health"")
async def health_check():
    """"""Health check endpoint.""""""
    return {{""status"": ""healthy""}}
";
    }

    private static string GenerateItemsRouter()
    {
        return @"""""""Items router.""""""
from fastapi import APIRouter, HTTPException
from app.models.item import Item, ItemCreate

router = APIRouter()

# In-memory storage for demo
items_db: dict[int, Item] = {}
current_id = 0


@router.get(""/"")
async def list_items() -> list[Item]:
    """"""List all items.""""""
    return list(items_db.values())


@router.get(""/{item_id}"")
async def get_item(item_id: int) -> Item:
    """"""Get a specific item by ID.""""""
    if item_id not in items_db:
        raise HTTPException(status_code=404, detail=""Item not found"")
    return items_db[item_id]


@router.post(""/"", status_code=201)
async def create_item(item: ItemCreate) -> Item:
    """"""Create a new item.""""""
    global current_id
    current_id += 1
    new_item = Item(id=current_id, **item.model_dump())
    items_db[current_id] = new_item
    return new_item


@router.delete(""/{item_id}"", status_code=204)
async def delete_item(item_id: int) -> None:
    """"""Delete an item.""""""
    if item_id not in items_db:
        raise HTTPException(status_code=404, detail=""Item not found"")
    del items_db[item_id]
";
    }

    private static string GenerateItemModel()
    {
        return @"""""""Item models.""""""
from pydantic import BaseModel


class ItemBase(BaseModel):
    """"""Base item model.""""""

    name: str
    description: str | None = None
    price: float


class ItemCreate(ItemBase):
    """"""Model for creating items.""""""

    pass


class Item(ItemBase):
    """"""Item model with ID.""""""

    id: int

    model_config = {""from_attributes"": True}
";
    }

    private static string GenerateRequirements(ProjectRequirements requirements)
    {
        var packages = new List<string>
        {
            "fastapi>=0.109.0",
            "uvicorn[standard]>=0.27.0",
            "pydantic>=2.5.0"
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            packages.Add("pytest>=8.0.0");
            packages.Add("pytest-asyncio>=0.23.0");
            packages.Add("httpx>=0.26.0");
        }

        if (requirements.LintingTools.Contains("Ruff"))
        {
            packages.Add("ruff>=0.2.0");
        }

        return string.Join("\n", packages) + "\n";
    }

    private static string GeneratePyproject(string projectName)
    {
        return $@"[project]
name = ""{projectName}""
version = ""0.1.0""
description = ""A FastAPI application""
readme = ""README.md""
requires-python = "">=3.12""

[tool.pytest.ini_options]
asyncio_mode = ""auto""
testpaths = [""tests""]

[tool.ruff]
line-length = 100
target-version = ""py312""

[tool.ruff.lint]
select = [""E"", ""F"", ""I"", ""N"", ""W"", ""UP""]
";
    }

    private static string GenerateGitignore()
    {
        return @"# Byte-compiled / optimized / DLL files
__pycache__/
*.py[cod]
*$py.class

# C extensions
*.so

# Distribution / packaging
.Python
build/
develop-eggs/
dist/
downloads/
eggs/
.eggs/
lib/
lib64/
parts/
sdist/
var/
wheels/
*.egg-info/
.installed.cfg
*.egg

# PyInstaller
*.manifest
*.spec

# Installer logs
pip-log.txt
pip-delete-this-directory.txt

# Unit test / coverage reports
htmlcov/
.tox/
.nox/
.coverage
.coverage.*
.cache
nosetests.xml
coverage.xml
*.cover
*.py,cover
.hypothesis/
.pytest_cache/

# Environments
.env
.venv
env/
venv/
ENV/
env.bak/
venv.bak/

# IDE
.idea/
.vscode/
*.swp
*.swo

# mypy
.mypy_cache/
.dmypy.json
dmypy.json

# ruff
.ruff_cache/
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A FastAPI application."}

## Getting Started

### Prerequisites

- Python 3.12+
- pip

### Installation

1. Create a virtual environment:
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

### Running the Application

```bash
uvicorn app.main:app --reload
```

The API will be available at `http://localhost:8000`.

### API Documentation

- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

### Running Tests

```bash
pytest
```

## Project Structure

```
{projectName}/
├── app/
│   ├── __init__.py
│   ├── main.py
│   ├── models/
│   │   ├── __init__.py
│   │   └── item.py
│   └── routers/
│       ├── __init__.py
│       └── items.py
├── tests/
│   ├── __init__.py
│   └── test_main.py
├── requirements.txt
├── pyproject.toml
└── README.md
```

## License

This project is licensed under the MIT License.
";
    }

    private static string GenerateTestMain()
    {
        return @"""""""Tests for main application.""""""
import pytest
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)


def test_root():
    """"""Test root endpoint.""""""
    response = client.get(""/"")
    assert response.status_code == 200
    assert ""message"" in response.json()


def test_health_check():
    """"""Test health check endpoint.""""""
    response = client.get(""/health"")
    assert response.status_code == 200
    assert response.json() == {""status"": ""healthy""}


def test_create_and_get_item():
    """"""Test creating and retrieving an item.""""""
    # Create item
    item_data = {""name"": ""Test Item"", ""price"": 9.99}
    response = client.post(""/items/"", json=item_data)
    assert response.status_code == 201
    created_item = response.json()
    assert created_item[""name""] == item_data[""name""]
    assert created_item[""price""] == item_data[""price""]
    assert ""id"" in created_item

    # Get item
    item_id = created_item[""id""]
    response = client.get(f""/items/{item_id}"")
    assert response.status_code == 200
    assert response.json()[""id""] == item_id


def test_get_nonexistent_item():
    """"""Test getting a nonexistent item.""""""
    response = client.get(""/items/99999"")
    assert response.status_code == 404
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM python:3.12-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . .

# Run the application
EXPOSE 8000
CMD [""uvicorn"", ""app.main:app"", ""--host"", ""0.0.0.0"", ""--port"", ""8000""]
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
      - ""8000:8000""
    environment:
      - ENVIRONMENT=development
    volumes:
      - .:/app
    command: uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
";
    }

    private static string GenerateRuffConfig()
    {
        return @"# Ruff configuration
line-length = 100
target-version = ""py312""

[lint]
select = [
    ""E"",   # pycodestyle errors
    ""F"",   # Pyflakes
    ""I"",   # isort
    ""N"",   # pep8-naming
    ""W"",   # pycodestyle warnings
    ""UP"",  # pyupgrade
    ""B"",   # flake8-bugbear
    ""C4"",  # flake8-comprehensions
    ""SIM"", # flake8-simplify
]
ignore = []

[lint.isort]
known-first-party = [""app""]
";
    }
}
