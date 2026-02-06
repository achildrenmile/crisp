using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Dart Shelf API projects.
/// </summary>
public sealed class DartShelfGenerator : IProjectGenerator
{
    private readonly ILogger<DartShelfGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public DartShelfGenerator(
        ILogger<DartShelfGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "dart-shelf";
    public string TemplateName => "Dart Shelf";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Dart &&
               requirements.Framework == ProjectFramework.DartShelf;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Dart Shelf project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();

        // Create directories
        var binPath = Path.Combine(outputPath, "bin");
        var libPath = Path.Combine(outputPath, "lib");
        var srcPath = Path.Combine(libPath, "src");
        var routesPath = Path.Combine(srcPath, "routes");
        var modelsPath = Path.Combine(srcPath, "models");
        var testPath = Path.Combine(outputPath, "test");

        await _filesystem.CreateDirectoryAsync(binPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(libPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(routesPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(modelsPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(testPath, cancellationToken);

        // pubspec.yaml
        var pubspecContent = GeneratePubspec(requirements.ProjectName, requirements);
        var pubspecPath = Path.Combine(outputPath, "pubspec.yaml");
        await _filesystem.WriteFileAsync(pubspecPath, pubspecContent, cancellationToken);
        createdFiles.Add(pubspecPath);

        // analysis_options.yaml
        var analysisContent = GenerateAnalysisOptions();
        var analysisPath = Path.Combine(outputPath, "analysis_options.yaml");
        await _filesystem.WriteFileAsync(analysisPath, analysisContent, cancellationToken);
        createdFiles.Add(analysisPath);

        // bin/server.dart (entry point)
        var serverContent = GenerateServer(requirements.ProjectName);
        var serverPath = Path.Combine(binPath, "server.dart");
        await _filesystem.WriteFileAsync(serverPath, serverContent, cancellationToken);
        createdFiles.Add(serverPath);

        // lib/<project_name>.dart (library entry)
        var libEntryContent = GenerateLibEntry();
        var libEntryPath = Path.Combine(libPath, $"{ToSnakeCase(requirements.ProjectName)}.dart");
        await _filesystem.WriteFileAsync(libEntryPath, libEntryContent, cancellationToken);
        createdFiles.Add(libEntryPath);

        // lib/src/app.dart
        var appContent = GenerateApp(requirements.ProjectName);
        var appPath = Path.Combine(srcPath, "app.dart");
        await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
        createdFiles.Add(appPath);

        // lib/src/routes/items_route.dart
        var itemsRouteContent = GenerateItemsRoute();
        var itemsRoutePath = Path.Combine(routesPath, "items_route.dart");
        await _filesystem.WriteFileAsync(itemsRoutePath, itemsRouteContent, cancellationToken);
        createdFiles.Add(itemsRoutePath);

        // lib/src/routes/health_route.dart
        var healthRouteContent = GenerateHealthRoute();
        var healthRoutePath = Path.Combine(routesPath, "health_route.dart");
        await _filesystem.WriteFileAsync(healthRoutePath, healthRouteContent, cancellationToken);
        createdFiles.Add(healthRoutePath);

        // lib/src/models/item.dart
        var itemModelContent = GenerateItemModel();
        var itemModelPath = Path.Combine(modelsPath, "item.dart");
        await _filesystem.WriteFileAsync(itemModelPath, itemModelContent, cancellationToken);
        createdFiles.Add(itemModelPath);

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

        // test/server_test.dart
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testContent = GenerateTest(requirements.ProjectName);
            var testFilePath = Path.Combine(testPath, "server_test.dart");
            await _filesystem.WriteFileAsync(testFilePath, testContent, cancellationToken);
            createdFiles.Add(testFilePath);
        }

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile();
            var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
            await _filesystem.WriteFileAsync(dockerfilePath, dockerfileContent, cancellationToken);
            createdFiles.Add(dockerfilePath);

            var dockerignoreContent = GenerateDockerignore();
            var dockerignorePath = Path.Combine(outputPath, ".dockerignore");
            await _filesystem.WriteFileAsync(dockerignorePath, dockerignoreContent, cancellationToken);
            createdFiles.Add(dockerignorePath);
        }

        _logger.LogInformation("Generated {Count} files", createdFiles.Count);
        return createdFiles;
    }

    public Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var projectSnake = ToSnakeCase(requirements.ProjectName);

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "pubspec.yaml", Description = "Dart package manifest" },
            new() { RelativePath = "analysis_options.yaml", Description = "Dart analyzer configuration" },
            new() { RelativePath = "bin", IsDirectory = true, Description = "Executable entry points" },
            new() { RelativePath = "bin/server.dart", Description = "Server entry point" },
            new() { RelativePath = "lib", IsDirectory = true, Description = "Library source code" },
            new() { RelativePath = $"lib/{projectSnake}.dart", Description = "Library entry" },
            new() { RelativePath = "lib/src", IsDirectory = true, Description = "Source files" },
            new() { RelativePath = "lib/src/app.dart", Description = "Application setup" },
            new() { RelativePath = "lib/src/routes", IsDirectory = true, Description = "Route handlers" },
            new() { RelativePath = "lib/src/routes/items_route.dart", Description = "Items API routes" },
            new() { RelativePath = "lib/src/routes/health_route.dart", Description = "Health check route" },
            new() { RelativePath = "lib/src/models", IsDirectory = true, Description = "Data models" },
            new() { RelativePath = "lib/src/models/item.dart", Description = "Item model" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "test", IsDirectory = true, Description = "Test files" });
            files.Add(new PlannedFile { RelativePath = "test/server_test.dart", Description = "Server tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = ".dockerignore", Description = "Docker ignore file" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string ToSnakeCase(string name)
    {
        return name.Replace("-", "_").ToLowerInvariant();
    }

    private static string GeneratePubspec(string projectName, ProjectRequirements requirements)
    {
        var snakeName = projectName.Replace("-", "_").ToLowerInvariant();
        var description = requirements.Description ?? "A Dart Shelf REST API server.";
        return $"""
name: {snakeName}
description: {description}
version: 1.0.0
publish_to: none

environment:
  sdk: '>=3.0.0 <4.0.0'

dependencies:
  shelf: ^1.4.1
  shelf_router: ^1.1.4
  args: ^2.4.2

dev_dependencies:
  lints: ^3.0.0
  test: ^1.24.0
  http: ^1.2.0
""";
    }

    private static string GenerateAnalysisOptions()
    {
        return """
include: package:lints/recommended.yaml

linter:
  rules:
    prefer_single_quotes: true
    avoid_print: false
    prefer_const_constructors: true
    prefer_final_locals: true

analyzer:
  errors:
    unused_import: warning
    unused_local_variable: warning
""";
    }

    private static string GenerateServer(string projectName)
    {
        var snakeName = projectName.Replace("-", "_").ToLowerInvariant();
        return $$"""
import 'dart:io';

import 'package:args/args.dart';
import 'package:shelf/shelf_io.dart' as shelf_io;
import 'package:{{snakeName}}/{{snakeName}}.dart';

void main(List<String> args) async {
  final parser = ArgParser()..addOption('port', abbr: 'p', defaultsTo: '8080');
  final result = parser.parse(args);

  final port = int.parse(result['port'] as String);
  final app = createApp();

  final server = await shelf_io.serve(app, InternetAddress.anyIPv4, port);

  print('Server running on http://${server.address.host}:${server.port}');
}
""";
    }

    private static string GenerateLibEntry()
    {
        return """
/// Server library
library;

export 'src/app.dart';
""";
    }

    private static string GenerateApp(string projectName)
    {
        return $$"""
import 'package:shelf/shelf.dart';
import 'package:shelf_router/shelf_router.dart';

import 'routes/health_route.dart';
import 'routes/items_route.dart';

/// Creates the main application handler
Handler createApp() {
  final router = Router();

  // Root endpoint
  router.get('/', (Request request) {
    return Response.ok(
      '{"message": "Welcome to {{projectName}}"}',
      headers: {'content-type': 'application/json'},
    );
  });

  // Mount routes
  router.mount('/health', HealthRoute().router.call);
  router.mount('/items', ItemsRoute().router.call);

  // Middleware pipeline
  final handler = const Pipeline()
      .addMiddleware(logRequests())
      .addMiddleware(_corsMiddleware())
      .addHandler(router.call);

  return handler;
}

/// CORS middleware
Middleware _corsMiddleware() {
  return (Handler innerHandler) {
    return (Request request) async {
      if (request.method == 'OPTIONS') {
        return Response.ok('', headers: _corsHeaders);
      }

      final response = await innerHandler(request);
      return response.change(headers: _corsHeaders);
    };
  };
}

const _corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
  'Access-Control-Allow-Headers': 'Origin, Content-Type, Accept, Authorization',
};
""";
    }

    private static string GenerateHealthRoute()
    {
        return """
import 'package:shelf/shelf.dart';
import 'package:shelf_router/shelf_router.dart';

/// Health check route handler
class HealthRoute {
  Router get router {
    final router = Router();

    router.get('/', (Request request) {
      return Response.ok(
        '{"status": "healthy"}',
        headers: {'content-type': 'application/json'},
      );
    });

    return router;
  }
}
""";
    }

    private static string GenerateItemsRoute()
    {
        return """
import 'dart:convert';

import 'package:shelf/shelf.dart';
import 'package:shelf_router/shelf_router.dart';

import '../models/item.dart';

/// Items CRUD route handler
class ItemsRoute {
  final Map<int, Item> _items = {};
  int _currentId = 0;

  Router get router {
    final router = Router();

    // GET all items
    router.get('/', (Request request) {
      final itemsList = _items.values.map((item) => item.toJson()).toList();
      return Response.ok(
        jsonEncode(itemsList),
        headers: {'content-type': 'application/json'},
      );
    });

    // GET item by ID
    router.get('/<id|[0-9]+>', (Request request, String id) {
      final itemId = int.parse(id);
      final item = _items[itemId];

      if (item == null) {
        return Response.notFound(
          jsonEncode({'error': 'Item not found'}),
          headers: {'content-type': 'application/json'},
        );
      }

      return Response.ok(
        jsonEncode(item.toJson()),
        headers: {'content-type': 'application/json'},
      );
    });

    // POST create item
    router.post('/', (Request request) async {
      final body = await request.readAsString();
      final data = jsonDecode(body) as Map<String, dynamic>;

      if (!data.containsKey('name') || !data.containsKey('price')) {
        return Response(
          400,
          body: jsonEncode({'error': 'Name and price are required'}),
          headers: {'content-type': 'application/json'},
        );
      }

      _currentId++;
      final item = Item(
        id: _currentId,
        name: data['name'] as String,
        description: data['description'] as String?,
        price: (data['price'] as num).toDouble(),
      );
      _items[_currentId] = item;

      return Response(
        201,
        body: jsonEncode(item.toJson()),
        headers: {'content-type': 'application/json'},
      );
    });

    // PUT update item
    router.put('/<id|[0-9]+>', (Request request, String id) async {
      final itemId = int.parse(id);
      final existing = _items[itemId];

      if (existing == null) {
        return Response.notFound(
          jsonEncode({'error': 'Item not found'}),
          headers: {'content-type': 'application/json'},
        );
      }

      final body = await request.readAsString();
      final data = jsonDecode(body) as Map<String, dynamic>;

      final updated = Item(
        id: itemId,
        name: data['name'] as String? ?? existing.name,
        description: data['description'] as String? ?? existing.description,
        price: (data['price'] as num?)?.toDouble() ?? existing.price,
      );
      _items[itemId] = updated;

      return Response.ok(
        jsonEncode(updated.toJson()),
        headers: {'content-type': 'application/json'},
      );
    });

    // DELETE item
    router.delete('/<id|[0-9]+>', (Request request, String id) {
      final itemId = int.parse(id);

      if (!_items.containsKey(itemId)) {
        return Response.notFound(
          jsonEncode({'error': 'Item not found'}),
          headers: {'content-type': 'application/json'},
        );
      }

      _items.remove(itemId);
      return Response(204);
    });

    return router;
  }
}
""";
    }

    private static string GenerateItemModel()
    {
        return """
/// Item data model
class Item {
  final int id;
  final String name;
  final String? description;
  final double price;

  const Item({
    required this.id,
    required this.name,
    this.description,
    required this.price,
  });

  factory Item.fromJson(Map<String, dynamic> json) {
    return Item(
      id: json['id'] as int,
      name: json['name'] as String,
      description: json['description'] as String?,
      price: (json['price'] as num).toDouble(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      if (description != null) 'description': description,
      'price': price,
    };
  }
}
""";
    }

    private static string GenerateGitignore()
    {
        return """
# Dart/Flutter
.dart_tool/
.packages
build/
pubspec.lock

# IDE
.idea/
*.iml
.vscode/

# OS
.DS_Store
Thumbs.db

# Coverage
coverage/

# Generated files
*.g.dart
*.freezed.dart
""";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        var projectSnake = ToSnakeCase(projectName);
        var description = requirements.Description ?? "A Dart Shelf REST API server.";
        return $"""
# {projectName}

{description}

## Getting Started

### Prerequisites

- Dart SDK 3.0+

### Installation

```bash
dart pub get
```

### Development

```bash
dart run bin/server.dart
```

The server will start on `http://localhost:8080`.

You can specify a different port:

```bash
dart run bin/server.dart --port 3000
```

### Production Build

```bash
dart compile exe bin/server.dart -o bin/server
./bin/server
```

### Testing

```bash
dart test
```

## API Endpoints

- `GET /` - Welcome message
- `GET /health` - Health check
- `GET /items` - List all items
- `GET /items/:id` - Get item by ID
- `POST /items` - Create item
- `PUT /items/:id` - Update item
- `DELETE /items/:id` - Delete item

## Project Structure

```
{projectName}/
├── bin/
│   └── server.dart       # Entry point
├── lib/
│   ├── {projectSnake}.dart    # Library entry
│   └── src/
│       ├── app.dart      # Application setup
│       ├── routes/
│       │   ├── health_route.dart
│       │   └── items_route.dart
│       └── models/
│           └── item.dart
├── test/
├── pubspec.yaml
└── README.md
```

## License

MIT
""";
    }

    private static string GenerateTest(string projectName)
    {
        var snakeName = projectName.Replace("-", "_").ToLowerInvariant();
        return $$"""
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:shelf/shelf_io.dart' as shelf_io;
import 'package:{{snakeName}}/{{snakeName}}.dart';
import 'package:test/test.dart';

void main() {
  late HttpServer server;
  late Uri baseUrl;

  setUp(() async {
    final app = createApp();
    server = await shelf_io.serve(app, 'localhost', 0);
    baseUrl = Uri.parse('http://${server.address.host}:${server.port}');
  });

  tearDown(() async {
    await server.close();
  });

  test('GET / returns welcome message', () async {
    final response = await http.get(baseUrl);

    expect(response.statusCode, equals(200));
    expect(jsonDecode(response.body), containsPair('message', isNotEmpty));
  });

  test('GET /health returns healthy status', () async {
    final response = await http.get(baseUrl.resolve('/health'));

    expect(response.statusCode, equals(200));
    expect(jsonDecode(response.body), equals({'status': 'healthy'}));
  });

  group('Items API', () {
    test('POST /items creates an item', () async {
      final response = await http.post(
        baseUrl.resolve('/items'),
        headers: {'content-type': 'application/json'},
        body: jsonEncode({'name': 'Test Item', 'price': 9.99}),
      );

      expect(response.statusCode, equals(201));
      final body = jsonDecode(response.body);
      expect(body['name'], equals('Test Item'));
      expect(body['price'], equals(9.99));
      expect(body['id'], isNotNull);
    });

    test('GET /items returns empty list initially', () async {
      final response = await http.get(baseUrl.resolve('/items'));

      expect(response.statusCode, equals(200));
      expect(jsonDecode(response.body), isList);
    });

    test('GET /items/:id returns 404 for nonexistent item', () async {
      final response = await http.get(baseUrl.resolve('/items/99999'));

      expect(response.statusCode, equals(404));
    });
  });
}
""";
    }

    private static string GenerateDockerfile()
    {
        return """
# Build stage
FROM dart:stable AS build

WORKDIR /app
COPY pubspec.* ./
RUN dart pub get

COPY . .
RUN dart compile exe bin/server.dart -o bin/server

# Runtime stage
FROM debian:bookworm-slim

RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/bin/server /app/bin/server

EXPOSE 8080
CMD ["/app/bin/server", "--port", "8080"]
""";
    }

    private static string GenerateDockerignore()
    {
        return """
.dart_tool/
.packages
build/
.git
.gitignore
*.md
test/
coverage/
.idea/
.vscode/
""";
    }
}
