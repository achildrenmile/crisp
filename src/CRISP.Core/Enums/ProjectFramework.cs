namespace CRISP.Core.Enums;

/// <summary>
/// Supported project frameworks.
/// </summary>
public enum ProjectFramework
{
    // .NET
    AspNetCoreWebApi,
    AspNetCoreMvc,
    AspNetCoreMinimalApi,
    BlazorServer,
    BlazorWebAssembly,
    ConsoleApp,
    WorkerService,

    // JavaScript/TypeScript
    NextJs,
    React,
    Vue,
    Express,
    NestJs,

    // Python
    FastApi,
    Flask,
    Django,

    // Java
    SpringBoot,
    Quarkus,

    // Go
    GinGonic,
    Echo,

    // Rust
    Actix,
    Axum,

    // C++
    CppCMake,
    CppConan
}
