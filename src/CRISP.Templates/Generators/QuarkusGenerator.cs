using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Java Quarkus projects.
/// </summary>
public sealed class QuarkusGenerator : IProjectGenerator
{
    private readonly ILogger<QuarkusGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public QuarkusGenerator(
        ILogger<QuarkusGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "java-quarkus";
    public string TemplateName => "Java Quarkus";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Java &&
               requirements.Framework == ProjectFramework.Quarkus;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Quarkus project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var packagePath = "com/example/" + requirements.ProjectName.Replace("-", "").ToLowerInvariant();
        var packageName = "com.example." + requirements.ProjectName.Replace("-", "").ToLowerInvariant();

        // Create directories
        var srcMainJava = Path.Combine(outputPath, "src", "main", "java", packagePath.Replace('/', Path.DirectorySeparatorChar));
        var srcMainResources = Path.Combine(outputPath, "src", "main", "resources");
        var srcTestJava = Path.Combine(outputPath, "src", "test", "java", packagePath.Replace('/', Path.DirectorySeparatorChar));

        await _filesystem.CreateDirectoryAsync(srcMainJava, cancellationToken);
        await _filesystem.CreateDirectoryAsync(srcMainResources, cancellationToken);
        await _filesystem.CreateDirectoryAsync(srcTestJava, cancellationToken);

        // pom.xml
        var pomContent = GeneratePom(requirements.ProjectName, packageName, requirements);
        var pomPath = Path.Combine(outputPath, "pom.xml");
        await _filesystem.WriteFileAsync(pomPath, pomContent, cancellationToken);
        createdFiles.Add(pomPath);

        // Main resource classes
        var itemResourceContent = GenerateItemResource(packageName);
        var itemResourcePath = Path.Combine(srcMainJava, "ItemResource.java");
        await _filesystem.WriteFileAsync(itemResourcePath, itemResourceContent, cancellationToken);
        createdFiles.Add(itemResourcePath);

        var healthResourceContent = GenerateHealthResource(packageName);
        var healthResourcePath = Path.Combine(srcMainJava, "HealthResource.java");
        await _filesystem.WriteFileAsync(healthResourcePath, healthResourceContent, cancellationToken);
        createdFiles.Add(healthResourcePath);

        // Model classes
        var itemContent = GenerateItem(packageName);
        var itemPath = Path.Combine(srcMainJava, "Item.java");
        await _filesystem.WriteFileAsync(itemPath, itemContent, cancellationToken);
        createdFiles.Add(itemPath);

        var createItemRequestContent = GenerateCreateItemRequest(packageName);
        var createItemRequestPath = Path.Combine(srcMainJava, "CreateItemRequest.java");
        await _filesystem.WriteFileAsync(createItemRequestPath, createItemRequestContent, cancellationToken);
        createdFiles.Add(createItemRequestPath);

        // Service class
        var itemServiceContent = GenerateItemService(packageName);
        var itemServicePath = Path.Combine(srcMainJava, "ItemService.java");
        await _filesystem.WriteFileAsync(itemServicePath, itemServiceContent, cancellationToken);
        createdFiles.Add(itemServicePath);

        // application.properties
        var appPropsContent = GenerateApplicationProperties(requirements.ProjectName);
        var appPropsPath = Path.Combine(srcMainResources, "application.properties");
        await _filesystem.WriteFileAsync(appPropsPath, appPropsContent, cancellationToken);
        createdFiles.Add(appPropsPath);

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var itemResourceTestContent = GenerateItemResourceTest(packageName);
            var itemResourceTestPath = Path.Combine(srcTestJava, "ItemResourceTest.java");
            await _filesystem.WriteFileAsync(itemResourceTestPath, itemResourceTestContent, cancellationToken);
            createdFiles.Add(itemResourceTestPath);

            var healthResourceTestContent = GenerateHealthResourceTest(packageName);
            var healthResourceTestPath = Path.Combine(srcTestJava, "HealthResourceTest.java");
            await _filesystem.WriteFileAsync(healthResourceTestPath, healthResourceTestContent, cancellationToken);
            createdFiles.Add(healthResourceTestPath);
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
            var dockerDir = Path.Combine(srcMainResources, "docker");
            await _filesystem.CreateDirectoryAsync(dockerDir, cancellationToken);

            var dockerfileJvmContent = GenerateDockerfileJvm();
            var dockerfileJvmPath = Path.Combine(outputPath, "src", "main", "docker", "Dockerfile.jvm");
            await _filesystem.CreateDirectoryAsync(Path.Combine(outputPath, "src", "main", "docker"), cancellationToken);
            await _filesystem.WriteFileAsync(dockerfileJvmPath, dockerfileJvmContent, cancellationToken);
            createdFiles.Add(dockerfileJvmPath);

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
            new() { RelativePath = "pom.xml", Description = "Maven build file" },
            new() { RelativePath = "src/main/java", IsDirectory = true, Description = "Java sources" },
            new() { RelativePath = "src/main/java/.../ItemResource.java", Description = "Items REST resource" },
            new() { RelativePath = "src/main/java/.../HealthResource.java", Description = "Health REST resource" },
            new() { RelativePath = "src/main/java/.../Item.java", Description = "Item model" },
            new() { RelativePath = "src/main/java/.../CreateItemRequest.java", Description = "Create item DTO" },
            new() { RelativePath = "src/main/java/.../ItemService.java", Description = "Item service" },
            new() { RelativePath = "src/main/resources/application.properties", Description = "Quarkus configuration" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "src/test/java/.../ItemResourceTest.java", Description = "Items resource tests" });
            files.Add(new PlannedFile { RelativePath = "src/test/java/.../HealthResourceTest.java", Description = "Health resource tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "src/main/docker/Dockerfile.jvm", Description = "JVM Dockerfile" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GeneratePom(string projectName, string packageName, ProjectRequirements requirements)
    {
        return $@"<?xml version=""1.0""?>
<project xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd""
         xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <modelVersion>4.0.0</modelVersion>
    <groupId>{packageName}</groupId>
    <artifactId>{projectName}</artifactId>
    <version>1.0.0-SNAPSHOT</version>

    <properties>
        <compiler-plugin.version>3.12.1</compiler-plugin.version>
        <maven.compiler.release>21</maven.compiler.release>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        <quarkus.platform.artifact-id>quarkus-bom</quarkus.platform.artifact-id>
        <quarkus.platform.group-id>io.quarkus.platform</quarkus.platform.group-id>
        <quarkus.platform.version>3.7.0</quarkus.platform.version>
        <surefire-plugin.version>3.2.3</surefire-plugin.version>
    </properties>

    <dependencyManagement>
        <dependencies>
            <dependency>
                <groupId>${{quarkus.platform.group-id}}</groupId>
                <artifactId>${{quarkus.platform.artifact-id}}</artifactId>
                <version>${{quarkus.platform.version}}</version>
                <type>pom</type>
                <scope>import</scope>
            </dependency>
        </dependencies>
    </dependencyManagement>

    <dependencies>
        <dependency>
            <groupId>io.quarkus</groupId>
            <artifactId>quarkus-arc</artifactId>
        </dependency>
        <dependency>
            <groupId>io.quarkus</groupId>
            <artifactId>quarkus-resteasy-reactive</artifactId>
        </dependency>
        <dependency>
            <groupId>io.quarkus</groupId>
            <artifactId>quarkus-resteasy-reactive-jackson</artifactId>
        </dependency>
        <dependency>
            <groupId>io.quarkus</groupId>
            <artifactId>quarkus-smallrye-health</artifactId>
        </dependency>
        <dependency>
            <groupId>io.quarkus</groupId>
            <artifactId>quarkus-junit5</artifactId>
            <scope>test</scope>
        </dependency>
        <dependency>
            <groupId>io.rest-assured</groupId>
            <artifactId>rest-assured</artifactId>
            <scope>test</scope>
        </dependency>
    </dependencies>

    <build>
        <plugins>
            <plugin>
                <groupId>${{quarkus.platform.group-id}}</groupId>
                <artifactId>quarkus-maven-plugin</artifactId>
                <version>${{quarkus.platform.version}}</version>
                <extensions>true</extensions>
                <executions>
                    <execution>
                        <goals>
                            <goal>build</goal>
                            <goal>generate-code</goal>
                            <goal>generate-code-tests</goal>
                        </goals>
                    </execution>
                </executions>
            </plugin>
            <plugin>
                <artifactId>maven-compiler-plugin</artifactId>
                <version>${{compiler-plugin.version}}</version>
                <configuration>
                    <compilerArgs>
                        <arg>-parameters</arg>
                    </compilerArgs>
                </configuration>
            </plugin>
            <plugin>
                <artifactId>maven-surefire-plugin</artifactId>
                <version>${{surefire-plugin.version}}</version>
                <configuration>
                    <systemPropertyVariables>
                        <java.util.logging.manager>org.jboss.logmanager.LogManager</java.util.logging.manager>
                        <maven.home>${{maven.home}}</maven.home>
                    </systemPropertyVariables>
                </configuration>
            </plugin>
        </plugins>
    </build>

    <profiles>
        <profile>
            <id>native</id>
            <activation>
                <property>
                    <name>native</name>
                </property>
            </activation>
            <properties>
                <quarkus.package.type>native</quarkus.package.type>
            </properties>
        </profile>
    </profiles>
</project>
";
    }

    private static string GenerateItemResource(string packageName)
    {
        return $@"package {packageName};

import jakarta.inject.Inject;
import jakarta.ws.rs.*;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import java.util.List;

@Path(""/items"")
@Produces(MediaType.APPLICATION_JSON)
@Consumes(MediaType.APPLICATION_JSON)
public class ItemResource {{

    @Inject
    ItemService itemService;

    @GET
    public List<Item> getAll() {{
        return itemService.getAll();
    }}

    @GET
    @Path(""/{{id}}"")
    public Response getById(@PathParam(""id"") Long id) {{
        Item item = itemService.getById(id);
        if (item == null) {{
            return Response.status(Response.Status.NOT_FOUND)
                    .entity(new ErrorResponse(""Item not found""))
                    .build();
        }}
        return Response.ok(item).build();
    }}

    @POST
    public Response create(CreateItemRequest request) {{
        Item item = itemService.create(request);
        return Response.status(Response.Status.CREATED).entity(item).build();
    }}

    @DELETE
    @Path(""/{{id}}"")
    public Response delete(@PathParam(""id"") Long id) {{
        boolean deleted = itemService.delete(id);
        if (!deleted) {{
            return Response.status(Response.Status.NOT_FOUND)
                    .entity(new ErrorResponse(""Item not found""))
                    .build();
        }}
        return Response.noContent().build();
    }}

    public static class ErrorResponse {{
        public String error;

        public ErrorResponse(String error) {{
            this.error = error;
        }}
    }}
}}
";
    }

    private static string GenerateHealthResource(string packageName)
    {
        return $@"package {packageName};

import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import java.util.Map;

@Path(""/"")
@Produces(MediaType.APPLICATION_JSON)
public class HealthResource {{

    @GET
    public Map<String, String> root() {{
        return Map.of(
            ""message"", ""Welcome to the API"",
            ""version"", ""1.0.0""
        );
    }}

    @GET
    @Path(""/health"")
    public Map<String, String> health() {{
        return Map.of(""status"", ""healthy"");
    }}
}}
";
    }

    private static string GenerateItem(string packageName)
    {
        return $@"package {packageName};

public class Item {{
    private Long id;
    private String name;
    private String description;
    private Double price;

    public Item() {{}}

    public Item(Long id, String name, String description, Double price) {{
        this.id = id;
        this.name = name;
        this.description = description;
        this.price = price;
    }}

    public Long getId() {{ return id; }}
    public void setId(Long id) {{ this.id = id; }}

    public String getName() {{ return name; }}
    public void setName(String name) {{ this.name = name; }}

    public String getDescription() {{ return description; }}
    public void setDescription(String description) {{ this.description = description; }}

    public Double getPrice() {{ return price; }}
    public void setPrice(Double price) {{ this.price = price; }}
}}
";
    }

    private static string GenerateCreateItemRequest(string packageName)
    {
        return $@"package {packageName};

public class CreateItemRequest {{
    private String name;
    private String description;
    private Double price;

    public String getName() {{ return name; }}
    public void setName(String name) {{ this.name = name; }}

    public String getDescription() {{ return description; }}
    public void setDescription(String description) {{ this.description = description; }}

    public Double getPrice() {{ return price; }}
    public void setPrice(Double price) {{ this.price = price; }}
}}
";
    }

    private static string GenerateItemService(string packageName)
    {
        return $@"package {packageName};

import jakarta.enterprise.context.ApplicationScoped;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@ApplicationScoped
public class ItemService {{

    private final Map<Long, Item> items = new ConcurrentHashMap<>();
    private final AtomicLong counter = new AtomicLong(1);

    public List<Item> getAll() {{
        return new ArrayList<>(items.values());
    }}

    public Item getById(Long id) {{
        return items.get(id);
    }}

    public Item create(CreateItemRequest request) {{
        Long id = counter.getAndIncrement();
        Item item = new Item(id, request.getName(), request.getDescription(), request.getPrice());
        items.put(id, item);
        return item;
    }}

    public boolean delete(Long id) {{
        return items.remove(id) != null;
    }}
}}
";
    }

    private static string GenerateApplicationProperties(string projectName)
    {
        return $@"# Application
quarkus.application.name={projectName}

# HTTP
quarkus.http.port=8080

# Health
quarkus.smallrye-health.root-path=/q/health

# Logging
quarkus.log.level=INFO
quarkus.log.console.enable=true
quarkus.log.console.format=%d{{HH:mm:ss}} %-5p [%c{{2.}}] (%t) %s%e%n
";
    }

    private static string GenerateItemResourceTest(string packageName)
    {
        return $@"package {packageName};

import io.quarkus.test.junit.QuarkusTest;
import org.junit.jupiter.api.Test;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;
import static org.hamcrest.Matchers.greaterThanOrEqualTo;

@QuarkusTest
public class ItemResourceTest {{

    @Test
    public void testGetAllItems() {{
        given()
            .when().get(""/items"")
            .then()
            .statusCode(200);
    }}

    @Test
    public void testCreateItem() {{
        given()
            .contentType(""application/json"")
            .body(""{{\\""name\\"": \\""Test Item\\"", \\""price\\"": 9.99}}"")
            .when().post(""/items"")
            .then()
            .statusCode(201)
            .body(""name"", is(""Test Item""));
    }}

    @Test
    public void testGetItemNotFound() {{
        given()
            .when().get(""/items/99999"")
            .then()
            .statusCode(404);
    }}
}}
";
    }

    private static string GenerateHealthResourceTest(string packageName)
    {
        return $@"package {packageName};

import io.quarkus.test.junit.QuarkusTest;
import org.junit.jupiter.api.Test;

import static io.restassured.RestAssured.given;
import static org.hamcrest.CoreMatchers.is;

@QuarkusTest
public class HealthResourceTest {{

    @Test
    public void testRootEndpoint() {{
        given()
            .when().get(""/"")
            .then()
            .statusCode(200)
            .body(""message"", is(""Welcome to the API""));
    }}

    @Test
    public void testHealthEndpoint() {{
        given()
            .when().get(""/health"")
            .then()
            .statusCode(200)
            .body(""status"", is(""healthy""));
    }}
}}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Maven
target/
pom.xml.tag
pom.xml.releaseBackup
pom.xml.versionsBackup
pom.xml.next
release.properties

# IDE
.idea/
*.iml
.vscode/
*.swp
.project
.classpath
.settings/

# OS
.DS_Store

# Quarkus
.quarkus/
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Java Quarkus REST API application."}

## Getting Started

### Prerequisites

- Java 21+
- Maven 3.9+

### Running in Development Mode

```bash
./mvnw quarkus:dev
```

The API will be available at `http://localhost:8080`.

Quarkus Dev UI is available at `http://localhost:8080/q/dev/`.

### Building

```bash
./mvnw package
```

### Running Tests

```bash
./mvnw test
```

### Building Native Executable

```bash
./mvnw package -Pnative
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
│   ├── main/
│   │   ├── java/.../
│   │   │   ├── ItemResource.java
│   │   │   ├── HealthResource.java
│   │   │   ├── Item.java
│   │   │   ├── CreateItemRequest.java
│   │   │   └── ItemService.java
│   │   └── resources/
│   │       └── application.properties
│   └── test/
│       └── java/.../
│           ├── ItemResourceTest.java
│           └── HealthResourceTest.java
├── pom.xml
└── README.md
```

## Features

- Quarkus RESTEasy Reactive
- Jackson JSON serialization
- SmallRye Health (MicroProfile Health)
- Dev mode with live reload
- Native compilation support

## License

MIT
";
    }

    private static string GenerateDockerfileJvm()
    {
        return @"FROM registry.access.redhat.com/ubi8/openjdk-21:1.18

ENV LANGUAGE='en_US:en'

COPY target/quarkus-app/lib/ /deployments/lib/
COPY target/quarkus-app/*.jar /deployments/
COPY target/quarkus-app/app/ /deployments/app/
COPY target/quarkus-app/quarkus/ /deployments/quarkus/

EXPOSE 8080
USER 185
ENV JAVA_OPTS=""-Dquarkus.http.host=0.0.0.0 -Djava.util.logging.manager=org.jboss.logmanager.LogManager""
ENV JAVA_APP_JAR=""/deployments/quarkus-run.jar""

ENTRYPOINT [ ""/opt/jboss/container/java/run/run-java.sh"" ]
";
    }

    private static string GenerateDockerCompose(string projectName)
    {
        return $@"version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: src/main/docker/Dockerfile.jvm
    container_name: {projectName}
    ports:
      - ""8080:8080""
    environment:
      - QUARKUS_HTTP_PORT=8080
";
    }
}
