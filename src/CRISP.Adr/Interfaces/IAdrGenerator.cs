namespace CRISP.Adr.Interfaces;

/// <summary>
/// Generates Architecture Decision Record files from collected decisions.
/// </summary>
public interface IAdrGenerator
{
    /// <summary>
    /// Generates all ADR files from collected decisions and writes them to the workspace.
    /// Called at the end of the scaffolding process, after all decisions have been collected.
    /// </summary>
    /// <param name="decisions">All decisions collected during scaffolding.</param>
    /// <param name="workspacePath">Root path of the scaffolded repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file paths (relative to repo root) that were created.</returns>
    Task<IReadOnlyList<string>> GenerateAsync(
        IReadOnlyList<AdrDecision> decisions,
        string workspacePath,
        CancellationToken cancellationToken = default);
}
