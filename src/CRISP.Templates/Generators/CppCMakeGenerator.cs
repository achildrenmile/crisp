using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for C++ projects with CMake.
/// </summary>
public sealed class CppCMakeGenerator : IProjectGenerator
{
    private readonly ILogger<CppCMakeGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public CppCMakeGenerator(
        ILogger<CppCMakeGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "cpp-cmake";
    public string TemplateName => "C++ with CMake";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.Cpp &&
               requirements.Framework == ProjectFramework.CppCMake;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating C++ CMake project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var projectNameUpper = requirements.ProjectName.ToUpperInvariant().Replace("-", "_");

        // Create directories
        var srcPath = Path.Combine(outputPath, "src");
        var includePath = Path.Combine(outputPath, "include", requirements.ProjectName);
        var testsPath = Path.Combine(outputPath, "tests");

        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(includePath, cancellationToken);
        await _filesystem.CreateDirectoryAsync(testsPath, cancellationToken);

        // CMakeLists.txt (root)
        var rootCMakeContent = GenerateRootCMakeLists(requirements.ProjectName, projectNameUpper, requirements);
        var rootCMakePath = Path.Combine(outputPath, "CMakeLists.txt");
        await _filesystem.WriteFileAsync(rootCMakePath, rootCMakeContent, cancellationToken);
        createdFiles.Add(rootCMakePath);

        // src/CMakeLists.txt
        var srcCMakeContent = GenerateSrcCMakeLists(requirements.ProjectName);
        var srcCMakePath = Path.Combine(srcPath, "CMakeLists.txt");
        await _filesystem.WriteFileAsync(srcCMakePath, srcCMakeContent, cancellationToken);
        createdFiles.Add(srcCMakePath);

        // src/main.cpp
        var mainContent = GenerateMain(requirements.ProjectName);
        var mainPath = Path.Combine(srcPath, "main.cpp");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // src/app.cpp
        var appCppContent = GenerateAppCpp(requirements.ProjectName);
        var appCppPath = Path.Combine(srcPath, "app.cpp");
        await _filesystem.WriteFileAsync(appCppPath, appCppContent, cancellationToken);
        createdFiles.Add(appCppPath);

        // include/project/app.hpp
        var appHppContent = GenerateAppHpp(requirements.ProjectName);
        var appHppPath = Path.Combine(includePath, "app.hpp");
        await _filesystem.WriteFileAsync(appHppPath, appHppContent, cancellationToken);
        createdFiles.Add(appHppPath);

        // include/project/version.hpp.in
        var versionHppContent = GenerateVersionHpp(requirements.ProjectName, projectNameUpper);
        var versionHppPath = Path.Combine(includePath, "version.hpp.in");
        await _filesystem.WriteFileAsync(versionHppPath, versionHppContent, cancellationToken);
        createdFiles.Add(versionHppPath);

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testsCMakeContent = GenerateTestsCMakeLists(requirements.ProjectName);
            var testsCMakePath = Path.Combine(testsPath, "CMakeLists.txt");
            await _filesystem.WriteFileAsync(testsCMakePath, testsCMakeContent, cancellationToken);
            createdFiles.Add(testsCMakePath);

            var testMainContent = GenerateTestMain(requirements.ProjectName);
            var testMainPath = Path.Combine(testsPath, "test_main.cpp");
            await _filesystem.WriteFileAsync(testMainPath, testMainContent, cancellationToken);
            createdFiles.Add(testMainPath);

            var testAppContent = GenerateTestApp(requirements.ProjectName);
            var testAppPath = Path.Combine(testsPath, "test_app.cpp");
            await _filesystem.WriteFileAsync(testAppPath, testAppContent, cancellationToken);
            createdFiles.Add(testAppPath);
        }

        // .clang-format
        var clangFormatContent = GenerateClangFormat();
        var clangFormatPath = Path.Combine(outputPath, ".clang-format");
        await _filesystem.WriteFileAsync(clangFormatPath, clangFormatContent, cancellationToken);
        createdFiles.Add(clangFormatPath);

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

        // CMakePresets.json
        var presetsContent = GenerateCMakePresets(requirements.ProjectName);
        var presetsPath = Path.Combine(outputPath, "CMakePresets.json");
        await _filesystem.WriteFileAsync(presetsPath, presetsContent, cancellationToken);
        createdFiles.Add(presetsPath);

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
            new() { RelativePath = "CMakeLists.txt", Description = "Root CMake configuration" },
            new() { RelativePath = "CMakePresets.json", Description = "CMake presets" },
            new() { RelativePath = "src", IsDirectory = true, Description = "Source files" },
            new() { RelativePath = "src/CMakeLists.txt", Description = "Source CMake config" },
            new() { RelativePath = "src/main.cpp", Description = "Application entry point" },
            new() { RelativePath = "src/app.cpp", Description = "Application implementation" },
            new() { RelativePath = "include", IsDirectory = true, Description = "Header files" },
            new() { RelativePath = "include/.../app.hpp", Description = "Application header" },
            new() { RelativePath = "include/.../version.hpp.in", Description = "Version header template" },
            new() { RelativePath = ".clang-format", Description = "Clang format config" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "tests/CMakeLists.txt", Description = "Tests CMake config" });
            files.Add(new PlannedFile { RelativePath = "tests/test_main.cpp", Description = "Test main" });
            files.Add(new PlannedFile { RelativePath = "tests/test_app.cpp", Description = "Application tests" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateRootCMakeLists(string projectName, string projectNameUpper, ProjectRequirements requirements)
    {
        var testSection = !string.IsNullOrEmpty(requirements.TestingFramework) ? @"
# Testing
option(BUILD_TESTING ""Build tests"" ON)
if(BUILD_TESTING)
    enable_testing()
    add_subdirectory(tests)
endif()
" : "";

        return $@"cmake_minimum_required(VERSION 3.20)

project({projectName}
    VERSION 0.1.0
    DESCRIPTION ""{requirements.Description ?? "A C++ application"}""
    LANGUAGES CXX
)

# C++ Standard
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Export compile commands for IDE support
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Build type
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release CACHE STRING ""Build type"" FORCE)
endif()

# Options
option({projectNameUpper}_BUILD_EXAMPLES ""Build examples"" OFF)

# Include directories
include_directories(${{CMAKE_SOURCE_DIR}}/include)
include_directories(${{CMAKE_BINARY_DIR}}/include)

# Configure version header
configure_file(
    ${{CMAKE_SOURCE_DIR}}/include/{projectName}/version.hpp.in
    ${{CMAKE_BINARY_DIR}}/include/{projectName}/version.hpp
)

# Source
add_subdirectory(src)
{testSection}
# Install
include(GNUInstallDirs)
install(TARGETS {projectName}
    RUNTIME DESTINATION ${{CMAKE_INSTALL_BINDIR}}
)
";
    }

    private static string GenerateSrcCMakeLists(string projectName)
    {
        return $@"# Source files
set(SOURCES
    main.cpp
    app.cpp
)

# Executable
add_executable({projectName} ${{SOURCES}})

# Include directories
target_include_directories({projectName} PRIVATE
    ${{CMAKE_SOURCE_DIR}}/include
    ${{CMAKE_BINARY_DIR}}/include
)

# Compiler warnings
if(MSVC)
    target_compile_options({projectName} PRIVATE /W4 /WX)
else()
    target_compile_options({projectName} PRIVATE -Wall -Wextra -Wpedantic -Werror)
endif()
";
    }

    private static string GenerateMain(string projectName)
    {
        return $@"#include <iostream>
#include <{projectName}/app.hpp>
#include <{projectName}/version.hpp>

int main(int argc, char* argv[]) {{
    std::cout << ""{projectName} v"" << {projectName}::VERSION << std::endl;

    {projectName}::App app;

    if (argc > 1) {{
        std::string command(argv[1]);
        if (command == ""--version"" || command == ""-v"") {{
            return 0;
        }}
        if (command == ""--help"" || command == ""-h"") {{
            std::cout << ""Usage: {projectName} [OPTIONS]"" << std::endl;
            std::cout << ""Options:"" << std::endl;
            std::cout << ""  -h, --help     Show this help message"" << std::endl;
            std::cout << ""  -v, --version  Show version"" << std::endl;
            return 0;
        }}
    }}

    return app.run();
}}
";
    }

    private static string GenerateAppCpp(string projectName)
    {
        return $@"#include <{projectName}/app.hpp>
#include <iostream>

namespace {projectName} {{

App::App() = default;
App::~App() = default;

int App::run() {{
    std::cout << ""Application running..."" << std::endl;

    // Example: demonstrate some functionality
    auto result = add(2, 3);
    std::cout << ""2 + 3 = "" << result << std::endl;

    auto greeting = greet(""World"");
    std::cout << greeting << std::endl;

    return 0;
}}

int App::add(int a, int b) const {{
    return a + b;
}}

std::string App::greet(const std::string& name) const {{
    return ""Hello, "" + name + ""!"";
}}

}} // namespace {projectName}
";
    }

    private static string GenerateAppHpp(string projectName)
    {
        var guardName = projectName.ToUpperInvariant().Replace("-", "_") + "_APP_HPP";
        return $@"#ifndef {guardName}
#define {guardName}

#include <string>

namespace {projectName} {{

class App {{
public:
    App();
    ~App();

    // Disable copy
    App(const App&) = delete;
    App& operator=(const App&) = delete;

    // Enable move
    App(App&&) = default;
    App& operator=(App&&) = default;

    /// Run the application
    int run();

    /// Add two numbers
    int add(int a, int b) const;

    /// Generate a greeting
    std::string greet(const std::string& name) const;
}};

}} // namespace {projectName}

#endif // {guardName}
";
    }

    private static string GenerateVersionHpp(string projectName, string projectNameUpper)
    {
        var guardName = projectNameUpper + "_VERSION_HPP";
        return $@"#ifndef {guardName}
#define {guardName}

namespace {projectName} {{

constexpr const char* VERSION = ""@PROJECT_VERSION@"";
constexpr int VERSION_MAJOR = @PROJECT_VERSION_MAJOR@;
constexpr int VERSION_MINOR = @PROJECT_VERSION_MINOR@;
constexpr int VERSION_PATCH = @PROJECT_VERSION_PATCH@;

}} // namespace {projectName}

#endif // {guardName}
";
    }

    private static string GenerateTestsCMakeLists(string projectName)
    {
        return $@"# Fetch GoogleTest
include(FetchContent)
FetchContent_Declare(
    googletest
    URL https://github.com/google/googletest/archive/refs/tags/v1.14.0.zip
)
set(gtest_force_shared_crt ON CACHE BOOL """" FORCE)
FetchContent_MakeAvailable(googletest)

# Test executable
add_executable({projectName}_tests
    test_main.cpp
    test_app.cpp
    ${{CMAKE_SOURCE_DIR}}/src/app.cpp
)

target_include_directories({projectName}_tests PRIVATE
    ${{CMAKE_SOURCE_DIR}}/include
    ${{CMAKE_BINARY_DIR}}/include
)

target_link_libraries({projectName}_tests
    GTest::gtest_main
)

include(GoogleTest)
gtest_discover_tests({projectName}_tests)
";
    }

    private static string GenerateTestMain(string projectName)
    {
        return @"#include <gtest/gtest.h>

int main(int argc, char** argv) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
";
    }

    private static string GenerateTestApp(string projectName)
    {
        return $@"#include <gtest/gtest.h>
#include <{projectName}/app.hpp>

namespace {projectName} {{

class AppTest : public ::testing::Test {{
protected:
    App app;
}};

TEST_F(AppTest, AddReturnsCorrectSum) {{
    EXPECT_EQ(app.add(2, 3), 5);
    EXPECT_EQ(app.add(-1, 1), 0);
    EXPECT_EQ(app.add(0, 0), 0);
}}

TEST_F(AppTest, GreetReturnsCorrectMessage) {{
    EXPECT_EQ(app.greet(""World""), ""Hello, World!"");
    EXPECT_EQ(app.greet(""User""), ""Hello, User!"");
}}

}} // namespace {projectName}
";
    }

    private static string GenerateClangFormat()
    {
        return @"---
Language: Cpp
BasedOnStyle: Google
IndentWidth: 4
ColumnLimit: 100
AllowShortFunctionsOnASingleLine: Empty
AllowShortIfStatementsOnASingleLine: Never
AllowShortLoopsOnASingleLine: false
BreakBeforeBraces: Attach
NamespaceIndentation: None
PointerAlignment: Left
SpaceAfterCStyleCast: false
...
";
    }

    private static string GenerateGitignore()
    {
        return @"# Build directories
build/
out/
cmake-build-*/

# IDE
.idea/
.vscode/
*.swp
.vs/
*.suo
*.user

# Compiled
*.o
*.obj
*.exe
*.out
*.app
*.dll
*.so
*.dylib

# CMake
CMakeFiles/
CMakeCache.txt
cmake_install.cmake
compile_commands.json

# OS
.DS_Store
Thumbs.db
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A modern C++ application using CMake."}

## Prerequisites

- CMake 3.20+
- C++20 compatible compiler (GCC 11+, Clang 14+, MSVC 2022+)

## Building

### Using CMake Presets (Recommended)

```bash
# Configure
cmake --preset=release

# Build
cmake --build --preset=release

# Test
ctest --preset=release
```

### Manual Build

```bash
mkdir build && cd build
cmake -DCMAKE_BUILD_TYPE=Release ..
cmake --build .
```

## Running

```bash
./build/src/{projectName}
```

### Command Line Options

```
Usage: {projectName} [OPTIONS]
Options:
  -h, --help     Show help message
  -v, --version  Show version
```

## Testing

```bash
cd build
ctest --output-on-failure
```

## Project Structure

```
{projectName}/
├── include/{projectName}/
│   ├── app.hpp
│   └── version.hpp.in
├── src/
│   ├── CMakeLists.txt
│   ├── main.cpp
│   └── app.cpp
├── tests/
│   ├── CMakeLists.txt
│   ├── test_main.cpp
│   └── test_app.cpp
├── CMakeLists.txt
├── CMakePresets.json
├── .clang-format
└── README.md
```

## Features

- Modern C++20
- CMake presets for easy configuration
- GoogleTest integration
- Clang-format configuration
- Version information from CMake

## License

MIT
";
    }

    private static string GenerateCMakePresets(string projectName)
    {
        return $@"{{
    ""version"": 6,
    ""cmakeMinimumRequired"": {{
        ""major"": 3,
        ""minor"": 20,
        ""patch"": 0
    }},
    ""configurePresets"": [
        {{
            ""name"": ""base"",
            ""hidden"": true,
            ""binaryDir"": ""${{sourceDir}}/build/${{presetName}}"",
            ""installDir"": ""${{sourceDir}}/install/${{presetName}}""
        }},
        {{
            ""name"": ""debug"",
            ""inherits"": ""base"",
            ""displayName"": ""Debug"",
            ""cacheVariables"": {{
                ""CMAKE_BUILD_TYPE"": ""Debug""
            }}
        }},
        {{
            ""name"": ""release"",
            ""inherits"": ""base"",
            ""displayName"": ""Release"",
            ""cacheVariables"": {{
                ""CMAKE_BUILD_TYPE"": ""Release""
            }}
        }}
    ],
    ""buildPresets"": [
        {{
            ""name"": ""debug"",
            ""configurePreset"": ""debug""
        }},
        {{
            ""name"": ""release"",
            ""configurePreset"": ""release""
        }}
    ],
    ""testPresets"": [
        {{
            ""name"": ""debug"",
            ""configurePreset"": ""debug"",
            ""output"": {{
                ""outputOnFailure"": true
            }}
        }},
        {{
            ""name"": ""release"",
            ""configurePreset"": ""release"",
            ""output"": {{
                ""outputOnFailure"": true
            }}
        }}
    ]
}}
";
    }

    private static string GenerateDockerfile(string projectName)
    {
        return $@"FROM gcc:13 AS builder

RUN apt-get update && apt-get install -y cmake

WORKDIR /app
COPY . .

RUN cmake -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF
RUN cmake --build build

FROM debian:bookworm-slim

WORKDIR /app
COPY --from=builder /app/build/src/{projectName} .

RUN useradd -m appuser
USER appuser

ENTRYPOINT [""./{projectName}""]
";
    }

    private static string GenerateDockerCompose(string projectName)
    {
        return $@"version: '3.8'

services:
  app:
    build: .
    container_name: {projectName}
";
    }
}
