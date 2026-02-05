using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for ASP.NET Core Web API projects.
/// </summary>
public sealed class AspNetCoreWebApiGenerator : IProjectGenerator
{
    private readonly ILogger<AspNetCoreWebApiGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public AspNetCoreWebApiGenerator(
        ILogger<AspNetCoreWebApiGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "aspnetcore-webapi";
    public string TemplateName => "ASP.NET Core Web API";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.CSharp &&
               requirements.Framework == ProjectFramework.AspNetCoreWebApi;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating ASP.NET Core Web API project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName;

        // Create solution file
        var slnContent = GenerateSolutionFile(projectName);
        var slnPath = Path.Combine(outputPath, $"{projectName}.sln");
        await _filesystem.WriteFileAsync(slnPath, slnContent, cancellationToken);
        createdFiles.Add(slnPath);

        // Create src directory
        var srcPath = Path.Combine(outputPath, "src", projectName);
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        // Create project file
        var csprojContent = GenerateProjectFile(projectName, requirements);
        var csprojPath = Path.Combine(srcPath, $"{projectName}.csproj");
        await _filesystem.WriteFileAsync(csprojPath, csprojContent, cancellationToken);
        createdFiles.Add(csprojPath);

        // Create Program.cs
        var programContent = GenerateProgramCs(projectName);
        var programPath = Path.Combine(srcPath, "Program.cs");
        await _filesystem.WriteFileAsync(programPath, programContent, cancellationToken);
        createdFiles.Add(programPath);

        // Create appsettings.json
        var appSettingsContent = GenerateAppSettings();
        var appSettingsPath = Path.Combine(srcPath, "appsettings.json");
        await _filesystem.WriteFileAsync(appSettingsPath, appSettingsContent, cancellationToken);
        createdFiles.Add(appSettingsPath);

        // Create appsettings.Development.json
        var appSettingsDevContent = GenerateAppSettingsDevelopment();
        var appSettingsDevPath = Path.Combine(srcPath, "appsettings.Development.json");
        await _filesystem.WriteFileAsync(appSettingsDevPath, appSettingsDevContent, cancellationToken);
        createdFiles.Add(appSettingsDevPath);

        // Create Controllers directory and sample controller
        var controllersPath = Path.Combine(srcPath, "Controllers");
        await _filesystem.CreateDirectoryAsync(controllersPath, cancellationToken);

        var controllerContent = GenerateSampleController(projectName);
        var controllerPath = Path.Combine(controllersPath, "WeatherForecastController.cs");
        await _filesystem.WriteFileAsync(controllerPath, controllerContent, cancellationToken);
        createdFiles.Add(controllerPath);

        // Create WeatherForecast model
        var modelContent = GenerateWeatherForecastModel(projectName);
        var modelPath = Path.Combine(srcPath, "WeatherForecast.cs");
        await _filesystem.WriteFileAsync(modelPath, modelContent, cancellationToken);
        createdFiles.Add(modelPath);

        // Create test project if testing framework specified
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testFiles = await GenerateTestProjectAsync(requirements, outputPath, cancellationToken);
            createdFiles.AddRange(testFiles);
        }

        // Create .gitignore
        var gitignoreContent = GenerateGitignore();
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // Create README.md
        var readmeContent = GenerateReadme(projectName, requirements);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // Create .editorconfig
        var editorConfigContent = GenerateEditorConfig();
        var editorConfigPath = Path.Combine(outputPath, ".editorconfig");
        await _filesystem.WriteFileAsync(editorConfigPath, editorConfigContent, cancellationToken);
        createdFiles.Add(editorConfigPath);

        // Create Dockerfile if container support requested
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile(projectName);
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
        var files = new List<PlannedFile>
        {
            new() { RelativePath = $"{requirements.ProjectName}.sln", Description = "Solution file" },
            new() { RelativePath = $"src/{requirements.ProjectName}", IsDirectory = true, Description = "Main project directory" },
            new() { RelativePath = $"src/{requirements.ProjectName}/{requirements.ProjectName}.csproj", Description = "Project file" },
            new() { RelativePath = $"src/{requirements.ProjectName}/Program.cs", Description = "Application entry point" },
            new() { RelativePath = $"src/{requirements.ProjectName}/appsettings.json", Description = "Application settings" },
            new() { RelativePath = $"src/{requirements.ProjectName}/appsettings.Development.json", Description = "Development settings" },
            new() { RelativePath = $"src/{requirements.ProjectName}/Controllers/WeatherForecastController.cs", Description = "Sample API controller" },
            new() { RelativePath = $"src/{requirements.ProjectName}/WeatherForecast.cs", Description = "Sample model" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = ".editorconfig", Description = "Editor configuration" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = $"tests/{requirements.ProjectName}.Tests", IsDirectory = true, Description = "Test project directory" });
            files.Add(new PlannedFile { RelativePath = $"tests/{requirements.ProjectName}.Tests/{requirements.ProjectName}.Tests.csproj", Description = "Test project file" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = ".dockerignore", Description = "Docker ignore file" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private async Task<IReadOnlyList<string>> GenerateTestProjectAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var createdFiles = new List<string>();
        var projectName = requirements.ProjectName;
        var testProjectName = $"{projectName}.Tests";
        var testPath = Path.Combine(outputPath, "tests", testProjectName);

        await _filesystem.CreateDirectoryAsync(testPath, cancellationToken);

        var testCsprojContent = GenerateTestProjectFile(projectName, testProjectName);
        var testCsprojPath = Path.Combine(testPath, $"{testProjectName}.csproj");
        await _filesystem.WriteFileAsync(testCsprojPath, testCsprojContent, cancellationToken);
        createdFiles.Add(testCsprojPath);

        var testClassContent = GenerateSampleTest(projectName, testProjectName);
        var testClassPath = Path.Combine(testPath, "WeatherForecastControllerTests.cs");
        await _filesystem.WriteFileAsync(testClassPath, testClassContent, cancellationToken);
        createdFiles.Add(testClassPath);

        return createdFiles;
    }

    private static string GenerateSolutionFile(string projectName)
    {
        var projectGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var solutionGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();

        return $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""src\{projectName}\{projectName}.csproj"", ""{{{projectGuid}}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {{{solutionGuid}}}
	EndGlobalSection
EndGlobal
".TrimStart();
    }

    private static string GenerateProjectFile(string projectName, ProjectRequirements requirements)
    {
        var packagesSection = "";

        if (requirements.LintingTools.Contains("Roslyn analyzers"))
        {
            packagesSection = @"
  <ItemGroup>
    <PackageReference Include=""Microsoft.CodeAnalysis.NetAnalyzers"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>";
        }

        return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{projectName.Replace("-", "_")}</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""8.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>
{packagesSection}
</Project>
";
    }

    private static string GenerateProgramCs(string projectName)
    {
        return $@"var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program {{ }}
";
    }

    private static string GenerateAppSettings()
    {
        return @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}
";
    }

    private static string GenerateAppSettingsDevelopment()
    {
        return @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  }
}
";
    }

    private static string GenerateSampleController(string projectName)
    {
        var ns = projectName.Replace("-", "_");
        return $@"using Microsoft.AspNetCore.Mvc;

namespace {ns}.Controllers;

[ApiController]
[Route(""[controller]"")]
public class WeatherForecastController : ControllerBase
{{
    private static readonly string[] Summaries =
    [
        ""Freezing"", ""Bracing"", ""Chilly"", ""Cool"", ""Mild"",
        ""Warm"", ""Balmy"", ""Hot"", ""Sweltering"", ""Scorching""
    ];

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {{
        _logger = logger;
    }}

    [HttpGet(Name = ""GetWeatherForecast"")]
    public IEnumerable<WeatherForecast> Get()
    {{
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {{
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }})
        .ToArray();
    }}
}}
";
    }

    private static string GenerateWeatherForecastModel(string projectName)
    {
        var ns = projectName.Replace("-", "_");
        return $@"namespace {ns};

public class WeatherForecast
{{
    public DateOnly Date {{ get; set; }}
    public int TemperatureC {{ get; set; }}
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary {{ get; set; }}
}}
";
    }

    private static string GenerateTestProjectFile(string projectName, string testProjectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Mvc.Testing"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.10.0"" />
    <PackageReference Include=""xunit"" Version=""2.8.1"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.1"">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""coverlet.collector"" Version=""6.0.2"">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{projectName}\{projectName}.csproj"" />
  </ItemGroup>

</Project>
";
    }

    private static string GenerateSampleTest(string projectName, string testProjectName)
    {
        var ns = testProjectName.Replace("-", "_");
        var appNs = projectName.Replace("-", "_");
        return $@"using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace {ns};

public class WeatherForecastControllerTests : IClassFixture<WebApplicationFactory<Program>>
{{
    private readonly WebApplicationFactory<Program> _factory;

    public WeatherForecastControllerTests(WebApplicationFactory<Program> factory)
    {{
        _factory = factory;
    }}

    [Fact]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {{
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(""/WeatherForecast"");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }}

    [Fact]
    public async Task GetWeatherForecast_ReturnsJsonContent()
    {{
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(""/WeatherForecast"");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().NotBeNullOrEmpty();
        content.Should().StartWith(""["");
    }}
}}
";
    }

    private static string GenerateGitignore()
    {
        return @"## .NET
bin/
obj/
*.user
*.userosscache
*.sln.docstates
*.suo

## Visual Studio
.vs/
*.rsuser

## JetBrains Rider
.idea/
*.sln.iml

## User-specific files
*.user
*.suo

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

## NuGet
*.nupkg
*.snupkg
.nuget/
packages/
project.lock.json
project.fragment.lock.json
artifacts/

## Test Results
TestResults/
coverage/

## ASP.NET
*.publishsettings
PublishScripts/

## Secrets
appsettings.*.json
!appsettings.json
!appsettings.Development.json
*.secrets.json
.env
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "An ASP.NET Core Web API project."}

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Running the Application

```bash
cd src/{projectName}
dotnet run
```

The API will be available at `https://localhost:5001` and `http://localhost:5000`.

### API Documentation

When running in development mode, Swagger UI is available at `/swagger`.

### Running Tests

```bash
dotnet test
```

## Project Structure

```
{projectName}/
├── src/
│   └── {projectName}/
│       ├── Controllers/
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── {projectName}.Tests/
└── README.md
```

## License

This project is licensed under the MIT License.
";
    }

    private static string GenerateEditorConfig()
    {
        return @"# EditorConfig is awesome: https://EditorConfig.org

# top-most EditorConfig file
root = true

# Default settings for all files
[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# files
[*.cs]
indent_size = 4

# XML project files
[*.{csproj,vbproj,vcxproj,vcxproj.filters,proj,projitems,shproj}]
indent_size = 2

# XML config files
[*.{props,targets,ruleset,config,nuspec,resx,vsixmanifest,vsct}]
indent_size = 2

# JSON files
[*.json]
indent_size = 2

# YAML files
[*.{yml,yaml}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false
";
    }

    private static string GenerateDockerfile(string projectName)
    {
        return $@"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY [""src/{projectName}/{projectName}.csproj"", ""src/{projectName}/""]
RUN dotnet restore ""src/{projectName}/{projectName}.csproj""
COPY . .
WORKDIR ""/src/src/{projectName}""
RUN dotnet build ""{projectName}.csproj"" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish ""{projectName}.csproj"" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{projectName}.dll""]
";
    }

    private static string GenerateDockerignore()
    {
        return @"**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.vs
**/.vscode
**/bin
**/obj
**/Dockerfile*
**/docker-compose*
**/*.md
**/*.user
**/*.suo
**/TestResults
";
    }
}
