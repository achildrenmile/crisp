namespace CRISP.Core.Enums;

/// <summary>
/// Repository visibility options.
/// </summary>
public enum RepositoryVisibility
{
    /// <summary>
    /// Private repository - only accessible to authorized users.
    /// </summary>
    Private = 0,

    /// <summary>
    /// Public repository - accessible to everyone.
    /// </summary>
    Public = 1,

    /// <summary>
    /// Internal repository - accessible to organization members (GitHub Enterprise / Azure DevOps).
    /// </summary>
    Internal = 2
}
