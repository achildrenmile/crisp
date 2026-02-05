using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Java Spring Boot projects.
/// </summary>
public sealed class SpringBootGenerator : IProjectGenerator
{
    private readonly ILogger<SpringBootGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public SpringBootGenerator(
        ILogger<SpringBootGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "java-springboot";
    public string TemplateName => "Java Spring Boot";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Java &&
               requirements.Framework == ProjectFramework.SpringBoot;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Spring Boot project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName.Replace("-", "");
        var packageName = $"com.example.{projectName.ToLowerInvariant()}";
        var packagePath = packageName.Replace(".", "/");

        // Create directory structure
        var srcMainJava = Path.Combine(outputPath, "src", "main", "java", packagePath);
        var srcMainResources = Path.Combine(outputPath, "src", "main", "resources");
        var srcTestJava = Path.Combine(outputPath, "src", "test", "java", packagePath);

        await _filesystem.CreateDirectoryAsync(srcMainJava, cancellationToken);
        await _filesystem.CreateDirectoryAsync(Path.Combine(srcMainJava, "controller"), cancellationToken);
        await _filesystem.CreateDirectoryAsync(Path.Combine(srcMainJava, "model"), cancellationToken);
        await _filesystem.CreateDirectoryAsync(Path.Combine(srcMainJava, "service"), cancellationToken);
        await _filesystem.CreateDirectoryAsync(Path.Combine(srcMainJava, "repository"), cancellationToken);
        await _filesystem.CreateDirectoryAsync(srcMainResources, cancellationToken);
        await _filesystem.CreateDirectoryAsync(srcTestJava, cancellationToken);

        // Main Application class
        var appContent = GenerateApplication(packageName, projectName);
        var appPath = Path.Combine(srcMainJava, $"{ToPascalCase(projectName)}Application.java");
        await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
        createdFiles.Add(appPath);

        // Controller
        var controllerContent = GenerateController(packageName);
        var controllerPath = Path.Combine(srcMainJava, "controller", "ItemController.java");
        await _filesystem.WriteFileAsync(controllerPath, controllerContent, cancellationToken);
        createdFiles.Add(controllerPath);

        // Model
        var modelContent = GenerateModel(packageName);
        var modelPath = Path.Combine(srcMainJava, "model", "Item.java");
        await _filesystem.WriteFileAsync(modelPath, modelContent, cancellationToken);
        createdFiles.Add(modelPath);

        // Service
        var serviceContent = GenerateService(packageName);
        var servicePath = Path.Combine(srcMainJava, "service", "ItemService.java");
        await _filesystem.WriteFileAsync(servicePath, serviceContent, cancellationToken);
        createdFiles.Add(servicePath);

        // application.properties
        var propsContent = GenerateApplicationProperties(requirements.ProjectName);
        var propsPath = Path.Combine(srcMainResources, "application.properties");
        await _filesystem.WriteFileAsync(propsPath, propsContent, cancellationToken);
        createdFiles.Add(propsPath);

        // pom.xml
        var pomContent = GeneratePom(requirements.ProjectName, packageName, requirements);
        var pomPath = Path.Combine(outputPath, "pom.xml");
        await _filesystem.WriteFileAsync(pomPath, pomContent, cancellationToken);
        createdFiles.Add(pomPath);

        // Test class
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testContent = GenerateApplicationTest(packageName, projectName);
            var testPath = Path.Combine(srcTestJava, $"{ToPascalCase(projectName)}ApplicationTests.java");
            await _filesystem.WriteFileAsync(testPath, testContent, cancellationToken);
            createdFiles.Add(testPath);

            var controllerTestContent = GenerateControllerTest(packageName);
            var controllerTestPath = Path.Combine(srcTestJava, "ItemControllerTests.java");
            await _filesystem.WriteFileAsync(controllerTestPath, controllerTestContent, cancellationToken);
            createdFiles.Add(controllerTestPath);
        }

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

        // Maven wrapper
        var mvnwContent = GenerateMvnw();
        var mvnwPath = Path.Combine(outputPath, "mvnw");
        await _filesystem.WriteFileAsync(mvnwPath, mvnwContent, cancellationToken);
        createdFiles.Add(mvnwPath);

        _logger.LogInformation("Generated {Count} files", createdFiles.Count);
        return createdFiles;
    }

    public Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var projectName = requirements.ProjectName.Replace("-", "");
        var packagePath = $"com/example/{projectName.ToLowerInvariant()}";

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "src/main/java", IsDirectory = true, Description = "Java source files" },
            new() { RelativePath = $"src/main/java/{packagePath}/{ToPascalCase(projectName)}Application.java", Description = "Main application class" },
            new() { RelativePath = $"src/main/java/{packagePath}/controller/ItemController.java", Description = "REST controller" },
            new() { RelativePath = $"src/main/java/{packagePath}/model/Item.java", Description = "Item model" },
            new() { RelativePath = $"src/main/java/{packagePath}/service/ItemService.java", Description = "Item service" },
            new() { RelativePath = "src/main/resources", IsDirectory = true, Description = "Resources" },
            new() { RelativePath = "src/main/resources/application.properties", Description = "Application configuration" },
            new() { RelativePath = "pom.xml", Description = "Maven build file" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = "mvnw", Description = "Maven wrapper" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "src/test/java", IsDirectory = true, Description = "Test source files" });
            files.Add(new PlannedFile { RelativePath = $"src/test/java/{packagePath}/{ToPascalCase(projectName)}ApplicationTests.java", Description = "Application tests" });
            files.Add(new PlannedFile { RelativePath = $"src/test/java/{packagePath}/ItemControllerTests.java", Description = "Controller tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    private static string GenerateApplication(string packageName, string projectName)
    {
        return $@"package {packageName};

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class {ToPascalCase(projectName)}Application {{

    public static void main(String[] args) {{
        SpringApplication.run({ToPascalCase(projectName)}Application.class, args);
    }}

}}
";
    }

    private static string GenerateController(string packageName)
    {
        return $@"package {packageName}.controller;

import {packageName}.model.Item;
import {packageName}.service.ItemService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping(""/api/items"")
public class ItemController {{

    private final ItemService itemService;

    public ItemController(ItemService itemService) {{
        this.itemService = itemService;
    }}

    @GetMapping
    public List<Item> getAllItems() {{
        return itemService.getAllItems();
    }}

    @GetMapping(""/{{id}}"")
    public ResponseEntity<Item> getItemById(@PathVariable Long id) {{
        return itemService.getItemById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }}

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public Item createItem(@RequestBody Item item) {{
        return itemService.createItem(item);
    }}

    @DeleteMapping(""/{{id}}"")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteItem(@PathVariable Long id) {{
        itemService.deleteItem(id);
    }}

    @GetMapping(""/health"")
    public ResponseEntity<String> health() {{
        return ResponseEntity.ok(""healthy"");
    }}
}}
";
    }

    private static string GenerateModel(string packageName)
    {
        return $@"package {packageName}.model;

public class Item {{

    private Long id;
    private String name;
    private String description;
    private Double price;

    public Item() {{
    }}

    public Item(Long id, String name, String description, Double price) {{
        this.id = id;
        this.name = name;
        this.description = description;
        this.price = price;
    }}

    public Long getId() {{
        return id;
    }}

    public void setId(Long id) {{
        this.id = id;
    }}

    public String getName() {{
        return name;
    }}

    public void setName(String name) {{
        this.name = name;
    }}

    public String getDescription() {{
        return description;
    }}

    public void setDescription(String description) {{
        this.description = description;
    }}

    public Double getPrice() {{
        return price;
    }}

    public void setPrice(Double price) {{
        this.price = price;
    }}
}}
";
    }

    private static string GenerateService(string packageName)
    {
        return $@"package {packageName}.service;

import {packageName}.model.Item;
import org.springframework.stereotype.Service;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Service
public class ItemService {{

    private final Map<Long, Item> items = new ConcurrentHashMap<>();
    private final AtomicLong idCounter = new AtomicLong();

    public List<Item> getAllItems() {{
        return new ArrayList<>(items.values());
    }}

    public Optional<Item> getItemById(Long id) {{
        return Optional.ofNullable(items.get(id));
    }}

    public Item createItem(Item item) {{
        Long id = idCounter.incrementAndGet();
        item.setId(id);
        items.put(id, item);
        return item;
    }}

    public void deleteItem(Long id) {{
        items.remove(id);
    }}
}}
";
    }

    private static string GenerateApplicationProperties(string projectName)
    {
        return $@"spring.application.name={projectName}
server.port=8080

# Logging
logging.level.root=INFO
logging.level.com.example=DEBUG
";
    }

    private static string GeneratePom(string projectName, string packageName, ProjectRequirements requirements)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>

    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.2.0</version>
        <relativePath/>
    </parent>

    <groupId>com.example</groupId>
    <artifactId>{projectName}</artifactId>
    <version>0.0.1-SNAPSHOT</version>
    <name>{projectName}</name>
    <description>{requirements.Description ?? "Spring Boot application"}</description>

    <properties>
        <java.version>21</java.version>
    </properties>

    <dependencies>
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-web</artifactId>
        </dependency>

        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-actuator</artifactId>
        </dependency>

        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-test</artifactId>
            <scope>test</scope>
        </dependency>
    </dependencies>

    <build>
        <plugins>
            <plugin>
                <groupId>org.springframework.boot</groupId>
                <artifactId>spring-boot-maven-plugin</artifactId>
            </plugin>
        </plugins>
    </build>

</project>
";
    }

    private static string GenerateApplicationTest(string packageName, string projectName)
    {
        return $@"package {packageName};

import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;

@SpringBootTest
class {ToPascalCase(projectName)}ApplicationTests {{

    @Test
    void contextLoads() {{
    }}

}}
";
    }

    private static string GenerateControllerTest(string packageName)
    {
        return $@"package {packageName};

import {packageName}.controller.ItemController;
import {packageName}.model.Item;
import {packageName}.service.ItemService;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

import java.util.Arrays;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

@WebMvcTest(ItemController.class)
class ItemControllerTests {{

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private ItemService itemService;

    @Test
    void getAllItems_ReturnsItemsList() throws Exception {{
        when(itemService.getAllItems()).thenReturn(
            Arrays.asList(new Item(1L, ""Test"", ""Description"", 9.99))
        );

        mockMvc.perform(get(""/api/items""))
                .andExpect(status().isOk())
                .andExpect(content().contentType(MediaType.APPLICATION_JSON))
                .andExpect(jsonPath(""$[0].name"").value(""Test""));
    }}

    @Test
    void health_ReturnsHealthy() throws Exception {{
        mockMvc.perform(get(""/api/items/health""))
                .andExpect(status().isOk())
                .andExpect(content().string(""healthy""));
    }}
}}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Compiled class files
*.class

# Log files
*.log

# Package files
*.jar
*.war
*.nar
*.ear
*.zip
*.tar.gz
*.rar

# Maven
target/
pom.xml.tag
pom.xml.releaseBackup
pom.xml.versionsBackup
pom.xml.next
release.properties

# IDE
.idea/
*.iml
*.iws
.project
.classpath
.settings/
.vscode/

# OS
.DS_Store

# Maven wrapper
!.mvn/wrapper/maven-wrapper.jar
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Spring Boot REST API application."}

## Getting Started

### Prerequisites

- Java 21+
- Maven 3.9+

### Build

```bash
./mvnw clean package
```

### Run

```bash
./mvnw spring-boot:run
```

The API will be available at `http://localhost:8080`.

### Test

```bash
./mvnw test
```

## API Endpoints

- `GET /api/items` - List all items
- `GET /api/items/{{id}}` - Get item by ID
- `POST /api/items` - Create item
- `DELETE /api/items/{{id}}` - Delete item
- `GET /api/items/health` - Health check

## Project Structure

```
{projectName}/
├── src/
│   ├── main/
│   │   ├── java/com/example/{projectName.ToLowerInvariant().Replace("-", "")}/
│   │   │   ├── controller/
│   │   │   ├── model/
│   │   │   ├── service/
│   │   │   └── Application.java
│   │   └── resources/
│   │       └── application.properties
│   └── test/
├── pom.xml
└── README.md
```

## License

MIT
";
    }

    private static string GenerateDockerfile(string projectName)
    {
        return $@"FROM eclipse-temurin:21-jdk-alpine AS builder

WORKDIR /app
COPY pom.xml .
COPY mvnw .
COPY .mvn .mvn
RUN ./mvnw dependency:go-offline

COPY src src
RUN ./mvnw package -DskipTests

FROM eclipse-temurin:21-jre-alpine

WORKDIR /app
COPY --from=builder /app/target/*.jar app.jar

RUN addgroup -S spring && adduser -S spring -G spring
USER spring:spring

EXPOSE 8080
ENTRYPOINT [""java"", ""-jar"", ""app.jar""]
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
      - SPRING_PROFILES_ACTIVE=prod
";
    }

    private static string GenerateMvnw()
    {
        return @"#!/bin/sh
# Maven Wrapper script
# Download and run Maven

MAVEN_VERSION=""3.9.6""
MAVEN_HOME=""$HOME/.m2/wrapper/dists/apache-maven-$MAVEN_VERSION""

if [ ! -d ""$MAVEN_HOME"" ]; then
    echo ""Downloading Maven $MAVEN_VERSION...""
    mkdir -p ""$MAVEN_HOME""
    curl -fsSL ""https://archive.apache.org/dist/maven/maven-3/$MAVEN_VERSION/binaries/apache-maven-$MAVEN_VERSION-bin.tar.gz"" | tar xzf - -C ""$MAVEN_HOME"" --strip-components=1
fi

exec ""$MAVEN_HOME/bin/mvn"" ""$@""
";
    }
}
