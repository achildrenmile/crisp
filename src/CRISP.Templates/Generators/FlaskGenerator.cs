using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Python Flask projects.
/// </summary>
public sealed class FlaskGenerator : IProjectGenerator
{
    private readonly ILogger<FlaskGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public FlaskGenerator(
        ILogger<FlaskGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "python-flask";
    public string TemplateName => "Python Flask";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Python &&
               requirements.Framework == ProjectFramework.Flask;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Flask project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName.Replace("-", "_");

        // Create directory structure
        var appPath = Path.Combine(outputPath, "app");
        var routesPath = Path.Combine(appPath, "routes");
        var modelsPath = Path.Combine(appPath, "models");

        await _filesystem.CreateDirectoryAsync(appPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(routesPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(modelsPath, cancellationToken);

        // app/__init__.py
        var appInitContent = GenerateAppInit(projectName);
        var appInitPath = Path.Combine(appPath, "__init__.py");
        await _filesystem.WriteFileAsync(appInitPath, appInitContent, cancellationToken);
        createdFiles.Add(appInitPath);

        // app/routes/__init__.py
        var routesInitContent = GenerateRoutesInit();
        var routesInitPath = Path.Combine(routesPath, "__init__.py");
        await _filesystem.WriteFileAsync(routesInitPath, routesInitContent, cancellationToken);
        createdFiles.Add(routesInitPath);

        // app/routes/items.py
        var itemsRouteContent = GenerateItemsRoute();
        var itemsRoutePath = Path.Combine(routesPath, "items.py");
        await _filesystem.WriteFileAsync(itemsRoutePath, itemsRouteContent, cancellationToken);
        createdFiles.Add(itemsRoutePath);

        // app/models/__init__.py
        var modelsInitPath = Path.Combine(modelsPath, "__init__.py");
        await _filesystem.WriteFileAsync(modelsInitPath, "", cancellationToken);
        createdFiles.Add(modelsInitPath);

        // app/models/item.py
        var itemModelContent = GenerateItemModel();
        var itemModelPath = Path.Combine(modelsPath, "item.py");
        await _filesystem.WriteFileAsync(itemModelPath, itemModelContent, cancellationToken);
        createdFiles.Add(itemModelPath);

        // app/config.py
        var configContent = GenerateConfig();
        var configPath = Path.Combine(appPath, "config.py");
        await _filesystem.WriteFileAsync(configPath, configContent, cancellationToken);
        createdFiles.Add(configPath);

        // run.py
        var runContent = GenerateRun();
        var runPath = Path.Combine(outputPath, "run.py");
        await _filesystem.WriteFileAsync(runPath, runContent, cancellationToken);
        createdFiles.Add(runPath);

        // requirements.txt
        var requirementsContent = GenerateRequirements(requirements);
        var requirementsPath = Path.Combine(outputPath, "requirements.txt");
        await _filesystem.WriteFileAsync(requirementsPath, requirementsContent, cancellationToken);
        createdFiles.Add(requirementsPath);

        // .gitignore
        var gitignoreContent = GenerateGitignore();
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // README.md
        var readmeContent = GenerateReadme(requirements.ProjectName, requirements);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // .env.example
        var envContent = GenerateEnvExample();
        var envPath = Path.Combine(outputPath, ".env.example");
        await _filesystem.WriteFileAsync(envPath, envContent, cancellationToken);
        createdFiles.Add(envPath);

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testsPath = Path.Combine(outputPath, "tests");
            await _filesystem.CreateDirectoryAsync(testsPath, cancellationToken);

            var testsInitPath = Path.Combine(testsPath, "__init__.py");
            await _filesystem.WriteFileAsync(testsInitPath, "", cancellationToken);
            createdFiles.Add(testsInitPath);

            var confTestContent = GenerateConftest();
            var confTestPath = Path.Combine(testsPath, "conftest.py");
            await _filesystem.WriteFileAsync(confTestPath, confTestContent, cancellationToken);
            createdFiles.Add(confTestPath);

            var testAppContent = GenerateTestApp();
            var testAppPath = Path.Combine(testsPath, "test_app.py");
            await _filesystem.WriteFileAsync(testAppPath, testAppContent, cancellationToken);
            createdFiles.Add(testAppPath);
        }

        // Docker support
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
            new() { RelativePath = "app/__init__.py", Description = "Flask app factory" },
            new() { RelativePath = "app/config.py", Description = "Configuration" },
            new() { RelativePath = "app/routes", IsDirectory = true, Description = "Route blueprints" },
            new() { RelativePath = "app/routes/__init__.py", Description = "Routes init" },
            new() { RelativePath = "app/routes/items.py", Description = "Items routes" },
            new() { RelativePath = "app/models", IsDirectory = true, Description = "Data models" },
            new() { RelativePath = "app/models/__init__.py", Description = "Models init" },
            new() { RelativePath = "app/models/item.py", Description = "Item model" },
            new() { RelativePath = "run.py", Description = "Application entry point" },
            new() { RelativePath = "requirements.txt", Description = "Python dependencies" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = ".env.example", Description = "Environment template" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "tests", IsDirectory = true, Description = "Test files" });
            files.Add(new PlannedFile { RelativePath = "tests/__init__.py", Description = "Tests package init" });
            files.Add(new PlannedFile { RelativePath = "tests/conftest.py", Description = "Pytest fixtures" });
            files.Add(new PlannedFile { RelativePath = "tests/test_app.py", Description = "Application tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateAppInit(string projectName)
    {
        return $@"\"\"\"Flask application factory.\"\"\"
from flask import Flask, jsonify

from .config import Config


def create_app(config_class=Config):
    \"\"\"Create and configure the Flask application.\"\"\"
    app = Flask(__name__)
    app.config.from_object(config_class)

    # Register blueprints
    from app.routes import items_bp
    app.register_blueprint(items_bp, url_prefix='/items')

    # Root endpoint
    @app.route('/')
    def index():
        return jsonify({{
            'message': 'Welcome to {projectName}',
            'version': '1.0.0'
        }})

    # Health check
    @app.route('/health')
    def health():
        return jsonify({{'status': 'healthy'}})

    return app
";
    }

    private static string GenerateRoutesInit()
    {
        return @"\"\"\"Routes package.\"\"\"
from .items import bp as items_bp

__all__ = ['items_bp']
";
    }

    private static string GenerateItemsRoute()
    {
        return @"\"\"\"Items routes.\"\"\"
from flask import Blueprint, jsonify, request, abort

from app.models.item import Item, items_db, get_next_id

bp = Blueprint('items', __name__)


@bp.route('', methods=['GET'])
def get_items():
    \"\"\"Get all items.\"\"\"
    return jsonify(list(items_db.values()))


@bp.route('/<int:item_id>', methods=['GET'])
def get_item(item_id: int):
    \"\"\"Get a specific item.\"\"\"
    item = items_db.get(item_id)
    if item is None:
        abort(404, description='Item not found')
    return jsonify(item)


@bp.route('', methods=['POST'])
def create_item():
    \"\"\"Create a new item.\"\"\"
    data = request.get_json()

    if not data or 'name' not in data or 'price' not in data:
        abort(400, description='Name and price are required')

    item_id = get_next_id()
    item: Item = {
        'id': item_id,
        'name': data['name'],
        'description': data.get('description', ''),
        'price': float(data['price'])
    }

    items_db[item_id] = item
    return jsonify(item), 201


@bp.route('/<int:item_id>', methods=['DELETE'])
def delete_item(item_id: int):
    \"\"\"Delete an item.\"\"\"
    if item_id not in items_db:
        abort(404, description='Item not found')

    del items_db[item_id]
    return '', 204


@bp.errorhandler(400)
def bad_request(error):
    return jsonify({'error': str(error.description)}), 400


@bp.errorhandler(404)
def not_found(error):
    return jsonify({'error': str(error.description)}), 404
";
    }

    private static string GenerateItemModel()
    {
        return @"\"\"\"Item model.\"\"\"
from typing import TypedDict

# In-memory storage
items_db: dict[int, 'Item'] = {}
_current_id = 0


class Item(TypedDict):
    \"\"\"Item type definition.\"\"\"
    id: int
    name: str
    description: str
    price: float


def get_next_id() -> int:
    \"\"\"Get the next available ID.\"\"\"
    global _current_id
    _current_id += 1
    return _current_id
";
    }

    private static string GenerateConfig()
    {
        return @"\"\"\"Application configuration.\"\"\"
import os


class Config:
    \"\"\"Base configuration.\"\"\"
    SECRET_KEY = os.environ.get('SECRET_KEY', 'dev-secret-key')
    DEBUG = False
    TESTING = False


class DevelopmentConfig(Config):
    \"\"\"Development configuration.\"\"\"
    DEBUG = True


class ProductionConfig(Config):
    \"\"\"Production configuration.\"\"\"
    pass


class TestingConfig(Config):
    \"\"\"Testing configuration.\"\"\"
    TESTING = True
";
    }

    private static string GenerateRun()
    {
        return @"\"\"\"Application entry point.\"\"\"
import os

from app import create_app

app = create_app()

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    debug = os.environ.get('FLASK_DEBUG', 'true').lower() == 'true'
    app.run(host='0.0.0.0', port=port, debug=debug)
";
    }

    private static string GenerateRequirements(ProjectRequirements requirements)
    {
        var packages = new List<string>
        {
            "flask>=3.0.0",
            "python-dotenv>=1.0.0"
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            packages.Add("pytest>=8.0.0");
            packages.Add("pytest-cov>=4.1.0");
        }

        if (requirements.IncludeContainerSupport)
        {
            packages.Add("gunicorn>=21.0.0");
        }

        if (requirements.LintingTools.Contains("Ruff"))
        {
            packages.Add("ruff>=0.2.0");
        }

        return string.Join("\n", packages) + "\n";
    }

    private static string GenerateGitignore()
    {
        return @"# Python
__pycache__/
*.py[cod]
*$py.class
*.so
.Python
venv/
.venv/
env/

# Flask
instance/
.webassets-cache

# IDE
.idea/
.vscode/
*.swp

# Environment
.env
.env.local

# Testing
.coverage
htmlcov/
.pytest_cache/

# Distribution
dist/
build/
*.egg-info/
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Flask REST API application."}

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

3. Copy environment file:
   ```bash
   cp .env.example .env
   ```

### Running the Application

```bash
python run.py
```

Or with Flask CLI:
```bash
flask run
```

The API will be available at `http://localhost:5000`.

### Testing

```bash
pytest
```

With coverage:
```bash
pytest --cov=app
```

## API Endpoints

- `GET /` - Welcome message
- `GET /health` - Health check
- `GET /items` - List all items
- `GET /items/<id>` - Get item by ID
- `POST /items` - Create item
- `DELETE /items/<id>` - Delete item

## Project Structure

```
{projectName}/
├── app/
│   ├── __init__.py
│   ├── config.py
│   ├── models/
│   │   ├── __init__.py
│   │   └── item.py
│   └── routes/
│       ├── __init__.py
│       └── items.py
├── tests/
│   ├── __init__.py
│   ├── conftest.py
│   └── test_app.py
├── run.py
├── requirements.txt
├── .env.example
└── README.md
```

## License

MIT
";
    }

    private static string GenerateEnvExample()
    {
        return @"# Flask
FLASK_APP=run.py
FLASK_DEBUG=true
SECRET_KEY=your-secret-key-here

# Server
PORT=5000
";
    }

    private static string GenerateConftest()
    {
        return @"\"\"\"Pytest fixtures.\"\"\"
import pytest

from app import create_app
from app.config import TestingConfig


@pytest.fixture
def app():
    \"\"\"Create application for testing.\"\"\"
    app = create_app(TestingConfig)
    yield app


@pytest.fixture
def client(app):
    \"\"\"Create test client.\"\"\"
    return app.test_client()
";
    }

    private static string GenerateTestApp()
    {
        return @"\"\"\"Application tests.\"\"\"


def test_index(client):
    \"\"\"Test index endpoint.\"\"\"
    response = client.get('/')
    assert response.status_code == 200
    assert 'message' in response.json


def test_health(client):
    \"\"\"Test health endpoint.\"\"\"
    response = client.get('/health')
    assert response.status_code == 200
    assert response.json == {'status': 'healthy'}


def test_create_and_get_item(client):
    \"\"\"Test creating and retrieving an item.\"\"\"
    # Create item
    item_data = {'name': 'Test Item', 'price': 9.99}
    response = client.post('/items', json=item_data)
    assert response.status_code == 201
    created_item = response.json
    assert created_item['name'] == item_data['name']

    # Get item
    item_id = created_item['id']
    response = client.get(f'/items/{item_id}')
    assert response.status_code == 200
    assert response.json['id'] == item_id


def test_get_nonexistent_item(client):
    \"\"\"Test getting a nonexistent item.\"\"\"
    response = client.get('/items/99999')
    assert response.status_code == 404


def test_delete_item(client):
    \"\"\"Test deleting an item.\"\"\"
    # Create item first
    response = client.post('/items', json={'name': 'To Delete', 'price': 5.00})
    item_id = response.json['id']

    # Delete it
    response = client.delete(f'/items/{item_id}')
    assert response.status_code == 204

    # Verify it's gone
    response = client.get(f'/items/{item_id}')
    assert response.status_code == 404
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 5000
CMD [""gunicorn"", ""--bind"", ""0.0.0.0:5000"", ""run:app""]
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
      - ""5000:5000""
    environment:
      - FLASK_DEBUG=false
      - SECRET_KEY=change-me-in-production
    volumes:
      - .:/app
    command: gunicorn --bind 0.0.0.0:5000 run:app --reload
";
    }
}
