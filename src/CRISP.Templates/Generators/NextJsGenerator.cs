using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Next.js projects with App Router.
/// </summary>
public sealed class NextJsGenerator : IProjectGenerator
{
    private readonly ILogger<NextJsGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public NextJsGenerator(
        ILogger<NextJsGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "nextjs";
    public string TemplateName => "Next.js";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return (requirements.Language == ProjectLanguage.JavaScript ||
                requirements.Language == ProjectLanguage.TypeScript) &&
               requirements.Framework == ProjectFramework.NextJs;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Next.js project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "tsx" : "jsx";
        var extJs = isTypeScript ? "ts" : "js";

        // Create app directory structure (App Router)
        var appPath = Path.Combine(outputPath, "app");
        await _filesystem.CreateDirectoryAsync(appPath, cancellationToken);

        var componentsPath = Path.Combine(outputPath, "components");
        await _filesystem.CreateDirectoryAsync(componentsPath, cancellationToken);

        var libPath = Path.Combine(outputPath, "lib");
        await _filesystem.CreateDirectoryAsync(libPath, cancellationToken);

        var publicPath = Path.Combine(outputPath, "public");
        await _filesystem.CreateDirectoryAsync(publicPath, cancellationToken);

        // App Router files
        var layoutContent = GenerateLayout(requirements.ProjectName, isTypeScript);
        var layoutPath = Path.Combine(appPath, $"layout.{ext}");
        await _filesystem.WriteFileAsync(layoutPath, layoutContent, cancellationToken);
        createdFiles.Add(layoutPath);

        var pageContent = GeneratePage(requirements.ProjectName, isTypeScript);
        var pagePath = Path.Combine(appPath, $"page.{ext}");
        await _filesystem.WriteFileAsync(pagePath, pageContent, cancellationToken);
        createdFiles.Add(pagePath);

        var globalsCssContent = GenerateGlobalsCss();
        var globalsCssPath = Path.Combine(appPath, "globals.css");
        await _filesystem.WriteFileAsync(globalsCssPath, globalsCssContent, cancellationToken);
        createdFiles.Add(globalsCssPath);

        // API route example
        var apiPath = Path.Combine(appPath, "api", "hello");
        await _filesystem.CreateDirectoryAsync(apiPath, cancellationToken);

        var apiRouteContent = GenerateApiRoute(isTypeScript);
        var apiRoutePath = Path.Combine(apiPath, $"route.{extJs}");
        await _filesystem.WriteFileAsync(apiRoutePath, apiRouteContent, cancellationToken);
        createdFiles.Add(apiRoutePath);

        // Components
        var headerContent = GenerateHeader(isTypeScript);
        var headerPath = Path.Combine(componentsPath, $"Header.{ext}");
        await _filesystem.WriteFileAsync(headerPath, headerContent, cancellationToken);
        createdFiles.Add(headerPath);

        var footerContent = GenerateFooter(isTypeScript);
        var footerPath = Path.Combine(componentsPath, $"Footer.{ext}");
        await _filesystem.WriteFileAsync(footerPath, footerContent, cancellationToken);
        createdFiles.Add(footerPath);

        // next.config.js
        var nextConfigContent = GenerateNextConfig(isTypeScript);
        var nextConfigPath = Path.Combine(outputPath, $"next.config.{extJs}");
        await _filesystem.WriteFileAsync(nextConfigPath, nextConfigContent, cancellationToken);
        createdFiles.Add(nextConfigPath);

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
        var eslintContent = GenerateEslintConfig();
        var eslintPath = Path.Combine(outputPath, ".eslintrc.json");
        await _filesystem.WriteFileAsync(eslintPath, eslintContent, cancellationToken);
        createdFiles.Add(eslintPath);

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
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;
        var ext = isTypeScript ? "tsx" : "jsx";
        var extJs = isTypeScript ? "ts" : "js";

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "app", IsDirectory = true, Description = "App Router directory" },
            new() { RelativePath = $"app/layout.{ext}", Description = "Root layout" },
            new() { RelativePath = $"app/page.{ext}", Description = "Home page" },
            new() { RelativePath = "app/globals.css", Description = "Global styles" },
            new() { RelativePath = "app/api/hello", IsDirectory = true, Description = "API route" },
            new() { RelativePath = $"app/api/hello/route.{extJs}", Description = "Hello API endpoint" },
            new() { RelativePath = "components", IsDirectory = true, Description = "React components" },
            new() { RelativePath = $"components/Header.{ext}", Description = "Header component" },
            new() { RelativePath = $"components/Footer.{ext}", Description = "Footer component" },
            new() { RelativePath = "lib", IsDirectory = true, Description = "Utility functions" },
            new() { RelativePath = "public", IsDirectory = true, Description = "Static assets" },
            new() { RelativePath = $"next.config.{extJs}", Description = "Next.js configuration" },
            new() { RelativePath = "package.json", Description = "Package manifest" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = ".eslintrc.json", Description = "ESLint configuration" }
        };

        if (isTypeScript)
        {
            files.Add(new PlannedFile { RelativePath = "tsconfig.json", Description = "TypeScript configuration" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = ".dockerignore", Description = "Docker ignore file" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateLayout(string projectName, bool isTypeScript)
    {
        var typeAnnotation = isTypeScript ? ": Metadata" : "";
        var childrenType = isTypeScript ? "{ children }: { children: React.ReactNode }" : "{ children }";

        return $@"import type {{ Metadata }} from 'next'
import './globals.css'
import Header from '@/components/Header'
import Footer from '@/components/Footer'

export const metadata{typeAnnotation} = {{
  title: '{projectName}',
  description: 'Built with Next.js',
}}

export default function RootLayout({childrenType}) {{
  return (
    <html lang=""en"">
      <body>
        <Header />
        <main>{{children}}</main>
        <Footer />
      </body>
    </html>
  )
}}
";
    }

    private static string GeneratePage(string projectName, bool isTypeScript)
    {
        return $@"export default function Home() {{
  return (
    <div className=""container"">
      <h1>Welcome to {projectName}</h1>
      <p>Get started by editing <code>app/page.tsx</code></p>

      <div className=""grid"">
        <a href=""https://nextjs.org/docs"" className=""card"">
          <h2>Documentation &rarr;</h2>
          <p>Find in-depth information about Next.js features and API.</p>
        </a>

        <a href=""https://nextjs.org/learn"" className=""card"">
          <h2>Learn &rarr;</h2>
          <p>Learn about Next.js in an interactive course with quizzes!</p>
        </a>

        <a href=""/api/hello"" className=""card"">
          <h2>API Routes &rarr;</h2>
          <p>Test the API route at /api/hello</p>
        </a>
      </div>
    </div>
  )
}}
";
    }

    private static string GenerateGlobalsCss()
    {
        return @":root {
  --foreground: #171717;
  --background: #ffffff;
  --primary: #0070f3;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html,
body {
  max-width: 100vw;
  min-height: 100vh;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  color: var(--foreground);
  background: var(--background);
}

main {
  min-height: calc(100vh - 140px);
  padding: 2rem;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  text-align: center;
}

h1 {
  font-size: 2.5rem;
  margin-bottom: 1rem;
}

code {
  background: #f4f4f4;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.9rem;
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

.card {
  padding: 1.5rem;
  border: 1px solid #eaeaea;
  border-radius: 10px;
  text-decoration: none;
  color: inherit;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.card:hover {
  border-color: var(--primary);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.card h2 {
  font-size: 1.25rem;
  margin-bottom: 0.5rem;
}

.card p {
  font-size: 0.9rem;
  color: #666;
}
";
    }

    private static string GenerateApiRoute(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"import { NextResponse } from 'next/server'

export async function GET() {
  return NextResponse.json({ message: 'Hello from Next.js!' })
}
";
        }

        return @"import { NextResponse } from 'next/server'

export async function GET() {
  return NextResponse.json({ message: 'Hello from Next.js!' })
}
";
    }

    private static string GenerateHeader(bool isTypeScript)
    {
        return @"import Link from 'next/link'

export default function Header() {
  return (
    <header style={{
      padding: '1rem 2rem',
      borderBottom: '1px solid #eaeaea',
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center'
    }}>
      <Link href=""/"" style={{ fontWeight: 'bold', fontSize: '1.25rem', textDecoration: 'none', color: 'inherit' }}>
        Logo
      </Link>
      <nav style={{ display: 'flex', gap: '1.5rem' }}>
        <Link href=""/"" style={{ textDecoration: 'none', color: 'inherit' }}>Home</Link>
        <Link href=""/api/hello"" style={{ textDecoration: 'none', color: 'inherit' }}>API</Link>
      </nav>
    </header>
  )
}
";
    }

    private static string GenerateFooter(bool isTypeScript)
    {
        return @"export default function Footer() {
  return (
    <footer style={{
      padding: '1.5rem',
      borderTop: '1px solid #eaeaea',
      textAlign: 'center',
      color: '#666',
      fontSize: '0.9rem'
    }}>
      Built with Next.js
    </footer>
  )
}
";
    }

    private static string GenerateNextConfig(bool isTypeScript)
    {
        return @"/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
}

module.exports = nextConfig
";
    }

    private static string GeneratePackageJson(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        var devDeps = new List<string>
        {
            "\"eslint\": \"^8.55.0\"",
            "\"eslint-config-next\": \"14.0.0\""
        };

        if (isTypeScript)
        {
            devDeps.Add("\"typescript\": \"^5.3.0\"");
            devDeps.Add("\"@types/node\": \"^20.11.0\"");
            devDeps.Add("\"@types/react\": \"^18.2.0\"");
            devDeps.Add("\"@types/react-dom\": \"^18.2.0\"");
        }

        return $@"{{
  ""name"": ""{projectName}"",
  ""version"": ""0.1.0"",
  ""private"": true,
  ""scripts"": {{
    ""dev"": ""next dev"",
    ""build"": ""next build"",
    ""start"": ""next start"",
    ""lint"": ""next lint""
  }},
  ""dependencies"": {{
    ""next"": ""14.0.0"",
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
    ""target"": ""es5"",
    ""lib"": [""dom"", ""dom.iterable"", ""esnext""],
    ""allowJs"": true,
    ""skipLibCheck"": true,
    ""strict"": true,
    ""noEmit"": true,
    ""esModuleInterop"": true,
    ""module"": ""esnext"",
    ""moduleResolution"": ""bundler"",
    ""resolveJsonModule"": true,
    ""isolatedModules"": true,
    ""jsx"": ""preserve"",
    ""incremental"": true,
    ""plugins"": [
      {
        ""name"": ""next""
      }
    ],
    ""paths"": {
      ""@/*"": [""./*""]
    }
  },
  ""include"": [""next-env.d.ts"", ""**/*.ts"", ""**/*.tsx"", "".next/types/**/*.ts""],
  ""exclude"": [""node_modules""]
}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Dependencies
node_modules/

# Next.js
.next/
out/

# Production
build/

# Misc
.DS_Store
*.pem

# Debug
npm-debug.log*

# Local env files
.env*.local

# Vercel
.vercel

# TypeScript
*.tsbuildinfo
next-env.d.ts
";
    }

    private static string GenerateReadme(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A Next.js application with App Router."}

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

Open [http://localhost:3000](http://localhost:3000) with your browser.

### Build

```bash
npm run build
npm start
```

## Project Structure

```
{projectName}/
├── app/
│   ├── api/
│   │   └── hello/
│   │       └── route.ts
│   ├── globals.css
│   ├── layout.tsx
│   └── page.tsx
├── components/
│   ├── Header.tsx
│   └── Footer.tsx
├── lib/
├── public/
├── next.config.js
└── package.json
```

## Learn More

- [Next.js Documentation](https://nextjs.org/docs)
- [Learn Next.js](https://nextjs.org/learn)

## License

MIT
";
    }

    private static string GenerateEslintConfig()
    {
        return @"{
  ""extends"": ""next/core-web-vitals""
}
";
    }

    private static string GenerateDockerfile()
    {
        return @"FROM node:20-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV production

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000
ENV PORT 3000

CMD [""node"", ""server.js""]
";
    }

    private static string GenerateDockerignore()
    {
        return @"node_modules
.next
.git
*.md
.env*
";
    }
}
