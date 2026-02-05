namespace CRISP.Core.Enums;

/// <summary>
/// Supported source control management platforms.
/// </summary>
public enum ScmPlatform
{
    /// <summary>
    /// GitHub - the primary and default platform.
    /// </summary>
    GitHub = 0,

    /// <summary>
    /// Azure DevOps Server (on-premises) - alternative platform.
    /// </summary>
    AzureDevOps = 1
}
