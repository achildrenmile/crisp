using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Python Django projects.
/// </summary>
public sealed class DjangoGenerator : IProjectGenerator
{
    private readonly ILogger<DjangoGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public DjangoGenerator(
        ILogger<DjangoGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "python-django";
    public string TemplateName => "Python Django";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Python &&
               requirements.Framework == ProjectFramework.Django;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Django project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName.Replace("-", "_");

        // Create project structure
        var configPath = Path.Combine(outputPath, "config");
        var appPath = Path.Combine(outputPath, "api");

        await _filesystem.CreateDirectoryAsync(configPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(appPath, cancellationToken);

        // manage.py
        var manageContent = GenerateManage(projectName);
        var managePath = Path.Combine(outputPath, "manage.py");
        await _filesystem.WriteFileAsync(managePath, manageContent, cancellationToken);
        createdFiles.Add(managePath);

        // config/__init__.py
        var configInitPath = Path.Combine(configPath, "__init__.py");
        await _filesystem.WriteFileAsync(configInitPath, "", cancellationToken);
        createdFiles.Add(configInitPath);

        // config/settings.py
        var settingsContent = GenerateSettings(projectName);
        var settingsPath = Path.Combine(configPath, "settings.py");
        await _filesystem.WriteFileAsync(settingsPath, settingsContent, cancellationToken);
        createdFiles.Add(settingsPath);

        // config/urls.py
        var urlsContent = GenerateUrls();
        var urlsPath = Path.Combine(configPath, "urls.py");
        await _filesystem.WriteFileAsync(urlsPath, urlsContent, cancellationToken);
        createdFiles.Add(urlsPath);

        // config/wsgi.py
        var wsgiContent = GenerateWsgi();
        var wsgiPath = Path.Combine(configPath, "wsgi.py");
        await _filesystem.WriteFileAsync(wsgiPath, wsgiContent, cancellationToken);
        createdFiles.Add(wsgiPath);

        // config/asgi.py
        var asgiContent = GenerateAsgi();
        var asgiPath = Path.Combine(configPath, "asgi.py");
        await _filesystem.WriteFileAsync(asgiPath, asgiContent, cancellationToken);
        createdFiles.Add(asgiPath);

        // api/__init__.py
        var apiInitPath = Path.Combine(appPath, "__init__.py");
        await _filesystem.WriteFileAsync(apiInitPath, "", cancellationToken);
        createdFiles.Add(apiInitPath);

        // api/models.py
        var modelsContent = GenerateModels();
        var modelsPath = Path.Combine(appPath, "models.py");
        await _filesystem.WriteFileAsync(modelsPath, modelsContent, cancellationToken);
        createdFiles.Add(modelsPath);

        // api/views.py
        var viewsContent = GenerateViews();
        var viewsPath = Path.Combine(appPath, "views.py");
        await _filesystem.WriteFileAsync(viewsPath, viewsContent, cancellationToken);
        createdFiles.Add(viewsPath);

        // api/serializers.py
        var serializersContent = GenerateSerializers();
        var serializersPath = Path.Combine(appPath, "serializers.py");
        await _filesystem.WriteFileAsync(serializersPath, serializersContent, cancellationToken);
        createdFiles.Add(serializersPath);

        // api/urls.py
        var apiUrlsContent = GenerateApiUrls();
        var apiUrlsPath = Path.Combine(appPath, "urls.py");
        await _filesystem.WriteFileAsync(apiUrlsPath, apiUrlsContent, cancellationToken);
        createdFiles.Add(apiUrlsPath);

        // api/apps.py
        var appsContent = GenerateApps();
        var appsPath = Path.Combine(appPath, "apps.py");
        await _filesystem.WriteFileAsync(appsPath, appsContent, cancellationToken);
        createdFiles.Add(appsPath);

        // api/admin.py
        var adminContent = GenerateAdmin();
        var adminPath = Path.Combine(appPath, "admin.py");
        await _filesystem.WriteFileAsync(adminPath, adminContent, cancellationToken);
        createdFiles.Add(adminPath);

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

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testsContent = GenerateTests();
            var testsPath = Path.Combine(appPath, "tests.py");
            await _filesystem.WriteFileAsync(testsPath, testsContent, cancellationToken);
            createdFiles.Add(testsPath);
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
            new() { RelativePath = "manage.py", Description = "Django management script" },
            new() { RelativePath = "config", IsDirectory = true, Description = "Project configuration" },
            new() { RelativePath = "config/__init__.py", Description = "Package init" },
            new() { RelativePath = "config/settings.py", Description = "Django settings" },
            new() { RelativePath = "config/urls.py", Description = "URL configuration" },
            new() { RelativePath = "config/wsgi.py", Description = "WSGI configuration" },
            new() { RelativePath = "config/asgi.py", Description = "ASGI configuration" },
            new() { RelativePath = "api", IsDirectory = true, Description = "API application" },
            new() { RelativePath = "api/__init__.py", Description = "Package init" },
            new() { RelativePath = "api/models.py", Description = "Database models" },
            new() { RelativePath = "api/views.py", Description = "API views" },
            new() { RelativePath = "api/serializers.py", Description = "DRF serializers" },
            new() { RelativePath = "api/urls.py", Description = "API URLs" },
            new() { RelativePath = "api/apps.py", Description = "App configuration" },
            new() { RelativePath = "api/admin.py", Description = "Admin configuration" },
            new() { RelativePath = "requirements.txt", Description = "Python dependencies" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "api/tests.py", Description = "API tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateManage(string projectName)
    {
        return @"#!/usr/bin/env python
""""""Django's command-line utility for administrative tasks.""""""
import os
import sys


def main():
    """"""Run administrative tasks.""""""
    os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'config.settings')
    try:
        from django.core.management import execute_from_command_line
    except ImportError as exc:
        raise ImportError(
            ""Couldn't import Django. Are you sure it's installed and ""
            ""available on your PYTHONPATH environment variable? Did you ""
            ""forget to activate a virtual environment?""
        ) from exc
    execute_from_command_line(sys.argv)


if __name__ == '__main__':
    main()
";
    }

    private static string GenerateSettings(string projectName)
    {
        return $@"""""""Django settings for {projectName} project.""""""
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

SECRET_KEY = os.environ.get('SECRET_KEY', 'django-insecure-change-me-in-production')

DEBUG = os.environ.get('DEBUG', 'True').lower() == 'true'

ALLOWED_HOSTS = os.environ.get('ALLOWED_HOSTS', 'localhost,127.0.0.1').split(',')

INSTALLED_APPS = [
    'django.contrib.admin',
    'django.contrib.auth',
    'django.contrib.contenttypes',
    'django.contrib.sessions',
    'django.contrib.messages',
    'django.contrib.staticfiles',
    'rest_framework',
    'api',
]

MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'django.contrib.sessions.middleware.SessionMiddleware',
    'django.middleware.common.CommonMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
    'django.contrib.auth.middleware.AuthenticationMiddleware',
    'django.contrib.messages.middleware.MessageMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
]

ROOT_URLCONF = 'config.urls'

TEMPLATES = [
    {{
        'BACKEND': 'django.template.backends.django.DjangoTemplates',
        'DIRS': [],
        'APP_DIRS': True,
        'OPTIONS': {{
            'context_processors': [
                'django.template.context_processors.debug',
                'django.template.context_processors.request',
                'django.contrib.auth.context_processors.auth',
                'django.contrib.messages.context_processors.messages',
            ],
        }},
    }},
]

WSGI_APPLICATION = 'config.wsgi.application'

DATABASES = {{
    'default': {{
        'ENGINE': 'django.db.backends.sqlite3',
        'NAME': BASE_DIR / 'db.sqlite3',
    }}
}}

AUTH_PASSWORD_VALIDATORS = [
    {{'NAME': 'django.contrib.auth.password_validation.UserAttributeSimilarityValidator'}},
    {{'NAME': 'django.contrib.auth.password_validation.MinimumLengthValidator'}},
    {{'NAME': 'django.contrib.auth.password_validation.CommonPasswordValidator'}},
    {{'NAME': 'django.contrib.auth.password_validation.NumericPasswordValidator'}},
]

LANGUAGE_CODE = 'en-us'
TIME_ZONE = 'UTC'
USE_I18N = True
USE_TZ = True

STATIC_URL = 'static/'

DEFAULT_AUTO_FIELD = 'django.db.models.BigAutoField'

REST_FRAMEWORK = {{
    'DEFAULT_PERMISSION_CLASSES': [
        'rest_framework.permissions.AllowAny',
    ],
}}
";
    }

    private static string GenerateUrls()
    {
        return @"""""""URL configuration for the project.""""""
from django.contrib import admin
from django.urls import path, include

urlpatterns = [
    path('admin/', admin.site.urls),
    path('api/', include('api.urls')),
]
";
    }

    private static string GenerateWsgi()
    {
        return @"""""""WSGI config for the project.""""""
import os

from django.core.wsgi import get_wsgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'config.settings')

application = get_wsgi_application()
";
    }

    private static string GenerateAsgi()
    {
        return @"""""""ASGI config for the project.""""""
import os

from django.core.asgi import get_asgi_application

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'config.settings')

application = get_asgi_application()
";
    }

    private static string GenerateModels()
    {
        return @"""""""API models.""""""
from django.db import models


class Item(models.Model):
    """"""Item model.""""""

    name = models.CharField(max_length=200)
    description = models.TextField(blank=True)
    price = models.DecimalField(max_digits=10, decimal_places=2)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        ordering = ['-created_at']

    def __str__(self):
        return self.name
";
    }

    private static string GenerateViews()
    {
        return @"""""""API views.""""""
from rest_framework import viewsets, status
from rest_framework.decorators import api_view
from rest_framework.response import Response

from .models import Item
from .serializers import ItemSerializer


class ItemViewSet(viewsets.ModelViewSet):
    """"""ViewSet for Item model.""""""

    queryset = Item.objects.all()
    serializer_class = ItemSerializer


@api_view(['GET'])
def health_check(request):
    """"""Health check endpoint.""""""
    return Response({{'status': 'healthy'}})


@api_view(['GET'])
def root(request):
    """"""Root endpoint.""""""
    return Response({{'message': 'Welcome to the API'}})
";
    }

    private static string GenerateSerializers()
    {
        return @"""""""API serializers.""""""
from rest_framework import serializers

from .models import Item


class ItemSerializer(serializers.ModelSerializer):
    """"""Serializer for Item model.""""""

    class Meta:
        model = Item
        fields = ['id', 'name', 'description', 'price', 'created_at', 'updated_at']
        read_only_fields = ['created_at', 'updated_at']
";
    }

    private static string GenerateApiUrls()
    {
        return @"""""""API URL configuration.""""""
from django.urls import path, include
from rest_framework.routers import DefaultRouter

from . import views

router = DefaultRouter()
router.register(r'items', views.ItemViewSet)

urlpatterns = [
    path('', views.root),
    path('health/', views.health_check),
    path('', include(router.urls)),
]
";
    }

    private static string GenerateApps()
    {
        return @"""""""API app configuration.""""""
from django.apps import AppConfig


class ApiConfig(AppConfig):
    default_auto_field = 'django.db.models.BigAutoField'
    name = 'api'
";
    }

    private static string GenerateAdmin()
    {
        return @"""""""Admin configuration.""""""
from django.contrib import admin

from .models import Item


@admin.register(Item)
class ItemAdmin(admin.ModelAdmin):
    list_display = ['name', 'price', 'created_at']
    search_fields = ['name', 'description']
    list_filter = ['created_at']
";
    }

    private static string GenerateRequirements(ProjectRequirements requirements)
    {
        var packages = new List<string>
        {
            "Django>=5.0",
            "djangorestframework>=3.14.0",
            "python-dotenv>=1.0.0"
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            packages.Add("pytest>=8.0.0");
            packages.Add("pytest-django>=4.7.0");
        }

        if (requirements.IncludeContainerSupport)
        {
            packages.Add("gunicorn>=21.0.0");
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
env/
venv/
.venv/

# Django
*.log
local_settings.py
db.sqlite3
media/

# IDE
.idea/
.vscode/
*.swp

# Environment
.env
.env.local

# Static files
staticfiles/

# Coverage
htmlcov/
.coverage
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Django REST API application."}

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

3. Run migrations:
   ```bash
   python manage.py migrate
   ```

4. Create a superuser (optional):
   ```bash
   python manage.py createsuperuser
   ```

### Running the Application

```bash
python manage.py runserver
```

The API will be available at `http://localhost:8000`.

### API Endpoints

- `GET /api/` - API root
- `GET /api/health/` - Health check
- `GET /api/items/` - List items
- `POST /api/items/` - Create item
- `GET /api/items/{{id}}/` - Get item
- `PUT /api/items/{{id}}/` - Update item
- `DELETE /api/items/{{id}}/` - Delete item

### Admin Interface

Access the admin interface at `http://localhost:8000/admin/`

### Testing

```bash
pytest
```

## Project Structure

```
{projectName}/
├── config/
│   ├── __init__.py
│   ├── settings.py
│   ├── urls.py
│   ├── wsgi.py
│   └── asgi.py
├── api/
│   ├── __init__.py
│   ├── admin.py
│   ├── apps.py
│   ├── models.py
│   ├── serializers.py
│   ├── urls.py
│   ├── views.py
│   └── tests.py
├── manage.py
├── requirements.txt
└── README.md
```

## License

MIT
";
    }

    private static string GenerateTests()
    {
        return @"""""""API tests.""""""
import pytest
from django.urls import reverse
from rest_framework import status
from rest_framework.test import APIClient

from .models import Item


@pytest.fixture
def api_client():
    return APIClient()


@pytest.fixture
def sample_item(db):
    return Item.objects.create(
        name='Test Item',
        description='Test Description',
        price=9.99
    )


@pytest.mark.django_db
class TestHealthCheck:
    def test_health_check(self, api_client):
        response = api_client.get('/api/health/')
        assert response.status_code == status.HTTP_200_OK
        assert response.data == {{'status': 'healthy'}}


@pytest.mark.django_db
class TestItemAPI:
    def test_list_items(self, api_client, sample_item):
        response = api_client.get('/api/items/')
        assert response.status_code == status.HTTP_200_OK
        assert len(response.data) == 1

    def test_create_item(self, api_client):
        data = {{'name': 'New Item', 'price': '19.99'}}
        response = api_client.post('/api/items/', data)
        assert response.status_code == status.HTTP_201_CREATED
        assert response.data['name'] == 'New Item'

    def test_get_item(self, api_client, sample_item):
        response = api_client.get(f'/api/items/{{sample_item.id}}/')
        assert response.status_code == status.HTTP_200_OK
        assert response.data['name'] == sample_item.name

    def test_delete_item(self, api_client, sample_item):
        response = api_client.delete(f'/api/items/{{sample_item.id}}/')
        assert response.status_code == status.HTTP_204_NO_CONTENT
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

RUN python manage.py collectstatic --noinput

EXPOSE 8000
CMD [""gunicorn"", ""--bind"", ""0.0.0.0:8000"", ""config.wsgi:application""]
";
    }

    private static string GenerateDockerCompose(string projectName)
    {
        return $@"version: '3.8'

services:
  web:
    build: .
    container_name: {projectName}
    ports:
      - ""8000:8000""
    environment:
      - DEBUG=False
      - SECRET_KEY=change-me-in-production
      - ALLOWED_HOSTS=localhost,127.0.0.1
    volumes:
      - .:/app
    command: gunicorn --bind 0.0.0.0:8000 config.wsgi:application --reload
";
    }
}
