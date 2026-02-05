using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for React projects with Vite.
/// </summary>
public sealed class ReactGenerator : IProjectGenerator
{
    private readonly ILogger<ReactGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public ReactGenerator(
        ILogger<ReactGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "react-vite";
    public string TemplateName => "React with Vite";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return (requirements.Language == ProjectLanguage.JavaScript ||
                requirements.Language == ProjectLanguage.TypeScript) &&
               requirements.Framework == ProjectFramework.React;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating React project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "tsx" : "jsx";
        var extJs = isTypeScript ? "ts" : "js";

        // Create directories
        var srcPath = Path.Combine(outputPath, "src");
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        var componentsPath = Path.Combine(srcPath, "components");
        await _filesystem.CreateDirectoryAsync(componentsPath, cancellationToken);

        var hooksPath = Path.Combine(srcPath, "hooks");
        await _filesystem.CreateDirectoryAsync(hooksPath, cancellationToken);

        var publicPath = Path.Combine(outputPath, "public");
        await _filesystem.CreateDirectoryAsync(publicPath, cancellationToken);

        // index.html
        var indexHtmlContent = GenerateIndexHtml(requirements.ProjectName);
        var indexHtmlPath = Path.Combine(outputPath, "index.html");
        await _filesystem.WriteFileAsync(indexHtmlPath, indexHtmlContent, cancellationToken);
        createdFiles.Add(indexHtmlPath);

        // vite.config
        var viteConfigContent = GenerateViteConfig(isTypeScript);
        var viteConfigPath = Path.Combine(outputPath, $"vite.config.{extJs}");
        await _filesystem.WriteFileAsync(viteConfigPath, viteConfigContent, cancellationToken);
        createdFiles.Add(viteConfigPath);

        // main entry
        var mainContent = GenerateMain(isTypeScript);
        var mainPath = Path.Combine(srcPath, $"main.{ext}");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // App component
        var appContent = GenerateApp(requirements.ProjectName, isTypeScript);
        var appPath = Path.Combine(srcPath, $"App.{ext}");
        await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
        createdFiles.Add(appPath);

        // App.css
        var appCssContent = GenerateAppCss();
        var appCssPath = Path.Combine(srcPath, "App.css");
        await _filesystem.WriteFileAsync(appCssPath, appCssContent, cancellationToken);
        createdFiles.Add(appCssPath);

        // index.css
        var indexCssContent = GenerateIndexCss();
        var indexCssPath = Path.Combine(srcPath, "index.css");
        await _filesystem.WriteFileAsync(indexCssPath, indexCssContent, cancellationToken);
        createdFiles.Add(indexCssPath);

        // Sample component
        var buttonContent = GenerateButton(isTypeScript);
        var buttonPath = Path.Combine(componentsPath, $"Button.{ext}");
        await _filesystem.WriteFileAsync(buttonPath, buttonContent, cancellationToken);
        createdFiles.Add(buttonPath);

        // Custom hook
        var hookContent = GenerateUseCounter(isTypeScript);
        var hookPath = Path.Combine(hooksPath, $"useCounter.{extJs}");
        await _filesystem.WriteFileAsync(hookPath, hookContent, cancellationToken);
        createdFiles.Add(hookPath);

        // package.json
        var packageContent = GeneratePackageJson(requirements.ProjectName, isTypeScript, requirements);
        var packagePath = Path.Combine(outputPath, "package.json");
        await _filesystem.WriteFileAsync(packagePath, packageContent, cancellationToken);
        createdFiles.Add(packagePath);

        // TypeScript config
        if (isTypeScript)
        {
            var tsconfigContent = GenerateTsConfig();
            var tsconfigPath = Path.Combine(outputPath, "tsconfig.json");
            await _filesystem.WriteFileAsync(tsconfigPath, tsconfigContent, cancellationToken);
            createdFiles.Add(tsconfigPath);

            var tsconfigNodeContent = GenerateTsConfigNode();
            var tsconfigNodePath = Path.Combine(outputPath, "tsconfig.node.json");
            await _filesystem.WriteFileAsync(tsconfigNodePath, tsconfigNodeContent, cancellationToken);
            createdFiles.Add(tsconfigNodePath);

            var viteEnvContent = GenerateViteEnv();
            var viteEnvPath = Path.Combine(srcPath, "vite-env.d.ts");
            await _filesystem.WriteFileAsync(viteEnvPath, viteEnvContent, cancellationToken);
            createdFiles.Add(viteEnvPath);
        }

        // .gitignore
        var gitignoreContent = GenerateGitignore();
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // README.md
        var readmeContent = GenerateReadme(requirements.ProjectName, isTypeScript, requirements);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // ESLint config
        var eslintContent = GenerateEslintConfig(isTypeScript);
        var eslintPath = Path.Combine(outputPath, "eslint.config.js");
        await _filesystem.WriteFileAsync(eslintPath, eslintContent, cancellationToken);
        createdFiles.Add(eslintPath);

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testContent = GenerateAppTest(isTypeScript);
            var testPath = Path.Combine(srcPath, $"App.test.{ext}");
            await _filesystem.WriteFileAsync(testPath, testContent, cancellationToken);
            createdFiles.Add(testPath);

            var setupTestContent = GenerateSetupTests(isTypeScript);
            var setupTestPath = Path.Combine(srcPath, $"setupTests.{extJs}");
            await _filesystem.WriteFileAsync(setupTestPath, setupTestContent, cancellationToken);
            createdFiles.Add(setupTestPath);
        }

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile();
            var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
            await _filesystem.WriteFileAsync(dockerfilePath, dockerfileContent, cancellationToken);
            createdFiles.Add(dockerfilePath);

            var nginxContent = GenerateNginxConfig();
            var nginxPath = Path.Combine(outputPath, "nginx.conf");
            await _filesystem.WriteFileAsync(nginxPath, nginxContent, cancellationToken);
            createdFiles.Add(nginxPath);
        }

        _logger.LogInformation("Generated {Count} files", createdFiles.Count);
        return createdFiles;
    }

    public Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "tsx" : "jsx";
        var extJs = isTypeScript ? "ts" : "js";

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "index.html", Description = "HTML entry point" },
            new() { RelativePath = $"vite.config.{extJs}", Description = "Vite configuration" },
            new() { RelativePath = "src", IsDirectory = true, Description = "Source code" },
            new() { RelativePath = $"src/main.{ext}", Description = "Application entry point" },
            new() { RelativePath = $"src/App.{ext}", Description = "Root component" },
            new() { RelativePath = "src/App.css", Description = "App styles" },
            new() { RelativePath = "src/index.css", Description = "Global styles" },
            new() { RelativePath = "src/components", IsDirectory = true, Description = "React components" },
            new() { RelativePath = $"src/components/Button.{ext}", Description = "Button component" },
            new() { RelativePath = "src/hooks", IsDirectory = true, Description = "Custom hooks" },
            new() { RelativePath = $"src/hooks/useCounter.{extJs}", Description = "Counter hook" },
            new() { RelativePath = "public", IsDirectory = true, Description = "Static assets" },
            new() { RelativePath = "package.json", Description = "Package manifest" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = "eslint.config.js", Description = "ESLint configuration" }
        };

        if (isTypeScript)
        {
            files.Add(new PlannedFile { RelativePath = "tsconfig.json", Description = "TypeScript configuration" });
            files.Add(new PlannedFile { RelativePath = "tsconfig.node.json", Description = "TypeScript Node configuration" });
            files.Add(new PlannedFile { RelativePath = "src/vite-env.d.ts", Description = "Vite type definitions" });
        }

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = $"src/App.test.{ext}", Description = "App tests" });
            files.Add(new PlannedFile { RelativePath = $"src/setupTests.{extJs}", Description = "Test setup" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "nginx.conf", Description = "Nginx configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateIndexHtml(string projectName)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
  <head>
    <meta charset=""UTF-8"" />
    <link rel=""icon"" type=""image/svg+xml"" href=""/vite.svg"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{projectName}</title>
  </head>
  <body>
    <div id=""root""></div>
    <script type=""module"" src=""/src/main.tsx""></script>
  </body>
</html>
";
    }

    private static string GenerateViteConfig(bool isTypeScript)
    {
        return @"import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
  },
  build: {
    outDir: 'dist',
  },
})
";
    }

    private static string GenerateMain(bool isTypeScript)
    {
        return @"import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
";
    }

    private static string GenerateApp(string projectName, bool isTypeScript)
    {
        var propsType = isTypeScript ? ": React.FC" : "";
        return $@"import {{ useState }} from 'react'
import './App.css'
import Button from './components/Button'
import {{ useCounter }} from './hooks/useCounter'

function App(){propsType} {{
  const {{ count, increment, decrement, reset }} = useCounter(0)

  return (
    <div className=""app"">
      <header className=""app-header"">
        <h1>{projectName}</h1>
        <p>Welcome to your new React application!</p>
      </header>

      <main className=""app-main"">
        <div className=""counter"">
          <h2>Counter: {{count}}</h2>
          <div className=""counter-buttons"">
            <Button onClick={{decrement}}>-</Button>
            <Button onClick={{reset}} variant=""secondary"">Reset</Button>
            <Button onClick={{increment}}>+</Button>
          </div>
        </div>
      </main>
    </div>
  )
}}

export default App
";
    }

    private static string GenerateAppCss()
    {
        return @".app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 2rem;
  text-align: center;
  color: white;
}

.app-header h1 {
  margin: 0 0 0.5rem 0;
  font-size: 2.5rem;
}

.app-header p {
  margin: 0;
  opacity: 0.9;
}

.app-main {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 2rem;
}

.counter {
  text-align: center;
  padding: 2rem;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.counter h2 {
  margin: 0 0 1.5rem 0;
  font-size: 2rem;
  color: #333;
}

.counter-buttons {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}
";
    }

    private static string GenerateIndexCss()
    {
        return @":root {
  font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
  line-height: 1.5;
  font-weight: 400;

  color-scheme: light dark;
  color: #213547;
  background-color: #f5f5f5;

  font-synthesis: none;
  text-rendering: optimizeLegibility;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  margin: 0;
  min-width: 320px;
  min-height: 100vh;
}
";
    }

    private static string GenerateButton(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"import React from 'react';

interface ButtonProps {
  children: React.ReactNode;
  onClick?: () => void;
  variant?: 'primary' | 'secondary';
  disabled?: boolean;
}

const Button: React.FC<ButtonProps> = ({
  children,
  onClick,
  variant = 'primary',
  disabled = false,
}) => {
  const baseStyles: React.CSSProperties = {
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: 600,
    border: 'none',
    borderRadius: '0.5rem',
    cursor: disabled ? 'not-allowed' : 'pointer',
    transition: 'all 0.2s ease',
    opacity: disabled ? 0.5 : 1,
  };

  const variantStyles: Record<string, React.CSSProperties> = {
    primary: {
      backgroundColor: '#667eea',
      color: 'white',
    },
    secondary: {
      backgroundColor: '#e2e8f0',
      color: '#4a5568',
    },
  };

  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{ ...baseStyles, ...variantStyles[variant] }}
    >
      {children}
    </button>
  );
};

export default Button;
";
        }

        return @"import React from 'react';

const Button = ({
  children,
  onClick,
  variant = 'primary',
  disabled = false,
}) => {
  const baseStyles = {
    padding: '0.75rem 1.5rem',
    fontSize: '1rem',
    fontWeight: 600,
    border: 'none',
    borderRadius: '0.5rem',
    cursor: disabled ? 'not-allowed' : 'pointer',
    transition: 'all 0.2s ease',
    opacity: disabled ? 0.5 : 1,
  };

  const variantStyles = {
    primary: {
      backgroundColor: '#667eea',
      color: 'white',
    },
    secondary: {
      backgroundColor: '#e2e8f0',
      color: '#4a5568',
    },
  };

  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{ ...baseStyles, ...variantStyles[variant] }}
    >
      {children}
    </button>
  );
};

export default Button;
";
    }

    private static string GenerateUseCounter(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"import { useState, useCallback } from 'react';

interface UseCounterReturn {
  count: number;
  increment: () => void;
  decrement: () => void;
  reset: () => void;
  setCount: (value: number) => void;
}

export const useCounter = (initialValue: number = 0): UseCounterReturn => {
  const [count, setCount] = useState(initialValue);

  const increment = useCallback(() => setCount((c) => c + 1), []);
  const decrement = useCallback(() => setCount((c) => c - 1), []);
  const reset = useCallback(() => setCount(initialValue), [initialValue]);

  return { count, increment, decrement, reset, setCount };
};
";
        }

        return @"import { useState, useCallback } from 'react';

export const useCounter = (initialValue = 0) => {
  const [count, setCount] = useState(initialValue);

  const increment = useCallback(() => setCount((c) => c + 1), []);
  const decrement = useCallback(() => setCount((c) => c - 1), []);
  const reset = useCallback(() => setCount(initialValue), [initialValue]);

  return { count, increment, decrement, reset, setCount };
};
";
    }

    private static string GeneratePackageJson(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        var devDeps = new List<string>
        {
            "\"@vitejs/plugin-react\": \"^4.2.0\"",
            "\"vite\": \"^5.0.0\"",
            "\"eslint\": \"^8.55.0\"",
            "\"eslint-plugin-react\": \"^7.33.0\"",
            "\"eslint-plugin-react-hooks\": \"^4.6.0\""
        };

        if (isTypeScript)
        {
            devDeps.Add("\"typescript\": \"^5.3.0\"");
            devDeps.Add("\"@types/react\": \"^18.2.0\"");
            devDeps.Add("\"@types/react-dom\": \"^18.2.0\"");
            devDeps.Add("\"@typescript-eslint/eslint-plugin\": \"^6.0.0\"");
            devDeps.Add("\"@typescript-eslint/parser\": \"^6.0.0\"");
        }

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            devDeps.Add("\"vitest\": \"^1.0.0\"");
            devDeps.Add("\"@testing-library/react\": \"^14.0.0\"");
            devDeps.Add("\"@testing-library/jest-dom\": \"^6.0.0\"");
            devDeps.Add("\"jsdom\": \"^23.0.0\"");
        }

        var scripts = new List<string>
        {
            "\"dev\": \"vite\"",
            "\"build\": \"vite build\"",
            "\"preview\": \"vite preview\"",
            "\"lint\": \"eslint src\""
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            scripts.Add("\"test\": \"vitest\"");
        }

        return $@"{{
  ""name"": ""{projectName}"",
  ""private"": true,
  ""version"": ""0.1.0"",
  ""type"": ""module"",
  ""scripts"": {{
    {string.Join(",\n    ", scripts)}
  }},
  ""dependencies"": {{
    ""react"": ""^18.2.0"",
    ""react-dom"": ""^18.2.0""
  }},
  ""devDependencies"": {{
    {string.Join(",\n    ", devDeps)}
  }}
}}
";
    }

    private static string GenerateTsConfig()
    {
        return @"{
  ""compilerOptions"": {
    ""target"": ""ES2020"",
    ""useDefineForClassFields"": true,
    ""lib"": [""ES2020"", ""DOM"", ""DOM.Iterable""],
    ""module"": ""ESNext"",
    ""skipLibCheck"": true,
    ""moduleResolution"": ""bundler"",
    ""allowImportingTsExtensions"": true,
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""noEmit"": true,
    ""jsx"": ""react-jsx"",
    ""strict"": true,
    ""noUnusedLocals"": true,
    ""noUnusedParameters"": true,
    ""noFallthroughCasesInSwitch"": true
  },
  ""include"": [""src""],
  ""references"": [{ ""path"": ""./tsconfig.node.json"" }]
}
";
    }

    private static string GenerateTsConfigNode()
    {
        return @"{
  ""compilerOptions"": {
    ""composite"": true,
    ""skipLibCheck"": true,
    ""module"": ""ESNext"",
    ""moduleResolution"": ""bundler"",
    ""allowSyntheticDefaultImports"": true,
    ""strict"": true
  },
  ""include"": [""vite.config.ts""]
}
";
    }

    private static string GenerateViteEnv()
    {
        return @"/// <reference types=""vite/client"" />
";
    }

    private static string GenerateGitignore()
    {
        return @"# Dependencies
node_modules/

# Build
dist/

# Environment
.env
.env.local
.env.*.local

# Logs
*.log
npm-debug.log*

# IDE
.idea/
.vscode/
*.swp

# OS
.DS_Store

# Test coverage
coverage/
";
    }

    private static string GenerateReadme(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        var lang = isTypeScript ? "TypeScript" : "JavaScript";
        return $@"# {projectName}

{requirements.Description ?? $"A React application built with Vite and {lang}."}

## Getting Started

### Prerequisites

- Node.js 20+
- npm or yarn

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

### Testing

```bash
npm test
```

## Project Structure

```
{projectName}/
├── public/
├── src/
│   ├── components/
│   ├── hooks/
│   ├── App.{(isTypeScript ? "tsx" : "jsx")}
│   └── main.{(isTypeScript ? "tsx" : "jsx")}
├── index.html
├── package.json
{(isTypeScript ? "├── tsconfig.json\n" : "")}└── vite.config.{(isTypeScript ? "ts" : "js")}
```

## License

MIT
";
    }

    private static string GenerateEslintConfig(bool isTypeScript)
    {
        return @"import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'

export default [
  { ignores: ['dist'] },
  {
    files: ['**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
    },
  },
]
";
    }

    private static string GenerateAppTest(bool isTypeScript)
    {
        return @"import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  it('renders the app title', () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
  })

  it('renders the counter', () => {
    render(<App />)
    expect(screen.getByText(/Counter:/)).toBeInTheDocument()
  })
})
";
    }

    private static string GenerateSetupTests(bool isTypeScript)
    {
        return @"import '@testing-library/jest-dom/vitest'
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM node:20-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine

COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD [""nginx"", ""-g"", ""daemon off;""]
";
    }

    private static string GenerateNginxConfig()
    {
        return @"server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control ""public, immutable"";
    }
}
";
    }
}
