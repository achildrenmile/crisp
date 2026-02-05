using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for Node.js Express projects.
/// </summary>
public sealed class ExpressGenerator : IProjectGenerator
{
    private readonly ILogger<ExpressGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public ExpressGenerator(
        ILogger<ExpressGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "nodejs-express";
    public string TemplateName => "Node.js Express";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return (requirements.Language == ProjectLanguage.JavaScript ||
                requirements.Language == ProjectLanguage.TypeScript) &&
               requirements.Framework == ProjectFramework.Express;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Express project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();
        var isTypeScript = requirements.Language == ProjectLanguage.TypeScript;

        // Create src directory
        var srcPath = Path.Combine(outputPath, "src");
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        // Create routes directory
        var routesPath = Path.Combine(srcPath, "routes");
        await _filesystem.CreateDirectoryAsync(routesPath, cancellationToken);

        // Create middleware directory
        var middlewarePath = Path.Combine(srcPath, "middleware");
        await _filesystem.CreateDirectoryAsync(middlewarePath, cancellationToken);

        // Generate main files
        if (isTypeScript)
        {
            var appContent = GenerateTypeScriptApp(requirements.ProjectName);
            var appPath = Path.Combine(srcPath, "app.ts");
            await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
            createdFiles.Add(appPath);

            var indexContent = GenerateTypeScriptIndex();
            var indexPath = Path.Combine(srcPath, "index.ts");
            await _filesystem.WriteFileAsync(indexPath, indexContent, cancellationToken);
            createdFiles.Add(indexPath);

            var routesContent = GenerateTypeScriptRoutes();
            var routesFilePath = Path.Combine(routesPath, "items.ts");
            await _filesystem.WriteFileAsync(routesFilePath, routesContent, cancellationToken);
            createdFiles.Add(routesFilePath);

            var errorMiddleware = GenerateTypeScriptErrorMiddleware();
            var errorMiddlewarePath = Path.Combine(middlewarePath, "errorHandler.ts");
            await _filesystem.WriteFileAsync(errorMiddlewarePath, errorMiddleware, cancellationToken);
            createdFiles.Add(errorMiddlewarePath);

            var tsconfigContent = GenerateTsConfig();
            var tsconfigPath = Path.Combine(outputPath, "tsconfig.json");
            await _filesystem.WriteFileAsync(tsconfigPath, tsconfigContent, cancellationToken);
            createdFiles.Add(tsconfigPath);
        }
        else
        {
            var appContent = GenerateJavaScriptApp(requirements.ProjectName);
            var appPath = Path.Combine(srcPath, "app.js");
            await _filesystem.WriteFileAsync(appPath, appContent, cancellationToken);
            createdFiles.Add(appPath);

            var indexContent = GenerateJavaScriptIndex();
            var indexPath = Path.Combine(srcPath, "index.js");
            await _filesystem.WriteFileAsync(indexPath, indexContent, cancellationToken);
            createdFiles.Add(indexPath);

            var routesContent = GenerateJavaScriptRoutes();
            var routesFilePath = Path.Combine(routesPath, "items.js");
            await _filesystem.WriteFileAsync(routesFilePath, routesContent, cancellationToken);
            createdFiles.Add(routesFilePath);

            var errorMiddleware = GenerateJavaScriptErrorMiddleware();
            var errorMiddlewarePath = Path.Combine(middlewarePath, "errorHandler.js");
            await _filesystem.WriteFileAsync(errorMiddlewarePath, errorMiddleware, cancellationToken);
            createdFiles.Add(errorMiddlewarePath);
        }

        // package.json
        var packageContent = GeneratePackageJson(requirements.ProjectName, isTypeScript, requirements);
        var packagePath = Path.Combine(outputPath, "package.json");
        await _filesystem.WriteFileAsync(packagePath, packageContent, cancellationToken);
        createdFiles.Add(packagePath);

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

        // .env.example
        var envContent = GenerateEnvExample();
        var envPath = Path.Combine(outputPath, ".env.example");
        await _filesystem.WriteFileAsync(envPath, envContent, cancellationToken);
        createdFiles.Add(envPath);

        // ESLint config
        if (requirements.LintingTools.Contains("ESLint") || isTypeScript)
        {
            var eslintContent = GenerateEslintConfig(isTypeScript);
            var eslintPath = Path.Combine(outputPath, "eslint.config.js");
            await _filesystem.WriteFileAsync(eslintPath, eslintContent, cancellationToken);
            createdFiles.Add(eslintPath);
        }

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var testsPath = Path.Combine(outputPath, "tests");
            await _filesystem.CreateDirectoryAsync(testsPath, cancellationToken);

            var testContent = isTypeScript ? GenerateTypeScriptTests() : GenerateJavaScriptTests();
            var testFile = isTypeScript ? "app.test.ts" : "app.test.js";
            var testFilePath = Path.Combine(testsPath, testFile);
            await _filesystem.WriteFileAsync(testFilePath, testContent, cancellationToken);
            createdFiles.Add(testFilePath);

            var jestContent = GenerateJestConfig(isTypeScript);
            var jestPath = Path.Combine(outputPath, "jest.config.js");
            await _filesystem.WriteFileAsync(jestPath, jestContent, cancellationToken);
            createdFiles.Add(jestPath);
        }

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile(isTypeScript);
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
        var ext = isTypeScript ? "ts" : "js";

        var files = new List<PlannedFile>
        {
            new() { RelativePath = "src", IsDirectory = true, Description = "Source code" },
            new() { RelativePath = $"src/app.{ext}", Description = "Express application" },
            new() { RelativePath = $"src/index.{ext}", Description = "Entry point" },
            new() { RelativePath = "src/routes", IsDirectory = true, Description = "Route handlers" },
            new() { RelativePath = $"src/routes/items.{ext}", Description = "Items routes" },
            new() { RelativePath = "src/middleware", IsDirectory = true, Description = "Middleware" },
            new() { RelativePath = $"src/middleware/errorHandler.{ext}", Description = "Error handler" },
            new() { RelativePath = "package.json", Description = "Node.js package manifest" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" },
            new() { RelativePath = ".env.example", Description = "Environment variables template" }
        };

        if (isTypeScript)
        {
            files.Add(new PlannedFile { RelativePath = "tsconfig.json", Description = "TypeScript configuration" });
        }

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "tests", IsDirectory = true, Description = "Test files" });
            files.Add(new PlannedFile { RelativePath = $"tests/app.test.{ext}", Description = "Application tests" });
            files.Add(new PlannedFile { RelativePath = "jest.config.js", Description = "Jest configuration" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = ".dockerignore", Description = "Docker ignore file" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateTypeScriptApp(string projectName)
    {
        return $@"import express, {{ Express, Request, Response }} from 'express';
import cors from 'cors';
import helmet from 'helmet';
import itemsRouter from './routes/items';
import {{ errorHandler }} from './middleware/errorHandler';

const app: Express = express();

// Middleware
app.use(helmet());
app.use(cors());
app.use(express.json());

// Routes
app.get('/', (req: Request, res: Response) => {{
  res.json({{ message: 'Welcome to {projectName}' }});
}});

app.get('/health', (req: Request, res: Response) => {{
  res.json({{ status: 'healthy' }});
}});

app.use('/items', itemsRouter);

// Error handling
app.use(errorHandler);

export default app;
";
    }

    private static string GenerateTypeScriptIndex()
    {
        return @"import app from './app';

const PORT = process.env.PORT || 3000;

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
";
    }

    private static string GenerateTypeScriptRoutes()
    {
        return @"import { Router, Request, Response } from 'express';

const router = Router();

interface Item {
  id: number;
  name: string;
  description?: string;
  price: number;
}

const items: Map<number, Item> = new Map();
let currentId = 0;

// GET all items
router.get('/', (req: Request, res: Response) => {
  res.json(Array.from(items.values()));
});

// GET item by ID
router.get('/:id', (req: Request, res: Response) => {
  const id = parseInt(req.params.id);
  const item = items.get(id);

  if (!item) {
    return res.status(404).json({ error: 'Item not found' });
  }

  res.json(item);
});

// POST create item
router.post('/', (req: Request, res: Response) => {
  const { name, description, price } = req.body;

  if (!name || price === undefined) {
    return res.status(400).json({ error: 'Name and price are required' });
  }

  currentId++;
  const item: Item = { id: currentId, name, description, price };
  items.set(currentId, item);

  res.status(201).json(item);
});

// DELETE item
router.delete('/:id', (req: Request, res: Response) => {
  const id = parseInt(req.params.id);

  if (!items.has(id)) {
    return res.status(404).json({ error: 'Item not found' });
  }

  items.delete(id);
  res.status(204).send();
});

export default router;
";
    }

    private static string GenerateTypeScriptErrorMiddleware()
    {
        return @"import { Request, Response, NextFunction } from 'express';

export interface AppError extends Error {
  statusCode?: number;
}

export const errorHandler = (
  err: AppError,
  req: Request,
  res: Response,
  next: NextFunction
) => {
  const statusCode = err.statusCode || 500;
  const message = err.message || 'Internal Server Error';

  console.error(`Error: ${message}`);

  res.status(statusCode).json({
    error: message,
    ...(process.env.NODE_ENV === 'development' && { stack: err.stack }),
  });
};
";
    }

    private static string GenerateJavaScriptApp(string projectName)
    {
        return $@"const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const itemsRouter = require('./routes/items');
const {{ errorHandler }} = require('./middleware/errorHandler');

const app = express();

// Middleware
app.use(helmet());
app.use(cors());
app.use(express.json());

// Routes
app.get('/', (req, res) => {{
  res.json({{ message: 'Welcome to {projectName}' }});
}});

app.get('/health', (req, res) => {{
  res.json({{ status: 'healthy' }});
}});

app.use('/items', itemsRouter);

// Error handling
app.use(errorHandler);

module.exports = app;
";
    }

    private static string GenerateJavaScriptIndex()
    {
        return @"const app = require('./app');

const PORT = process.env.PORT || 3000;

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
";
    }

    private static string GenerateJavaScriptRoutes()
    {
        return @"const { Router } = require('express');

const router = Router();

const items = new Map();
let currentId = 0;

// GET all items
router.get('/', (req, res) => {
  res.json(Array.from(items.values()));
});

// GET item by ID
router.get('/:id', (req, res) => {
  const id = parseInt(req.params.id);
  const item = items.get(id);

  if (!item) {
    return res.status(404).json({ error: 'Item not found' });
  }

  res.json(item);
});

// POST create item
router.post('/', (req, res) => {
  const { name, description, price } = req.body;

  if (!name || price === undefined) {
    return res.status(400).json({ error: 'Name and price are required' });
  }

  currentId++;
  const item = { id: currentId, name, description, price };
  items.set(currentId, item);

  res.status(201).json(item);
});

// DELETE item
router.delete('/:id', (req, res) => {
  const id = parseInt(req.params.id);

  if (!items.has(id)) {
    return res.status(404).json({ error: 'Item not found' });
  }

  items.delete(id);
  res.status(204).send();
});

module.exports = router;
";
    }

    private static string GenerateJavaScriptErrorMiddleware()
    {
        return @"const errorHandler = (err, req, res, next) => {
  const statusCode = err.statusCode || 500;
  const message = err.message || 'Internal Server Error';

  console.error(`Error: ${message}`);

  res.status(statusCode).json({
    error: message,
    ...(process.env.NODE_ENV === 'development' && { stack: err.stack }),
  });
};

module.exports = { errorHandler };
";
    }

    private static string GeneratePackageJson(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        var scripts = new List<string>
        {
            isTypeScript ? "\"start\": \"node dist/index.js\"" : "\"start\": \"node src/index.js\"",
            isTypeScript ? "\"dev\": \"ts-node-dev --respawn src/index.ts\"" : "\"dev\": \"nodemon src/index.js\"",
        };

        if (isTypeScript)
        {
            scripts.Add("\"build\": \"tsc\"");
        }

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            scripts.Add("\"test\": \"jest\"");
        }

        var deps = new List<string>
        {
            "\"express\": \"^4.18.2\"",
            "\"cors\": \"^2.8.5\"",
            "\"helmet\": \"^7.1.0\"",
            "\"dotenv\": \"^16.4.0\""
        };

        var devDeps = new List<string>();

        if (isTypeScript)
        {
            devDeps.Add("\"typescript\": \"^5.3.0\"");
            devDeps.Add("\"@types/node\": \"^20.11.0\"");
            devDeps.Add("\"@types/express\": \"^4.17.21\"");
            devDeps.Add("\"@types/cors\": \"^2.8.17\"");
            devDeps.Add("\"ts-node-dev\": \"^2.0.0\"");
        }
        else
        {
            devDeps.Add("\"nodemon\": \"^3.0.0\"");
        }

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            devDeps.Add("\"jest\": \"^29.7.0\"");
            devDeps.Add("\"supertest\": \"^6.3.0\"");
            if (isTypeScript)
            {
                devDeps.Add("\"ts-jest\": \"^29.1.0\"");
                devDeps.Add("\"@types/jest\": \"^29.5.0\"");
                devDeps.Add("\"@types/supertest\": \"^6.0.0\"");
            }
        }

        return $@"{{
  ""name"": ""{projectName}"",
  ""version"": ""1.0.0"",
  ""description"": ""Express.js API"",
  ""main"": ""{(isTypeScript ? "dist/index.js" : "src/index.js")}"",
  ""scripts"": {{
    {string.Join(",\n    ", scripts)}
  }},
  ""dependencies"": {{
    {string.Join(",\n    ", deps)}
  }},
  ""devDependencies"": {{
    {string.Join(",\n    ", devDeps)}
  }},
  ""engines"": {{
    ""node"": "">=20.0.0""
  }}
}}
";
    }

    private static string GenerateTsConfig()
    {
        return @"{
  ""compilerOptions"": {
    ""target"": ""ES2022"",
    ""module"": ""commonjs"",
    ""lib"": [""ES2022""],
    ""outDir"": ""./dist"",
    ""rootDir"": ""./src"",
    ""strict"": true,
    ""esModuleInterop"": true,
    ""skipLibCheck"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""resolveJsonModule"": true,
    ""declaration"": true,
    ""declarationMap"": true,
    ""sourceMap"": true
  },
  ""include"": [""src/**/*""],
  ""exclude"": [""node_modules"", ""dist"", ""tests""]
}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Dependencies
node_modules/

# Build output
dist/

# Environment
.env
.env.local
.env.*.local

# Logs
logs/
*.log
npm-debug.log*

# IDE
.idea/
.vscode/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Test coverage
coverage/

# Temporary files
tmp/
temp/
";
    }

    private static string GenerateReadme(string projectName, bool isTypeScript, ProjectRequirements requirements)
    {
        var lang = isTypeScript ? "TypeScript" : "JavaScript";
        return $@"# {projectName}

{requirements.Description ?? $"A {lang} Express.js API."}

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

The API will be available at `http://localhost:3000`.

### Production

```bash
{(isTypeScript ? "npm run build\nnpm start" : "npm start")}
```

### Testing

```bash
npm test
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
│   ├── app.{(isTypeScript ? "ts" : "js")}
│   ├── index.{(isTypeScript ? "ts" : "js")}
│   ├── routes/
│   │   └── items.{(isTypeScript ? "ts" : "js")}
│   └── middleware/
│       └── errorHandler.{(isTypeScript ? "ts" : "js")}
├── tests/
├── package.json
{(isTypeScript ? "├── tsconfig.json\n" : "")}└── README.md
```

## License

MIT
";
    }

    private static string GenerateEnvExample()
    {
        return @"# Server
PORT=3000
NODE_ENV=development

# Database (if needed)
# DATABASE_URL=postgres://user:pass@localhost:5432/db
";
    }

    private static string GenerateEslintConfig(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
  {
    rules: {
      '@typescript-eslint/no-unused-vars': 'warn',
    },
  }
);
";
        }

        return @"module.exports = {
  env: {
    node: true,
    es2022: true,
  },
  extends: ['eslint:recommended'],
  parserOptions: {
    ecmaVersion: 'latest',
  },
  rules: {
    'no-unused-vars': 'warn',
  },
};
";
    }

    private static string GenerateTypeScriptTests()
    {
        return @"import request from 'supertest';
import app from '../src/app';

describe('Express App', () => {
  it('should return welcome message', async () => {
    const response = await request(app).get('/');
    expect(response.status).toBe(200);
    expect(response.body).toHaveProperty('message');
  });

  it('should return health status', async () => {
    const response = await request(app).get('/health');
    expect(response.status).toBe(200);
    expect(response.body).toEqual({ status: 'healthy' });
  });

  describe('Items API', () => {
    it('should create and get an item', async () => {
      const itemData = { name: 'Test Item', price: 9.99 };

      const createResponse = await request(app)
        .post('/items')
        .send(itemData);

      expect(createResponse.status).toBe(201);
      expect(createResponse.body.name).toBe(itemData.name);

      const id = createResponse.body.id;
      const getResponse = await request(app).get(`/items/${id}`);

      expect(getResponse.status).toBe(200);
      expect(getResponse.body.id).toBe(id);
    });

    it('should return 404 for nonexistent item', async () => {
      const response = await request(app).get('/items/99999');
      expect(response.status).toBe(404);
    });
  });
});
";
    }

    private static string GenerateJavaScriptTests()
    {
        return @"const request = require('supertest');
const app = require('../src/app');

describe('Express App', () => {
  it('should return welcome message', async () => {
    const response = await request(app).get('/');
    expect(response.status).toBe(200);
    expect(response.body).toHaveProperty('message');
  });

  it('should return health status', async () => {
    const response = await request(app).get('/health');
    expect(response.status).toBe(200);
    expect(response.body).toEqual({ status: 'healthy' });
  });

  describe('Items API', () => {
    it('should create and get an item', async () => {
      const itemData = { name: 'Test Item', price: 9.99 };

      const createResponse = await request(app)
        .post('/items')
        .send(itemData);

      expect(createResponse.status).toBe(201);
      expect(createResponse.body.name).toBe(itemData.name);

      const id = createResponse.body.id;
      const getResponse = await request(app).get(`/items/${id}`);

      expect(getResponse.status).toBe(200);
      expect(getResponse.body.id).toBe(id);
    });

    it('should return 404 for nonexistent item', async () => {
      const response = await request(app).get('/items/99999');
      expect(response.status).toBe(404);
    });
  });
});
";
    }

    private static string GenerateJestConfig(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  roots: ['<rootDir>/tests'],
  testMatch: ['**/*.test.ts'],
  collectCoverageFrom: ['src/**/*.ts'],
  coverageDirectory: 'coverage',
};
";
        }

        return @"module.exports = {
  testEnvironment: 'node',
  roots: ['<rootDir>/tests'],
  testMatch: ['**/*.test.js'],
  collectCoverageFrom: ['src/**/*.js'],
  coverageDirectory: 'coverage',
};
";
    }

    private static string GenerateDockerfile(bool isTypeScript)
    {
        if (isTypeScript)
        {
            return @"FROM node:20-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:20-alpine

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY --from=builder /app/dist ./dist

USER node
EXPOSE 3000
CMD [""node"", ""dist/index.js""]
";
        }

        return @"FROM node:20-alpine

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .

USER node
EXPOSE 3000
CMD [""node"", ""src/index.js""]
";
    }

    private static string GenerateDockerignore()
    {
        return @"node_modules
npm-debug.log
dist
coverage
.env
.env.*
*.md
.git
.gitignore
tests
";
    }
}
