using System.Text;
using CRISP.Adr;
using CRISP.Agent;
using CRISP.Api.Auth;
using CRISP.Api.Endpoints;
using CRISP.Api.Services;
using CRISP.Audit;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Git;
using CRISP.Mcp.AzureDevOps;
using CRISP.Mcp.GitHub;
using CRISP.Mcp.PolicyEngine;
using CRISP.Pipelines;
using CRISP.Templates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/crisp-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CRISP API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Configure settings
    builder.Services.Configure<CrispConfiguration>(
        builder.Configuration.GetSection("Crisp"));
    builder.Services.Configure<ClaudeApiOptions>(
        builder.Configuration.GetSection("Claude"));
    builder.Services.Configure<OpenAiOptions>(
        builder.Configuration.GetSection("OpenAI"));
    builder.Services.Configure<LlmConfiguration>(
        builder.Configuration.GetSection("Llm"));
    builder.Services.Configure<AuthConfiguration>(
        builder.Configuration.GetSection("Auth"));

    // Authentication services
    var authConfig = builder.Configuration.GetSection("Auth").Get<AuthConfiguration>()
        ?? new AuthConfiguration();

    builder.Services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

    // Configure authentication
    if (authConfig.Enabled)
    {
        var oidcEnabled = authConfig.Oidc?.Enabled ?? false;

        if (oidcEnabled && authConfig.Oidc != null)
        {
            // OIDC authentication with cookie-based sessions
            Log.Information("Configuring OIDC authentication with authority: {Authority}", authConfig.Oidc.Authority);

            var sameSiteMode = authConfig.Oidc.Cookie.SameSite.ToLowerInvariant() switch
            {
                "strict" => SameSiteMode.Strict,
                "lax" => SameSiteMode.Lax,
                "none" => SameSiteMode.None,
                _ => SameSiteMode.Lax
            };

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = authConfig.Oidc.Cookie.Name;
                options.Cookie.SameSite = sameSiteMode;
                options.Cookie.SecurePolicy = authConfig.Oidc.Cookie.SecurePolicy
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(authConfig.Oidc.Cookie.ExpirationMinutes);
                options.SlidingExpiration = true;
                options.Events = OidcEvents.CreateCookieEvents();
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authConfig.Oidc.Authority;
                options.ClientId = authConfig.Oidc.ClientId;
                options.ClientSecret = authConfig.Oidc.ClientSecret;
                options.ResponseType = authConfig.Oidc.ResponseType;
                options.CallbackPath = authConfig.Oidc.CallbackPath;
                options.SignedOutCallbackPath = authConfig.Oidc.SignedOutCallbackPath;
                options.SaveTokens = authConfig.Oidc.SaveTokens;
                options.GetClaimsFromUserInfoEndpoint = authConfig.Oidc.GetClaimsFromUserInfoEndpoint;

                options.Scope.Clear();
                foreach (var scope in authConfig.Oidc.Scopes)
                {
                    options.Scope.Add(scope);
                }

                options.Events = OidcEvents.CreateOidcEvents();
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Also support JWT Bearer for API calls
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authConfig.Jwt.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = authConfig.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authConfig.Jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            })
            .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(
                ApiKeyAuthOptions.DefaultScheme, _ => { });

            // Add policy selector to handle multiple auth schemes
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        JwtBearerDefaults.AuthenticationScheme,
                        ApiKeyAuthOptions.DefaultScheme)
                    .Build();
            });
        }
        else
        {
            // JWT-only authentication (default)
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authConfig.Jwt.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = authConfig.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authConfig.Jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            })
            .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(
                ApiKeyAuthOptions.DefaultScheme, _ => { });

            builder.Services.AddAuthorization();
        }
    }

    // Add HTTP client factory
    builder.Services.AddHttpClient("AzureDevOps");

    // Session management with persistence
    var sessionsPath = builder.Configuration.GetValue<string>("Crisp:SessionsPath")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".crisp", "sessions");
    builder.Services.AddSingleton<ISessionManager>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<PersistentSessionManager>>();
        return new PersistentSessionManager(logger, sessionsPath);
    });

    // LLM client - register based on configuration
    var llmConfig = builder.Configuration.GetSection("Llm").Get<LlmConfiguration>()
        ?? new LlmConfiguration();

    if (llmConfig.Provider == LlmProvider.OpenAI)
    {
        Log.Information("Using OpenAI as LLM provider");
        builder.Services.AddSingleton<ILlmClient, OpenAiClient>();
    }
    else
    {
        Log.Information("Using Claude as LLM provider");
        builder.Services.AddSingleton<ILlmClient, ClaudeClient>();
    }

    // Chat agent
    builder.Services.AddScoped<IChatAgent, ChatAgent>();

    // Register core CRISP services
    builder.Services.AddCrispAudit();
    builder.Services.AddCrispGit();
    builder.Services.AddCrispTemplates();
    builder.Services.AddCrispPipelines();
    builder.Services.AddCrispAdr(builder.Configuration);

    // Register source control provider based on configuration
    builder.Services.AddSingleton<ISourceControlProvider>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<CrispConfiguration>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        if (config.Value.ScmPlatform == ScmPlatform.GitHub)
        {
            var logger = loggerFactory.CreateLogger<GitHubSourceControlProvider>();
            return new GitHubSourceControlProvider(logger, config);
        }
        else
        {
            var logger = loggerFactory.CreateLogger<AzureDevOpsSourceControlProvider>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AzureDevOpsSourceControlProvider(logger, config, httpClientFactory);
        }
    });

    // Register policy engine
    builder.Services.AddSingleton<IPolicyEngine, PolicyEngineService>();

    // Register main CRISP agent
    builder.Services.AddScoped<ICrispAgent, CrispAgent>();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "CRISP API",
            Version = "v1",
            Description = "Code Repo Initialization & Scaffolding Platform API"
        });
    });

    // CORS for development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure middleware
    app.UseCors();
    app.UseSerilogRequestLogging();

    // Authentication & Authorization (if enabled)
    var authEnabled = app.Configuration.GetValue<bool>("Auth:Enabled", true);
    if (authEnabled)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Serve static files (React frontend)
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    if (Directory.Exists(wwwrootPath))
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRISP API v1");
        c.RoutePrefix = "swagger";
    });

    // Map endpoints
    app.MapAuthEndpoints();
    app.MapChatEndpoints();
    app.MapHealthEndpoints();

    // SPA fallback - serve index.html for non-API routes
    if (Directory.Exists(wwwrootPath))
    {
        app.MapFallbackToFile("index.html");
    }
    else
    {
        // Root redirect to Swagger when no frontend
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    Log.Information("CRISP API started. Open http://localhost:5000/swagger for API documentation.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CRISP API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
