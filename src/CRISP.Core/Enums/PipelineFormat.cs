namespace CRISP.Core.Enums;

/// <summary>
/// Pipeline definition format for Azure DevOps Server.
/// </summary>
public enum PipelineFormat
{
    /// <summary>
    /// YAML pipeline format (Azure DevOps Server 2019+).
    /// </summary>
    Yaml = 0,

    /// <summary>
    /// XAML build definition (legacy servers pre-2019).
    /// </summary>
    Xaml = 1
}
