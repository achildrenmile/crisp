using CRISP.Core.Configuration;
using CRISP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Templates;

/// <summary>
/// Implementation of filesystem operations.
/// </summary>
public sealed class FilesystemOperations : IFilesystemOperations
{
    private readonly ILogger<FilesystemOperations> _logger;
    private readonly CrispConfiguration _config;

    public FilesystemOperations(
        ILogger<FilesystemOperations> logger,
        IOptions<CrispConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public Task<string> CreateWorkspaceAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var workspaceName = $"{prefix}-{Guid.NewGuid():N}";
        var workspacePath = Path.Combine(_config.Common.WorkspaceDirectory, workspaceName);

        _logger.LogInformation("Creating workspace at {Path}", workspacePath);
        Directory.CreateDirectory(workspacePath);

        return Task.FromResult(workspacePath);
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogDebug("Creating directory: {Path}", path);
            Directory.CreateDirectory(path);
        }

        return Task.CompletedTask;
    }

    public async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogDebug("Writing file: {Path}", path);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    public async Task WriteBinaryFileAsync(string path, byte[] content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogDebug("Writing binary file: {Path}", path);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public async Task<string> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading file: {Path}", path);
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    public Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting: {Path} (recursive: {Recursive})", path, recursive);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(
        string path,
        string pattern = "*",
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, pattern, searchOption);

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public Task CopyAsync(
        string source,
        string destination,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Copying {Source} to {Destination}", source, destination);

        if (File.Exists(source))
        {
            var destDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(source, destination, overwrite);
        }
        else if (Directory.Exists(source))
        {
            CopyDirectory(source, destination, overwrite);
        }

        return Task.CompletedTask;
    }

    public Task CleanupWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up workspace: {Path}", workspacePath);

        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void CopyDirectory(string source, string destination, bool overwrite)
    {
        var dir = new DirectoryInfo(source);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            var targetPath = Path.Combine(destination, file.Name);
            file.CopyTo(targetPath, overwrite);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var targetPath = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, targetPath, overwrite);
        }
    }
}
