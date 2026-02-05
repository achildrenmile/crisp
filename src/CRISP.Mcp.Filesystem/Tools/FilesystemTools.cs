using System.ComponentModel;
using CRISP.Core.Interfaces;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.Filesystem.Tools;

/// <summary>
/// MCP tools for filesystem operations.
/// </summary>
[McpServerToolType]
public sealed class FilesystemTools
{
    private readonly IFilesystemOperations _filesystem;

    public FilesystemTools(IFilesystemOperations filesystem)
    {
        _filesystem = filesystem;
    }

    [McpServerTool("create_workspace")]
    [Description("Creates a temporary workspace directory for scaffolding")]
    public async Task<WorkspaceResult> CreateWorkspaceAsync(
        [Description("Prefix for the workspace name")] string prefix)
    {
        var path = await _filesystem.CreateWorkspaceAsync(prefix);
        return new WorkspaceResult { Success = true, Path = path };
    }

    [McpServerTool("create_directory")]
    [Description("Creates a directory at the specified path")]
    public async Task<OperationResult> CreateDirectoryAsync(
        [Description("Directory path to create")] string path)
    {
        await _filesystem.CreateDirectoryAsync(path);
        return new OperationResult { Success = true, Message = $"Directory created: {path}" };
    }

    [McpServerTool("write_file")]
    [Description("Writes content to a file")]
    public async Task<OperationResult> WriteFileAsync(
        [Description("File path")] string path,
        [Description("File content")] string content)
    {
        await _filesystem.WriteFileAsync(path, content);
        return new OperationResult { Success = true, Message = $"File written: {path}" };
    }

    [McpServerTool("read_file")]
    [Description("Reads content from a file")]
    public async Task<FileContentResult> ReadFileAsync(
        [Description("File path")] string path)
    {
        var content = await _filesystem.ReadFileAsync(path);
        return new FileContentResult { Success = true, Path = path, Content = content };
    }

    [McpServerTool("list_files")]
    [Description("Lists files in a directory")]
    public async Task<FileListResult> ListFilesAsync(
        [Description("Directory path")] string path,
        [Description("Search pattern (default: *)")] string pattern = "*",
        [Description("Search recursively")] bool recursive = false)
    {
        var files = await _filesystem.ListFilesAsync(path, pattern, recursive);
        return new FileListResult { Success = true, Path = path, Files = files };
    }

    [McpServerTool("delete")]
    [Description("Deletes a file or directory")]
    public async Task<OperationResult> DeleteAsync(
        [Description("Path to delete")] string path,
        [Description("Delete recursively")] bool recursive = false)
    {
        await _filesystem.DeleteAsync(path, recursive);
        return new OperationResult { Success = true, Message = $"Deleted: {path}" };
    }

    [McpServerTool("exists")]
    [Description("Checks if a file or directory exists")]
    public ExistsResult Exists(
        [Description("Path to check")] string path)
    {
        var exists = _filesystem.Exists(path);
        return new ExistsResult { Path = path, Exists = exists };
    }

    [McpServerTool("cleanup_workspace")]
    [Description("Cleans up a workspace directory")]
    public async Task<OperationResult> CleanupWorkspaceAsync(
        [Description("Workspace path")] string workspacePath)
    {
        await _filesystem.CleanupWorkspaceAsync(workspacePath);
        return new OperationResult { Success = true, Message = $"Workspace cleaned up: {workspacePath}" };
    }
}

public sealed record WorkspaceResult
{
    public required bool Success { get; init; }
    public required string Path { get; init; }
}

public sealed record OperationResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
}

public sealed record FileContentResult
{
    public required bool Success { get; init; }
    public required string Path { get; init; }
    public required string Content { get; init; }
}

public sealed record FileListResult
{
    public required bool Success { get; init; }
    public required string Path { get; init; }
    public required IReadOnlyList<string> Files { get; init; }
}

public sealed record ExistsResult
{
    public required string Path { get; init; }
    public required bool Exists { get; init; }
}
