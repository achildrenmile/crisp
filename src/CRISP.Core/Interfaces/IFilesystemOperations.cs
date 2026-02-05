namespace CRISP.Core.Interfaces;

/// <summary>
/// Filesystem operations for workspace management.
/// </summary>
public interface IFilesystemOperations
{
    /// <summary>
    /// Creates a temporary workspace directory.
    /// </summary>
    /// <param name="prefix">Directory name prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the created workspace.</returns>
    Task<string> CreateWorkspaceAsync(
        string prefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes content to a file.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="content">File content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes binary content to a file.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="content">Binary content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteBinaryFileAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads content from a file.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content.</returns>
    Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file or directory exists.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if the path exists.</returns>
    bool Exists(string path);

    /// <summary>
    /// Deletes a file or directory.
    /// </summary>
    /// <param name="path">Path to delete.</param>
    /// <param name="recursive">Whether to delete recursively (for directories).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string path,
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a directory.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="pattern">Search pattern.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file paths.</returns>
    Task<IReadOnlyList<string>> ListFilesAsync(
        string path,
        string pattern = "*",
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file or directory.
    /// </summary>
    /// <param name="source">Source path.</param>
    /// <param name="destination">Destination path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CopyAsync(
        string source,
        string destination,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up a workspace directory.
    /// </summary>
    /// <param name="workspacePath">Workspace path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupWorkspaceAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);
}
