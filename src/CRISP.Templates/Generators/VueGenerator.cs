using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Vue.js projects with Vite.
/// </summary>
public sealed class VueGenerator : IProjectGenerator
{
    private readonly ILogger<VueGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public VueGenerator(
        ILogger<VueGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "vue-vite";
    public string TemplateName => "Vue.js with Vite";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return (requirements.Language == ProjectLanguage.JavaScript ||
                requirements.Language == ProjectLanguage.TypeScript) &&
               requirements.Framework == ProjectFramework.Vue;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Vue.js project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "ts" : "js";

        // Create directories
        var srcPath = Path.Combine(outputPath, "src");
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        var componentsPath = Path.Combine(srcPath, "components");
        await _filesystem.CreateDirectoryAsync(componentsPath, cancellationToken);

        var viewsPath = Path.Combine(srcPath, "views");
        await _filesystem.CreateDirectoryAsync(viewsPath, cancellationToken);

        var composablesPath = Path.Combine(srcPath, "composables");
        await _filesystem.CreateDirectoryAsync(composablesPath, cancellationToken);

        var publicPath = Path.Combine(outputPath, "public");
        await _filesystem.CreateDirectoryAsync(publicPath, cancellationToken);

        // index.html
        var indexHtmlContent = GenerateIndexHtml(requirements.ProjectName);
        var indexHtmlPath = Path.Combine(outputPath, "index.html");
        await _filesystem.WriteFileAsync(indexHtmlPath, indexHtmlContent, cancellationToken);
        createdFiles.Add(indexHtmlPath);

        // vite.config
        var viteConfigContent = GenerateViteConfig(isTypeScript);
        var viteConfigPath = Path.Combine(outputPath, $"vite.config.{ext}");
        await _filesystem.WriteFileAsync(viteConfigPath, viteConfigContent, cancellationToken);
        createdFiles.Add(viteConfigPath);

        // main entry
        var mainContent = GenerateMain(isTypeScript);
        var mainPath = Path.Combine(srcPath, $"main.{ext}");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // App.vue
        var appContent = GenerateApp(requirements.ProjectName);
        var appPath = Path.Combine(srcPath, "App.vue");
        await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
        createdFiles.Add(appPath);

        // HelloWorld component
        var helloWorldContent = GenerateHelloWorld(isTypeScript);
        var helloWorldPath = Path.Combine(componentsPath, "HelloWorld.vue");
        await _filesystem.WriteFileAsync(helloWorldPath, helloWorldContent, cancellationToken);
        createdFiles.Add(helloWorldPath);

        // HomeView
        var homeViewContent = GenerateHomeView(isTypeScript);
        var homeViewPath = Path.Combine(viewsPath, "HomeView.vue");
        await _filesystem.WriteFileAsync(homeViewPath, homeViewContent, cancellationToken);
        createdFiles.Add(homeViewPath);

        // useCounter composable
        var useCounterContent = GenerateUseCounter(isTypeScript);
        var useCounterPath = Path.Combine(composablesPath, $"useCounter.{ext}");
        await _filesystem.WriteFileAsync(useCounterPath, useCounterContent, cancellationToken);
        createdFiles.Add(useCounterPath);

        // style.css
        var styleCssContent = GenerateStyleCss();
        var styleCssPath = Path.Combine(srcPath, "style.css");
        await _filesystem.WriteFileAsync(styleCssPath, styleCssContent, cancellationToken);
        createdFiles.Add(styleCssPath);

        // package.json
        var packageJsonContent = GeneratePackageJson(requirements.ProjectName, isTypeScript);
        var packageJsonPath = Path.Combine(outputPath, "package.json");
        await _filesystem.WriteFileAsync(packageJsonPath, packageJsonContent, cancellationToken);
        createdFiles.Add(packageJsonPath);

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

            var envDtsContent = GenerateEnvDts();
            var envDtsPath = Path.Combine(srcPath, "env.d.ts");
            await _filesystem.WriteFileAsync(envDtsPath, envDtsContent, cancellationToken);
            createdFiles.Add(envDtsPath);
        }

        // .gitignore
        var gitignoreContent = GenerateGitignore();
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        await _filesystem.WriteFileAsync(gitignorePath, gitignoreContent, cancellationToken);
        createdFiles.Add(gitignorePath);

        // README.md
        var readmeContent = GenerateReadme(requirements.ProjectName, requirements, isTypeScript);
        var readmePath = Path.Combine(outputPath, "README.md");
        await _filesystem.WriteFileAsync(readmePath, readmeContent, cancellationToken);
        createdFiles.Add(readmePath);

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile();
            var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
            await _filesystem.WriteFileAsync(dockerfilePath, dockerfileContent, cancellationToken);
            createdFiles.Add(dockerfilePath);

            var nginxConfContent = GenerateNginxConf();
            var nginxConfPath = Path.Combine(outputPath, "nginx.conf");
            await _filesystem.WriteFileAsync(nginxConfPath, nginxConfContent, cancellationToken);
            createdFiles.Add(nginxConfPath);
        }

        _logger.LogInformation("Generated {Count} files", createdFiles.Count);
        return createdFiles;
    }

    public Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "ts" : "js";

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "index.html", Description = "HTML entry point" },
            new() { RelativePath = $"vite.config.{ext}", Description = "Vite configuration" },
            new() { RelativePath = "src", IsDirectory = true, Description = "Source directory" },
            new() { RelativePath = $"src/main.{ext}", Description = "Application entry point" },
            new() { RelativePath = "src/App.vue", Description = "Root Vue component" },
            new() { RelativePath = "src/style.css", Description = "Global styles" },
            new() { RelativePath = "src/components", IsDirectory = true, Description = "Vue components" },
            new() { RelativePath = "src/components/HelloWorld.vue", Description = "Sample component" },
            new() { RelativePath = "src/views", IsDirectory = true, Description = "View components" },
            new() { RelativePath = "src/views/HomeView.vue", Description = "Home view" },
            new() { RelativePath = "src/composables", IsDirectory = true, Description = "Vue composables" },
            new() { RelativePath = $"src/composables/useCounter.{ext}", Description = "Counter composable" },
            new() { RelativePath = "package.json", Description = "NPM package manifest" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (isTypeScript)
        {
            files.Add(new PlannedFile { RelativePath = "tsconfig.json", Description = "TypeScript config" });
            files.Add(new PlannedFile { RelativePath = "tsconfig.node.json", Description = "TypeScript node config" });
            files.Add(new PlannedFile { RelativePath = "src/env.d.ts", Description = "Environment type declarations" });
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
    <div id=""app""></div>
    <script type=""module"" src=""/src/main.ts""></script>
  </body>
</html>
";
    }

    private static string GenerateViteConfig(bool isTypeScript)
    {
        return @"import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 3000,
    host: true
  },
  build: {
    outDir: 'dist',
    sourcemap: true
  }
})
";
    }

    private static string GenerateMain(bool isTypeScript)
    {
        return @"import { createApp } from 'vue'
import './style.css'
import App from './App.vue'

createApp(App).mount('#app')
";
    }

    private static string GenerateApp(string projectName)
    {
        return $@"<script setup lang=""ts"">
import HelloWorld from './components/HelloWorld.vue'
</script>

<template>
  <div class=""app"">
    <header>
      <h1>{projectName}</h1>
    </header>
    <main>
      <HelloWorld msg=""Welcome to Vue 3 + Vite"" />
    </main>
  </div>
</template>

<style scoped>
.app {{
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  text-align: center;
}}

header {{
  margin-bottom: 2rem;
}}

h1 {{
  font-size: 2.5rem;
  color: #42b883;
}}
</style>
";
    }

    private static string GenerateHelloWorld(bool isTypeScript)
    {
        var scriptLang = isTypeScript ? @" lang=""ts""" : "";
        return $@"<script setup{scriptLang}>
import {{ useCounter }} from '../composables/useCounter'

defineProps<{{
  msg: string
}}>()

const {{ count, increment, decrement, reset }} = useCounter()
</script>

<template>
  <div class=""hello-world"">
    <h2>{{{{ msg }}}}</h2>

    <div class=""card"">
      <p class=""count"">Count: {{{{ count }}}}</p>
      <div class=""buttons"">
        <button @click=""decrement"">-</button>
        <button @click=""reset"">Reset</button>
        <button @click=""increment"">+</button>
      </div>
    </div>

    <p class=""docs"">
      Check out
      <a href=""https://vuejs.org/guide/quick-start.html"" target=""_blank"">Vue 3 Docs</a>
      to get started.
    </p>
  </div>
</template>

<style scoped>
.hello-world {{
  padding: 2rem;
}}

.card {{
  background: #f9f9f9;
  border-radius: 8px;
  padding: 2rem;
  margin: 2rem 0;
}}

.count {{
  font-size: 2rem;
  font-weight: bold;
  color: #42b883;
  margin-bottom: 1rem;
}}

.buttons {{
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}}

button {{
  padding: 0.5rem 1.5rem;
  font-size: 1rem;
  border: none;
  border-radius: 4px;
  background: #42b883;
  color: white;
  cursor: pointer;
  transition: background 0.2s;
}}

button:hover {{
  background: #3aa876;
}}

.docs {{
  color: #666;
}}

.docs a {{
  color: #42b883;
}}
</style>
";
    }

    private static string GenerateHomeView(bool isTypeScript)
    {
        var scriptLang = isTypeScript ? @" lang=""ts""" : "";
        return $@"<script setup{scriptLang}>
import HelloWorld from '../components/HelloWorld.vue'
</script>

<template>
  <div class=""home"">
    <HelloWorld msg=""Home View"" />
  </div>
</template>

<style scoped>
.home {{
  padding: 1rem;
}}
</style>
";
    }

    private static string GenerateUseCounter(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"import { ref, computed } from 'vue'

export function useCounter(initialValue: number = 0) {
  const count = ref(initialValue)

  const doubleCount = computed(() => count.value * 2)

  function increment() {
    count.value++
  }

  function decrement() {
    count.value--
  }

  function reset() {
    count.value = initialValue
  }

  return {
    count,
    doubleCount,
    increment,
    decrement,
    reset
  }
}
";
        }

        return @"import { ref, computed } from 'vue'

export function useCounter(initialValue = 0) {
  const count = ref(initialValue)

  const doubleCount = computed(() => count.value * 2)

  function increment() {
    count.value++
  }

  function decrement() {
    count.value--
  }

  function reset() {
    count.value = initialValue
  }

  return {
    count,
    doubleCount,
    increment,
    decrement,
    reset
  }
}
";
    }

    private static string GenerateStyleCss()
    {
        return @":root {
  font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
  line-height: 1.5;
  font-weight: 400;

  color-scheme: light dark;
  color: rgba(255, 255, 255, 0.87);
  background-color: #242424;

  font-synthesis: none;
  text-rendering: optimizeLegibility;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

a {
  font-weight: 500;
  color: #646cff;
  text-decoration: inherit;
}
a:hover {
  color: #535bf2;
}

body {
  margin: 0;
  display: flex;
  place-items: center;
  min-width: 320px;
  min-height: 100vh;
}

#app {
  max-width: 1280px;
  margin: 0 auto;
  padding: 2rem;
  text-align: center;
}

@media (prefers-color-scheme: light) {
  :root {
    color: #213547;
    background-color: #ffffff;
  }
  a:hover {
    color: #747bff;
  }
}
";
    }

    private static string GeneratePackageJson(string projectName, bool isTypeScript)
    {
        var devDeps = isTypeScript
            ? @"""typescript"": ""^5.3.0"",
    ""@types/node"": ""^20.10.0"",
    ""vue-tsc"": ""^1.8.0"","
            : "";

        return $@"{{
  ""name"": ""{projectName}"",
  ""private"": true,
  ""version"": ""0.1.0"",
  ""type"": ""module"",
  ""scripts"": {{
    ""dev"": ""vite"",
    ""build"": ""vite build"",
    ""preview"": ""vite preview""{(isTypeScript ? @",
    ""type-check"": ""vue-tsc --noEmit""" : "")}
  }},
  ""dependencies"": {{
    ""vue"": ""^3.4.0""
  }},
  ""devDependencies"": {{
    {devDeps}
    ""@vitejs/plugin-vue"": ""^5.0.0"",
    ""vite"": ""^5.0.0""
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
    ""module"": ""ESNext"",
    ""lib"": [""ES2020"", ""DOM"", ""DOM.Iterable""],
    ""skipLibCheck"": true,

    /* Bundler mode */
    ""moduleResolution"": ""bundler"",
    ""allowImportingTsExtensions"": true,
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""noEmit"": true,
    ""jsx"": ""preserve"",

    /* Linting */
    ""strict"": true,
    ""noUnusedLocals"": true,
    ""noUnusedParameters"": true,
    ""noFallthroughCasesInSwitch"": true
  },
  ""include"": [""src/**/*.ts"", ""src/**/*.tsx"", ""src/**/*.vue""],
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

    private static string GenerateEnvDts()
    {
        return @"/// <reference types=""vite/client"" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Logs
logs
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*
pnpm-debug.log*
lerna-debug.log*

node_modules
dist
dist-ssr
*.local

# Editor directories and files
.vscode/*
!.vscode/extensions.json
.idea
.DS_Store
*.suo
*.ntvs*
*.njsproj
*.sln
*.sw?

# Environment
.env
.env.local
.env.*.local
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements, bool isTypeScript)
    {
        var lang = isTypeScript ? "TypeScript" : "JavaScript";
        return $@"# {projectName}

{requirements.Description ?? $"A Vue.js 3 application built with Vite and {lang}."}

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

The application will be available at `http://localhost:3000`.

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```
{(isTypeScript ? @"
### Type Check

```bash
npm run type-check
```
" : "")}
## Project Structure

```
{projectName}/
├── public/              # Static assets
├── src/
│   ├── components/      # Vue components
│   ├── composables/     # Vue composables (hooks)
│   ├── views/           # View components
│   ├── App.vue          # Root component
│   ├── main.{(isTypeScript ? "ts" : "js")}           # Application entry
│   └── style.css        # Global styles
├── index.html           # HTML entry point
├── vite.config.{(isTypeScript ? "ts" : "js")}       # Vite configuration
└── package.json
```

## Features

- Vue 3 with Composition API
- Vite for fast development and building
- {lang} support
- Hot Module Replacement (HMR)
- CSS scoped to components

## License

MIT
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM node:20-alpine AS build

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD [""nginx"", ""-g"", ""daemon off;""]
";
    }

    private static string GenerateNginxConf()
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
