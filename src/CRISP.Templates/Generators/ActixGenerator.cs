using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Rust Actix Web projects.
/// </summary>
public sealed class ActixGenerator : IProjectGenerator
{
    private readonly ILogger<ActixGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public ActixGenerator(
        ILogger<ActixGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "rust-actix";
    public string TemplateName => "Rust Actix Web";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Rust &&
               requirements.Framework == ProjectFramework.Actix;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Rust Actix Web project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var crateName = requirements.ProjectName.Replace("-", "_");

        // Create directories
        var srcPath = Path.Combine(outputPath, "src");
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        var handlersPath = Path.Combine(srcPath, "handlers");
        await _filesystem.CreateDirectoryAsync(handlersPath, cancellationToken);

        var modelsPath = Path.Combine(srcPath, "models");
        await _filesystem.CreateDirectoryAsync(modelsPath, cancellationToken);

        // Cargo.toml
        var cargoContent = GenerateCargoToml(requirements.ProjectName, crateName, requirements);
        var cargoPath = Path.Combine(outputPath, "Cargo.toml");
        await _filesystem.WriteFileAsync(cargoPath, cargoContent, cancellationToken);
        createdFiles.Add(cargoPath);

        // src/main.rs
        var mainContent = GenerateMain(crateName);
        var mainPath = Path.Combine(srcPath, "main.rs");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // src/lib.rs
        var libContent = GenerateLib();
        var libPath = Path.Combine(srcPath, "lib.rs");
        await _filesystem.WriteFileAsync(libPath, libContent, cancellationToken);
        createdFiles.Add(libPath);

        // src/handlers/mod.rs
        var handlersModContent = GenerateHandlersMod();
        var handlersModPath = Path.Combine(handlersPath, "mod.rs");
        await _filesystem.WriteFileAsync(handlersModPath, handlersModContent, cancellationToken);
        createdFiles.Add(handlersModPath);

        // src/handlers/items.rs
        var itemsHandlerContent = GenerateItemsHandler();
        var itemsHandlerPath = Path.Combine(handlersPath, "items.rs");
        await _filesystem.WriteFileAsync(itemsHandlerPath, itemsHandlerContent, cancellationToken);
        createdFiles.Add(itemsHandlerPath);

        // src/handlers/health.rs
        var healthHandlerContent = GenerateHealthHandler();
        var healthHandlerPath = Path.Combine(handlersPath, "health.rs");
        await _filesystem.WriteFileAsync(healthHandlerPath, healthHandlerContent, cancellationToken);
        createdFiles.Add(healthHandlerPath);

        // src/models/mod.rs
        var modelsModContent = GenerateModelsMod();
        var modelsModPath = Path.Combine(modelsPath, "mod.rs");
        await _filesystem.WriteFileAsync(modelsModPath, modelsModContent, cancellationToken);
        createdFiles.Add(modelsModPath);

        // src/models/item.rs
        var itemModelContent = GenerateItemModel();
        var itemModelPath = Path.Combine(modelsPath, "item.rs");
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
            new() { RelativePath = "Cargo.toml", Description = "Cargo manifest" },
            new() { RelativePath = "src", IsDirectory = true, Description = "Source directory" },
            new() { RelativePath = "src/main.rs", Description = "Application entry point" },
            new() { RelativePath = "src/lib.rs", Description = "Library root" },
            new() { RelativePath = "src/handlers", IsDirectory = true, Description = "HTTP handlers" },
            new() { RelativePath = "src/handlers/mod.rs", Description = "Handlers module" },
            new() { RelativePath = "src/handlers/items.rs", Description = "Items handlers" },
            new() { RelativePath = "src/handlers/health.rs", Description = "Health handler" },
            new() { RelativePath = "src/models", IsDirectory = true, Description = "Data models" },
            new() { RelativePath = "src/models/mod.rs", Description = "Models module" },
            new() { RelativePath = "src/models/item.rs", Description = "Item model" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateCargoToml(string projectName, string crateName, ProjectRequirements requirements)
    {
        return $@"[package]
name = ""{crateName}""
version = ""0.1.0""
edition = ""2021""
description = ""{requirements.Description ?? "A Rust Actix Web API"}""

[dependencies]
actix-web = ""4""
actix-rt = ""2""
serde = {{ version = ""1"", features = [""derive""] }}
serde_json = ""1""
tokio = {{ version = ""1"", features = [""full""] }}
env_logger = ""0.11""
log = ""0.4""

[dev-dependencies]
actix-rt = ""2""

[[bin]]
name = ""{crateName}""
path = ""src/main.rs""
";
    }

    private static string GenerateMain(string crateName)
    {
        return $@"use actix_web::{{web, App, HttpServer, middleware}};
use std::env;
use log::info;

use {crateName}::handlers;

#[actix_web::main]
async fn main() -> std::io::Result<()> {{
    env_logger::init_from_env(env_logger::Env::new().default_filter_or(""info""));

    let port = env::var(""PORT"").unwrap_or_else(|_| ""8080"".to_string());
    let addr = format!(""0.0.0.0:{{}}"", port);

    info!(""Starting server at http://{{}}"", addr);

    HttpServer::new(|| {{
        App::new()
            .wrap(middleware::Logger::default())
            .app_data(web::Data::new(handlers::items::ItemStore::new()))
            .route(""/"", web::get().to(handlers::health::root))
            .route(""/health"", web::get().to(handlers::health::health_check))
            .service(
                web::scope(""/items"")
                    .route("""", web::get().to(handlers::items::get_items))
                    .route("""", web::post().to(handlers::items::create_item))
                    .route(""/{{id}}"", web::get().to(handlers::items::get_item))
                    .route(""/{{id}}"", web::delete().to(handlers::items::delete_item))
            )
    }})
    .bind(&addr)?
    .run()
    .await
}}
";
    }

    private static string GenerateLib()
    {
        return @"pub mod handlers;
pub mod models;
";
    }

    private static string GenerateHandlersMod()
    {
        return @"pub mod health;
pub mod items;
";
    }

    private static string GenerateItemsHandler()
    {
        return @"use actix_web::{web, HttpResponse, Responder};
use std::sync::Mutex;
use std::collections::HashMap;

use crate::models::item::{Item, CreateItemRequest};

pub struct ItemStore {
    items: Mutex<HashMap<u64, Item>>,
    counter: Mutex<u64>,
}

impl ItemStore {
    pub fn new() -> Self {
        Self {
            items: Mutex::new(HashMap::new()),
            counter: Mutex::new(1),
        }
    }
}

impl Default for ItemStore {
    fn default() -> Self {
        Self::new()
    }
}

pub async fn get_items(store: web::Data<ItemStore>) -> impl Responder {
    let items = store.items.lock().unwrap();
    let items_vec: Vec<&Item> = items.values().collect();
    HttpResponse::Ok().json(items_vec)
}

pub async fn get_item(
    store: web::Data<ItemStore>,
    path: web::Path<u64>,
) -> impl Responder {
    let id = path.into_inner();
    let items = store.items.lock().unwrap();

    match items.get(&id) {
        Some(item) => HttpResponse::Ok().json(item),
        None => HttpResponse::NotFound().json(serde_json::json!({
            ""error"": ""Item not found""
        })),
    }
}

pub async fn create_item(
    store: web::Data<ItemStore>,
    body: web::Json<CreateItemRequest>,
) -> impl Responder {
    let mut counter = store.counter.lock().unwrap();
    let id = *counter;
    *counter += 1;

    let item = Item {
        id,
        name: body.name.clone(),
        description: body.description.clone(),
        price: body.price,
    };

    let mut items = store.items.lock().unwrap();
    items.insert(id, item.clone());

    HttpResponse::Created().json(item)
}

pub async fn delete_item(
    store: web::Data<ItemStore>,
    path: web::Path<u64>,
) -> impl Responder {
    let id = path.into_inner();
    let mut items = store.items.lock().unwrap();

    match items.remove(&id) {
        Some(_) => HttpResponse::NoContent().finish(),
        None => HttpResponse::NotFound().json(serde_json::json!({
            ""error"": ""Item not found""
        })),
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use actix_web::{test, App};

    #[actix_rt::test]
    async fn test_get_items_empty() {
        let store = web::Data::new(ItemStore::new());
        let app = test::init_service(
            App::new()
                .app_data(store.clone())
                .route(""/items"", web::get().to(get_items))
        ).await;

        let req = test::TestRequest::get().uri(""/items"").to_request();
        let resp = test::call_service(&app, req).await;
        assert!(resp.status().is_success());
    }

    #[actix_rt::test]
    async fn test_create_item() {
        let store = web::Data::new(ItemStore::new());
        let app = test::init_service(
            App::new()
                .app_data(store.clone())
                .route(""/items"", web::post().to(create_item))
        ).await;

        let req = test::TestRequest::post()
            .uri(""/items"")
            .set_json(CreateItemRequest {
                name: ""Test"".to_string(),
                description: None,
                price: 9.99,
            })
            .to_request();

        let resp = test::call_service(&app, req).await;
        assert_eq!(resp.status(), 201);
    }
}
";
    }

    private static string GenerateHealthHandler()
    {
        return @"use actix_web::{HttpResponse, Responder};
use serde_json::json;

pub async fn root() -> impl Responder {
    HttpResponse::Ok().json(json!({
        ""message"": ""Welcome to the API"",
        ""version"": ""1.0.0""
    }))
}

pub async fn health_check() -> impl Responder {
    HttpResponse::Ok().json(json!({
        ""status"": ""healthy""
    }))
}

#[cfg(test)]
mod tests {
    use super::*;
    use actix_web::{test, App, web};

    #[actix_rt::test]
    async fn test_health_check() {
        let app = test::init_service(
            App::new().route(""/health"", web::get().to(health_check))
        ).await;

        let req = test::TestRequest::get().uri(""/health"").to_request();
        let resp = test::call_service(&app, req).await;
        assert!(resp.status().is_success());
    }
}
";
    }

    private static string GenerateModelsMod()
    {
        return @"pub mod item;
";
    }

    private static string GenerateItemModel()
    {
        return @"use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Item {
    pub id: u64,
    pub name: String,
    #[serde(skip_serializing_if = ""Option::is_none"")]
    pub description: Option<String>,
    pub price: f64,
}

#[derive(Debug, Deserialize)]
pub struct CreateItemRequest {
    pub name: String,
    pub description: Option<String>,
    pub price: f64,
}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Rust
/target/
Cargo.lock

# IDE
.idea/
.vscode/
*.swp

# Environment
.env
.env.local

# OS
.DS_Store
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Rust Actix Web REST API application."}

## Getting Started

### Prerequisites

- Rust 1.75+ (install via [rustup](https://rustup.rs/))

### Installation

```bash
cargo build
```

### Running the Application

```bash
# Development
cargo run

# Release
cargo run --release
```

The API will be available at `http://localhost:8080`.

### Testing

```bash
cargo test
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
├── src/
│   ├── handlers/
│   │   ├── mod.rs
│   │   ├── health.rs
│   │   └── items.rs
│   ├── models/
│   │   ├── mod.rs
│   │   └── item.rs
│   ├── lib.rs
│   └── main.rs
├── Cargo.toml
└── README.md
```

## Environment Variables

- `PORT` - Server port (default: 8080)
- `RUST_LOG` - Log level (default: info)

## License

MIT
";
    }

    private static string GenerateDockerfile(string projectName)
    {
        var crateName = projectName.Replace("-", "_");
        return $@"FROM rust:1.75-alpine AS builder

RUN apk add --no-cache musl-dev

WORKDIR /app
COPY Cargo.toml ./
COPY src ./src

RUN cargo build --release

FROM alpine:latest

RUN apk add --no-cache ca-certificates

WORKDIR /app
COPY --from=builder /app/target/release/{crateName} .

RUN adduser -D appuser
USER appuser

EXPOSE 8080
CMD [""./{crateName}""]
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
      - RUST_LOG=info
      - PORT=8080
";
    }
}
