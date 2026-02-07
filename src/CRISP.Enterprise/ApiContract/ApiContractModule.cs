using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.ApiContract;

/// <summary>
/// Generates an initial API contract stub (OpenAPI) and testing tools (Bruno collection)
/// for API projects.
/// </summary>
public sealed class ApiContractModule : IEnterpriseModule
{
    private readonly ILogger<ApiContractModule> _logger;

    public ApiContractModule(ILogger<ApiContractModule> logger)
    {
        _logger = logger;
    }

    public string Id => "api-contract";
    public string DisplayName => "API Contract";
    public int Order => 900;

    public bool ShouldRun(ProjectContext context) => context.IsApiProject;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();

        try
        {
            // Create docs/api directory
            var apiDocsDir = Path.Combine(context.WorkspacePath, "docs", "api");
            Directory.CreateDirectory(apiDocsDir);

            // Generate OpenAPI stub
            var openApiPath = Path.Combine(apiDocsDir, "openapi.yaml");
            var openApiContent = GenerateOpenApiStub(context);
            await File.WriteAllTextAsync(openApiPath, openApiContent, cancellationToken);
            filesCreated.Add("docs/api/openapi.yaml");

            // Generate Bruno collection
            var brunoFiles = await GenerateBrunoCollectionAsync(context, cancellationToken);
            filesCreated.AddRange(brunoFiles);

            // Record ADR
            context.DecisionCollector.Record(
                title: "Define API contract using OpenAPI 3.1 with Bruno collection for testing",
                context: "API projects benefit from having a documented contract before implementation, enabling parallel frontend/backend development and contract testing.",
                decision: "Generate OpenAPI 3.1 specification stub and Bruno collection for API testing.",
                rationale: "OpenAPI is the industry standard for REST API documentation. Bruno provides a Git-friendly, file-based alternative to Postman for API testing.",
                category: AdrCategory.Interfaces,
                alternatives: new Dictionary<string, string>
                {
                    ["Auto-generated OpenAPI only"] = "Frameworks like FastAPI and ASP.NET Core can auto-generate; manual stub ensures early documentation",
                    ["Postman collection"] = "Bruno chosen for Git-friendly file-based format",
                    ["No API documentation"] = "Would make integration difficult for API consumers"
                },
                consequences: [
                    "API structure is documented from project start",
                    "Bruno collection enables quick API testing",
                    "Contract can be validated in CI pipeline"
                ],
                relatedFiles: filesCreated
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API contract module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateOpenApiStub(ProjectContext context)
    {
        var teamName = context.TeamName ?? "API Team";
        var teamEmail = context.TeamEmail ?? "api@example.com";

        return $"""
            openapi: 3.1.0
            info:
              title: {context.ProjectName} API
              version: 0.1.0
              description: |
                {context.ProjectDescription ?? $"API for {context.ProjectName}."}

                ## Authentication

                TODO: Document authentication requirements.

                ## Rate Limiting

                TODO: Document rate limiting if applicable.

              contact:
                name: {teamName}
                email: {teamEmail}

            servers:
              - url: http://localhost:{context.Port}
                description: Local development
              - url: https://staging.example.com
                description: Staging environment

            tags:
              - name: health
                description: Health check endpoints
              - name: items
                description: Item management (example)

            paths:
              /healthz:
                get:
                  summary: Liveness probe
                  description: Returns 200 if the service is alive.
                  operationId: getHealth
                  tags:
                    - health
                  responses:
                    '200':
                      description: Service is healthy
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/HealthResponse'

              /ready:
                get:
                  summary: Readiness probe
                  description: Returns 200 if the service is ready to accept traffic.
                  operationId: getReady
                  tags:
                    - health
                  responses:
                    '200':
                      description: Service is ready
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/HealthResponse'
                    '503':
                      description: Service is not ready

              /api/items:
                get:
                  summary: List items
                  description: Returns a list of items.
                  operationId: listItems
                  tags:
                    - items
                  parameters:
                    - name: limit
                      in: query
                      description: Maximum number of items to return
                      schema:
                        type: integer
                        default: 10
                        maximum: 100
                    - name: offset
                      in: query
                      description: Number of items to skip
                      schema:
                        type: integer
                        default: 0
                  responses:
                    '200':
                      description: List of items
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/ItemList'

                post:
                  summary: Create item
                  description: Creates a new item.
                  operationId: createItem
                  tags:
                    - items
                  requestBody:
                    required: true
                    content:
                      application/json:
                        schema:
                          $ref: '#/components/schemas/CreateItemRequest'
                  responses:
                    '201':
                      description: Item created
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Item'
                    '400':
                      description: Invalid request

              /api/items/{{id}}:
                get:
                  summary: Get item
                  description: Returns a single item by ID.
                  operationId: getItem
                  tags:
                    - items
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: string
                  responses:
                    '200':
                      description: Item details
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Item'
                    '404':
                      description: Item not found

            components:
              schemas:
                HealthResponse:
                  type: object
                  properties:
                    status:
                      type: string
                      example: healthy
                  required:
                    - status

                Item:
                  type: object
                  properties:
                    id:
                      type: string
                      example: "123"
                    name:
                      type: string
                      example: "Example Item"
                    createdAt:
                      type: string
                      format: date-time
                  required:
                    - id
                    - name

                ItemList:
                  type: object
                  properties:
                    items:
                      type: array
                      items:
                        $ref: '#/components/schemas/Item'
                    total:
                      type: integer
                  required:
                    - items
                    - total

                CreateItemRequest:
                  type: object
                  properties:
                    name:
                      type: string
                      minLength: 1
                      maxLength: 100
                  required:
                    - name
            """;
    }

    private static async Task<List<string>> GenerateBrunoCollectionAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        var brunoDir = Path.Combine(context.WorkspacePath, "docs", "api", "bruno");
        Directory.CreateDirectory(brunoDir);

        // Generate bruno.json
        var brunoJsonPath = Path.Combine(brunoDir, "bruno.json");
        var brunoJson = $$"""
            {
              "version": "1",
              "name": "{{context.ProjectName}} API",
              "type": "collection"
            }
            """;
        await File.WriteAllTextAsync(brunoJsonPath, brunoJson, cancellationToken);
        files.Add("docs/api/bruno/bruno.json");

        // Create environments directory
        var envsDir = Path.Combine(brunoDir, "environments");
        Directory.CreateDirectory(envsDir);

        // Development environment
        var devEnvPath = Path.Combine(envsDir, "development.bru");
        var devEnv = $"""
            vars {{
              baseUrl: http://localhost:{context.Port}
            }}
            """;
        await File.WriteAllTextAsync(devEnvPath, devEnv, cancellationToken);
        files.Add("docs/api/bruno/environments/development.bru");

        // Staging environment
        var stagingEnvPath = Path.Combine(envsDir, "staging.bru");
        var stagingEnv = """
            vars {
              baseUrl: https://staging.example.com
            }
            """;
        await File.WriteAllTextAsync(stagingEnvPath, stagingEnv, cancellationToken);
        files.Add("docs/api/bruno/environments/staging.bru");

        // Create health requests directory
        var healthDir = Path.Combine(brunoDir, "health");
        Directory.CreateDirectory(healthDir);

        // Health check request
        var healthCheckPath = Path.Combine(healthDir, "get-health.bru");
        var healthCheck = """
            meta {
              name: Health Check
              type: http
              seq: 1
            }

            get {
              url: {{baseUrl}}/healthz
              body: none
              auth: none
            }

            assert {
              res.status: eq 200
              res.body.status: eq healthy
            }

            tests {
              test("should return healthy status", function() {
                expect(res.status).to.equal(200);
                expect(res.body.status).to.equal("healthy");
              });
            }
            """;
        await File.WriteAllTextAsync(healthCheckPath, healthCheck, cancellationToken);
        files.Add("docs/api/bruno/health/get-health.bru");

        // Readiness check request
        var readinessCheckPath = Path.Combine(healthDir, "get-ready.bru");
        var readinessCheck = """
            meta {
              name: Readiness Check
              type: http
              seq: 2
            }

            get {
              url: {{baseUrl}}/ready
              body: none
              auth: none
            }

            assert {
              res.status: eq 200
              res.body.status: eq ready
            }

            tests {
              test("should return ready status", function() {
                expect(res.status).to.equal(200);
                expect(res.body.status).to.equal("ready");
              });
            }
            """;
        await File.WriteAllTextAsync(readinessCheckPath, readinessCheck, cancellationToken);
        files.Add("docs/api/bruno/health/get-ready.bru");

        // Create items requests directory
        var itemsDir = Path.Combine(brunoDir, "items");
        Directory.CreateDirectory(itemsDir);

        // List items request
        var listItemsPath = Path.Combine(itemsDir, "list-items.bru");
        var listItems = """
            meta {
              name: List Items
              type: http
              seq: 1
            }

            get {
              url: {{baseUrl}}/api/items
              body: none
              auth: none
            }

            query {
              limit: 10
              offset: 0
            }

            tests {
              test("should return items list", function() {
                expect(res.status).to.equal(200);
                expect(res.body).to.have.property("items");
                expect(res.body).to.have.property("total");
              });
            }
            """;
        await File.WriteAllTextAsync(listItemsPath, listItems, cancellationToken);
        files.Add("docs/api/bruno/items/list-items.bru");

        // Create item request
        var createItemPath = Path.Combine(itemsDir, "create-item.bru");
        var createItem = """
            meta {
              name: Create Item
              type: http
              seq: 2
            }

            post {
              url: {{baseUrl}}/api/items
              body: json
              auth: none
            }

            body:json {
              {
                "name": "New Item"
              }
            }

            tests {
              test("should create item", function() {
                expect(res.status).to.equal(201);
                expect(res.body).to.have.property("id");
                expect(res.body.name).to.equal("New Item");
              });
            }
            """;
        await File.WriteAllTextAsync(createItemPath, createItem, cancellationToken);
        files.Add("docs/api/bruno/items/create-item.bru");

        return files;
    }
}
