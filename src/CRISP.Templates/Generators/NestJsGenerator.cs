using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Templates.Generators;

/// <summary>
/// Generator for NestJS projects.
/// </summary>
public sealed class NestJsGenerator : IProjectGenerator
{
    private readonly ILogger<NestJsGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;

    public NestJsGenerator(
        ILogger<NestJsGenerator> logger,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _filesystem = filesystem;
    }

    public string TemplateId => "nestjs";
    public string TemplateName => "NestJS";
    public string Version => "1.0.0";

    public bool SupportsRequirements(ProjectRequirements requirements)
    {
        return requirements.Language == ProjectLanguage.TypeScript &&
               requirements.Framework == ProjectFramework.NestJs;
    }

    public async Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating NestJS project: {ProjectName}", requirements.ProjectName);

        var createdFiles = new List<string>();

        // Create directories
        var srcPath = Path.Combine(outputPath, "src");
        await _filesystem.CreateDirectoryAsync(srcPath, cancellationToken);

        var itemsPath = Path.Combine(srcPath, "items");
        await _filesystem.CreateDirectoryAsync(itemsPath, cancellationToken);

        var dtoPath = Path.Combine(itemsPath, "dto");
        await _filesystem.CreateDirectoryAsync(dtoPath, cancellationToken);

        var testPath = Path.Combine(outputPath, "test");
        await _filesystem.CreateDirectoryAsync(testPath, cancellationToken);

        // main.ts
        var mainContent = GenerateMain();
        var mainPath = Path.Combine(srcPath, "main.ts");
        await _filesystem.WriteFileAsync(mainPath, mainContent, cancellationToken);
        createdFiles.Add(mainPath);

        // app.module.ts
        var appModuleContent = GenerateAppModule();
        var appModulePath = Path.Combine(srcPath, "app.module.ts");
        await _filesystem.WriteFileAsync(appModulePath, appModuleContent, cancellationToken);
        createdFiles.Add(appModulePath);

        // app.controller.ts
        var appControllerContent = GenerateAppController(requirements.ProjectName);
        var appControllerPath = Path.Combine(srcPath, "app.controller.ts");
        await _filesystem.WriteFileAsync(appControllerPath, appControllerContent, cancellationToken);
        createdFiles.Add(appControllerPath);

        // app.service.ts
        var appServiceContent = GenerateAppService();
        var appServicePath = Path.Combine(srcPath, "app.service.ts");
        await _filesystem.WriteFileAsync(appServicePath, appServiceContent, cancellationToken);
        createdFiles.Add(appServicePath);

        // items/items.module.ts
        var itemsModuleContent = GenerateItemsModule();
        var itemsModulePath = Path.Combine(itemsPath, "items.module.ts");
        await _filesystem.WriteFileAsync(itemsModulePath, itemsModuleContent, cancellationToken);
        createdFiles.Add(itemsModulePath);

        // items/items.controller.ts
        var itemsControllerContent = GenerateItemsController();
        var itemsControllerPath = Path.Combine(itemsPath, "items.controller.ts");
        await _filesystem.WriteFileAsync(itemsControllerPath, itemsControllerContent, cancellationToken);
        createdFiles.Add(itemsControllerPath);

        // items/items.service.ts
        var itemsServiceContent = GenerateItemsService();
        var itemsServicePath = Path.Combine(itemsPath, "items.service.ts");
        await _filesystem.WriteFileAsync(itemsServicePath, itemsServiceContent, cancellationToken);
        createdFiles.Add(itemsServicePath);

        // items/dto/create-item.dto.ts
        var createItemDtoContent = GenerateCreateItemDto();
        var createItemDtoPath = Path.Combine(dtoPath, "create-item.dto.ts");
        await _filesystem.WriteFileAsync(createItemDtoPath, createItemDtoContent, cancellationToken);
        createdFiles.Add(createItemDtoPath);

        // items/item.entity.ts
        var itemEntityContent = GenerateItemEntity();
        var itemEntityPath = Path.Combine(itemsPath, "item.entity.ts");
        await _filesystem.WriteFileAsync(itemEntityPath, itemEntityContent, cancellationToken);
        createdFiles.Add(itemEntityPath);

        // package.json
        var packageJsonContent = GeneratePackageJson(requirements.ProjectName);
        var packageJsonPath = Path.Combine(outputPath, "package.json");
        await _filesystem.WriteFileAsync(packageJsonPath, packageJsonContent, cancellationToken);
        createdFiles.Add(packageJsonPath);

        // tsconfig.json
        var tsconfigContent = GenerateTsConfig();
        var tsconfigPath = Path.Combine(outputPath, "tsconfig.json");
        await _filesystem.WriteFileAsync(tsconfigPath, tsconfigContent, cancellationToken);
        createdFiles.Add(tsconfigPath);

        // tsconfig.build.json
        var tsconfigBuildContent = GenerateTsConfigBuild();
        var tsconfigBuildPath = Path.Combine(outputPath, "tsconfig.build.json");
        await _filesystem.WriteFileAsync(tsconfigBuildPath, tsconfigBuildContent, cancellationToken);
        createdFiles.Add(tsconfigBuildPath);

        // nest-cli.json
        var nestCliContent = GenerateNestCli();
        var nestCliPath = Path.Combine(outputPath, "nest-cli.json");
        await _filesystem.WriteFileAsync(nestCliPath, nestCliContent, cancellationToken);
        createdFiles.Add(nestCliPath);

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

        // Tests
        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            var appControllerSpecContent = GenerateAppControllerSpec();
            var appControllerSpecPath = Path.Combine(srcPath, "app.controller.spec.ts");
            await _filesystem.WriteFileAsync(appControllerSpecPath, appControllerSpecContent, cancellationToken);
            createdFiles.Add(appControllerSpecPath);

            var itemsControllerSpecContent = GenerateItemsControllerSpec();
            var itemsControllerSpecPath = Path.Combine(itemsPath, "items.controller.spec.ts");
            await _filesystem.WriteFileAsync(itemsControllerSpecPath, itemsControllerSpecContent, cancellationToken);
            createdFiles.Add(itemsControllerSpecPath);

            var jestConfigContent = GenerateJestConfig();
            var jestConfigPath = Path.Combine(outputPath, "jest.config.js");
            await _filesystem.WriteFileAsync(jestConfigPath, jestConfigContent, cancellationToken);
            createdFiles.Add(jestConfigPath);

            var e2eSpecContent = GenerateE2ESpec(requirements.ProjectName);
            var e2eSpecPath = Path.Combine(testPath, "app.e2e-spec.ts");
            await _filesystem.WriteFileAsync(e2eSpecPath, e2eSpecContent, cancellationToken);
            createdFiles.Add(e2eSpecPath);

            var jestE2EContent = GenerateJestE2EConfig();
            var jestE2EPath = Path.Combine(testPath, "jest-e2e.json");
            await _filesystem.WriteFileAsync(jestE2EPath, jestE2EContent, cancellationToken);
            createdFiles.Add(jestE2EPath);
        }

        // Docker support
        if (requirements.IncludeContainerSupport)
        {
            var dockerfileContent = GenerateDockerfile();
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
            new() { RelativePath = "src", IsDirectory = true, Description = "Source directory" },
            new() { RelativePath = "src/main.ts", Description = "Application entry point" },
            new() { RelativePath = "src/app.module.ts", Description = "Root module" },
            new() { RelativePath = "src/app.controller.ts", Description = "App controller" },
            new() { RelativePath = "src/app.service.ts", Description = "App service" },
            new() { RelativePath = "src/items", IsDirectory = true, Description = "Items feature module" },
            new() { RelativePath = "src/items/items.module.ts", Description = "Items module" },
            new() { RelativePath = "src/items/items.controller.ts", Description = "Items controller" },
            new() { RelativePath = "src/items/items.service.ts", Description = "Items service" },
            new() { RelativePath = "src/items/item.entity.ts", Description = "Item entity" },
            new() { RelativePath = "src/items/dto/create-item.dto.ts", Description = "Create item DTO" },
            new() { RelativePath = "package.json", Description = "NPM package manifest" },
            new() { RelativePath = "tsconfig.json", Description = "TypeScript config" },
            new() { RelativePath = "tsconfig.build.json", Description = "TypeScript build config" },
            new() { RelativePath = "nest-cli.json", Description = "NestJS CLI config" },
            new() { RelativePath = ".gitignore", Description = "Git ignore file" },
            new() { RelativePath = "README.md", Description = "Project readme" }
        };

        if (!string.IsNullOrEmpty(requirements.TestingFramework))
        {
            files.Add(new PlannedFile { RelativePath = "src/app.controller.spec.ts", Description = "App controller tests" });
            files.Add(new PlannedFile { RelativePath = "src/items/items.controller.spec.ts", Description = "Items controller tests" });
            files.Add(new PlannedFile { RelativePath = "jest.config.js", Description = "Jest configuration" });
            files.Add(new PlannedFile { RelativePath = "test/app.e2e-spec.ts", Description = "E2E tests" });
            files.Add(new PlannedFile { RelativePath = "test/jest-e2e.json", Description = "Jest E2E config" });
        }

        if (requirements.IncludeContainerSupport)
        {
            files.Add(new PlannedFile { RelativePath = "Dockerfile", Description = "Docker container definition" });
            files.Add(new PlannedFile { RelativePath = "docker-compose.yml", Description = "Docker Compose configuration" });
        }

        return Task.FromResult<IReadOnlyList<PlannedFile>>(files);
    }

    private static string GenerateMain()
    {
        return @"import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { AppModule } from './app.module';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);

  app.useGlobalPipes(new ValidationPipe({
    whitelist: true,
    transform: true,
  }));

  app.enableCors();

  const port = process.env.PORT || 3000;
  await app.listen(port);
  console.log(`Application is running on: http://localhost:${port}`);
}
bootstrap();
";
    }

    private static string GenerateAppModule()
    {
        return @"import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { ItemsModule } from './items/items.module';

@Module({
  imports: [ItemsModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
";
    }

    private static string GenerateAppController(string projectName)
    {
        return $@"import {{ Controller, Get }} from '@nestjs/common';
import {{ AppService }} from './app.service';

@Controller()
export class AppController {{
  constructor(private readonly appService: AppService) {{}}

  @Get()
  getRoot() {{
    return {{
      message: 'Welcome to {projectName}',
      version: '1.0.0',
    }};
  }}

  @Get('health')
  getHealth() {{
    return this.appService.getHealth();
  }}
}}
";
    }

    private static string GenerateAppService()
    {
        return @"import { Injectable } from '@nestjs/common';

@Injectable()
export class AppService {
  getHealth() {
    return { status: 'healthy' };
  }
}
";
    }

    private static string GenerateItemsModule()
    {
        return @"import { Module } from '@nestjs/common';
import { ItemsController } from './items.controller';
import { ItemsService } from './items.service';

@Module({
  controllers: [ItemsController],
  providers: [ItemsService],
})
export class ItemsModule {}
";
    }

    private static string GenerateItemsController()
    {
        return @"import {
  Controller,
  Get,
  Post,
  Delete,
  Body,
  Param,
  ParseIntPipe,
  HttpCode,
  HttpStatus,
  NotFoundException,
} from '@nestjs/common';
import { ItemsService } from './items.service';
import { CreateItemDto } from './dto/create-item.dto';
import { Item } from './item.entity';

@Controller('items')
export class ItemsController {
  constructor(private readonly itemsService: ItemsService) {}

  @Get()
  findAll(): Item[] {
    return this.itemsService.findAll();
  }

  @Get(':id')
  findOne(@Param('id', ParseIntPipe) id: number): Item {
    const item = this.itemsService.findOne(id);
    if (!item) {
      throw new NotFoundException(`Item with ID ${id} not found`);
    }
    return item;
  }

  @Post()
  create(@Body() createItemDto: CreateItemDto): Item {
    return this.itemsService.create(createItemDto);
  }

  @Delete(':id')
  @HttpCode(HttpStatus.NO_CONTENT)
  remove(@Param('id', ParseIntPipe) id: number): void {
    const deleted = this.itemsService.remove(id);
    if (!deleted) {
      throw new NotFoundException(`Item with ID ${id} not found`);
    }
  }
}
";
    }

    private static string GenerateItemsService()
    {
        return @"import { Injectable } from '@nestjs/common';
import { CreateItemDto } from './dto/create-item.dto';
import { Item } from './item.entity';

@Injectable()
export class ItemsService {
  private items: Item[] = [];
  private idCounter = 1;

  findAll(): Item[] {
    return this.items;
  }

  findOne(id: number): Item | undefined {
    return this.items.find(item => item.id === id);
  }

  create(createItemDto: CreateItemDto): Item {
    const item: Item = {
      id: this.idCounter++,
      ...createItemDto,
      createdAt: new Date(),
    };
    this.items.push(item);
    return item;
  }

  remove(id: number): boolean {
    const index = this.items.findIndex(item => item.id === id);
    if (index === -1) {
      return false;
    }
    this.items.splice(index, 1);
    return true;
  }
}
";
    }

    private static string GenerateCreateItemDto()
    {
        return @"import { IsString, IsNumber, IsOptional, Min } from 'class-validator';

export class CreateItemDto {
  @IsString()
  name: string;

  @IsString()
  @IsOptional()
  description?: string;

  @IsNumber()
  @Min(0)
  price: number;
}
";
    }

    private static string GenerateItemEntity()
    {
        return @"export interface Item {
  id: number;
  name: string;
  description?: string;
  price: number;
  createdAt: Date;
}
";
    }

    private static string GeneratePackageJson(string projectName)
    {
        return $@"{{
  ""name"": ""{projectName}"",
  ""version"": ""0.1.0"",
  ""description"": ""NestJS REST API"",
  ""private"": true,
  ""scripts"": {{
    ""build"": ""nest build"",
    ""format"": ""prettier --write \""src/**/*.ts\"" \""test/**/*.ts\"""",
    ""start"": ""nest start"",
    ""start:dev"": ""nest start --watch"",
    ""start:debug"": ""nest start --debug --watch"",
    ""start:prod"": ""node dist/main"",
    ""lint"": ""eslint \""{{src,test}}/**/*.ts\"" --fix"",
    ""test"": ""jest"",
    ""test:watch"": ""jest --watch"",
    ""test:cov"": ""jest --coverage"",
    ""test:e2e"": ""jest --config ./test/jest-e2e.json""
  }},
  ""dependencies"": {{
    ""@nestjs/common"": ""^10.3.0"",
    ""@nestjs/core"": ""^10.3.0"",
    ""@nestjs/platform-express"": ""^10.3.0"",
    ""class-transformer"": ""^0.5.1"",
    ""class-validator"": ""^0.14.0"",
    ""reflect-metadata"": ""^0.2.0"",
    ""rxjs"": ""^7.8.0""
  }},
  ""devDependencies"": {{
    ""@nestjs/cli"": ""^10.3.0"",
    ""@nestjs/schematics"": ""^10.1.0"",
    ""@nestjs/testing"": ""^10.3.0"",
    ""@types/express"": ""^4.17.21"",
    ""@types/jest"": ""^29.5.0"",
    ""@types/node"": ""^20.10.0"",
    ""@types/supertest"": ""^6.0.0"",
    ""jest"": ""^29.7.0"",
    ""source-map-support"": ""^0.5.21"",
    ""supertest"": ""^6.3.0"",
    ""ts-jest"": ""^29.1.0"",
    ""ts-loader"": ""^9.5.0"",
    ""ts-node"": ""^10.9.0"",
    ""tsconfig-paths"": ""^4.2.0"",
    ""typescript"": ""^5.3.0""
  }}
}}
";
    }

    private static string GenerateTsConfig()
    {
        return @"{
  ""compilerOptions"": {
    ""module"": ""commonjs"",
    ""declaration"": true,
    ""removeComments"": true,
    ""emitDecoratorMetadata"": true,
    ""experimentalDecorators"": true,
    ""allowSyntheticDefaultImports"": true,
    ""target"": ""ES2021"",
    ""sourceMap"": true,
    ""outDir"": ""./dist"",
    ""baseUrl"": ""./src"",
    ""incremental"": true,
    ""skipLibCheck"": true,
    ""strictNullChecks"": true,
    ""noImplicitAny"": true,
    ""strictBindCallApply"": true,
    ""forceConsistentCasingInFileNames"": true,
    ""noFallthroughCasesInSwitch"": true
  }
}
";
    }

    private static string GenerateTsConfigBuild()
    {
        return @"{
  ""extends"": ""./tsconfig.json"",
  ""exclude"": [""node_modules"", ""test"", ""dist"", ""**/*spec.ts""]
}
";
    }

    private static string GenerateNestCli()
    {
        return @"{
  ""$schema"": ""https://json.schemastore.org/nest-cli"",
  ""collection"": ""@nestjs/schematics"",
  ""sourceRoot"": ""src"",
  ""compilerOptions"": {
    ""deleteOutDir"": true
  }
}
";
    }

    private static string GenerateGitignore()
    {
        return @"# Dependencies
node_modules/

# Build
dist/

# IDE
.idea/
.vscode/
*.swp

# Environment
.env
.env.local
.env.*.local

# Logs
*.log
npm-debug.log*

# OS
.DS_Store

# Coverage
coverage/
";
    }

    private static string GenerateReadme(string projectName, ProjectRequirements requirements)
    {
        return $@"# {projectName}

{requirements.Description ?? "A NestJS REST API application."}

## Getting Started

### Prerequisites

- Node.js 20+
- npm

### Installation

```bash
npm install
```

### Running the Application

```bash
# Development
npm run start:dev

# Production
npm run build
npm run start:prod
```

The API will be available at `http://localhost:3000`.

### Testing

```bash
# Unit tests
npm run test

# E2E tests
npm run test:e2e

# Coverage
npm run test:cov
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
│   ├── items/
│   │   ├── dto/
│   │   │   └── create-item.dto.ts
│   │   ├── item.entity.ts
│   │   ├── items.controller.ts
│   │   ├── items.module.ts
│   │   └── items.service.ts
│   ├── app.controller.ts
│   ├── app.module.ts
│   ├── app.service.ts
│   └── main.ts
├── test/
│   ├── app.e2e-spec.ts
│   └── jest-e2e.json
├── nest-cli.json
├── package.json
├── tsconfig.json
└── README.md
```

## License

MIT
";
    }

    private static string GenerateAppControllerSpec()
    {
        return @"import { Test, TestingModule } from '@nestjs/testing';
import { AppController } from './app.controller';
import { AppService } from './app.service';

describe('AppController', () => {
  let appController: AppController;

  beforeEach(async () => {
    const app: TestingModule = await Test.createTestingModule({
      controllers: [AppController],
      providers: [AppService],
    }).compile();

    appController = app.get<AppController>(AppController);
  });

  describe('root', () => {
    it('should return welcome message', () => {
      const result = appController.getRoot();
      expect(result).toHaveProperty('message');
      expect(result).toHaveProperty('version');
    });
  });

  describe('health', () => {
    it('should return healthy status', () => {
      const result = appController.getHealth();
      expect(result).toEqual({ status: 'healthy' });
    });
  });
});
";
    }

    private static string GenerateItemsControllerSpec()
    {
        return @"import { Test, TestingModule } from '@nestjs/testing';
import { ItemsController } from './items.controller';
import { ItemsService } from './items.service';

describe('ItemsController', () => {
  let controller: ItemsController;
  let service: ItemsService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      controllers: [ItemsController],
      providers: [ItemsService],
    }).compile();

    controller = module.get<ItemsController>(ItemsController);
    service = module.get<ItemsService>(ItemsService);
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });

  describe('findAll', () => {
    it('should return an array of items', () => {
      const result = controller.findAll();
      expect(Array.isArray(result)).toBe(true);
    });
  });

  describe('create', () => {
    it('should create and return an item', () => {
      const dto = { name: 'Test Item', price: 9.99 };
      const result = controller.create(dto);
      expect(result).toHaveProperty('id');
      expect(result.name).toBe(dto.name);
      expect(result.price).toBe(dto.price);
    });
  });
});
";
    }

    private static string GenerateJestConfig()
    {
        return @"module.exports = {
  moduleFileExtensions: ['js', 'json', 'ts'],
  rootDir: 'src',
  testRegex: '.*\\.spec\\.ts$',
  transform: {
    '^.+\\.(t|j)s$': 'ts-jest',
  },
  collectCoverageFrom: ['**/*.(t|j)s'],
  coverageDirectory: '../coverage',
  testEnvironment: 'node',
};
";
    }

    private static string GenerateE2ESpec(string projectName)
    {
        return $@"import {{ Test, TestingModule }} from '@nestjs/testing';
import {{ INestApplication, ValidationPipe }} from '@nestjs/common';
import * as request from 'supertest';
import {{ AppModule }} from './../src/app.module';

describe('AppController (e2e)', () => {{
  let app: INestApplication;

  beforeEach(async () => {{
    const moduleFixture: TestingModule = await Test.createTestingModule({{
      imports: [AppModule],
    }}).compile();

    app = moduleFixture.createNestApplication();
    app.useGlobalPipes(new ValidationPipe({{
      whitelist: true,
      transform: true,
    }}));
    await app.init();
  }});

  afterEach(async () => {{
    await app.close();
  }});

  it('/ (GET)', () => {{
    return request(app.getHttpServer())
      .get('/')
      .expect(200)
      .expect((res) => {{
        expect(res.body.message).toContain('{projectName}');
      }});
  }});

  it('/health (GET)', () => {{
    return request(app.getHttpServer())
      .get('/health')
      .expect(200)
      .expect({{ status: 'healthy' }});
  }});

  describe('/items', () => {{
    it('POST should create an item', () => {{
      return request(app.getHttpServer())
        .post('/items')
        .send({{ name: 'Test', price: 10 }})
        .expect(201)
        .expect((res) => {{
          expect(res.body.name).toBe('Test');
          expect(res.body.price).toBe(10);
          expect(res.body.id).toBeDefined();
        }});
    }});

    it('GET should return items', () => {{
      return request(app.getHttpServer())
        .get('/items')
        .expect(200)
        .expect((res) => {{
          expect(Array.isArray(res.body)).toBe(true);
        }});
    }});
  }});
}});
";
    }

    private static string GenerateJestE2EConfig()
    {
        return @"{
  ""moduleFileExtensions"": [""js"", ""json"", ""ts""],
  ""rootDir"": ""."",
  ""testEnvironment"": ""node"",
  ""testRegex"": "".e2e-spec.ts$"",
  ""transform"": {
    ""^.+\\.(t|j)s$"": ""ts-jest""
  }
}
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

FROM node:20-alpine

WORKDIR /app
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/node_modules ./node_modules
COPY package*.json ./

RUN adduser -D appuser
USER appuser

EXPOSE 3000
CMD [""node"", ""dist/main""]
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
      - ""3000:3000""
    environment:
      - NODE_ENV=production
      - PORT=3000
";
    }
}
