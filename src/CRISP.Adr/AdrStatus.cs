namespace CRISP.Adr;

/// <summary>
/// Represents the lifecycle status of an Architecture Decision Record.
/// </summary>
public enum AdrStatus
{
    /// <summary>Decision is proposed but not yet accepted.</summary>
    Proposed,

    /// <summary>Decision has been accepted and is in effect.</summary>
    Accepted,

    /// <summary>Decision is no longer relevant or recommended.</summary>
    Deprecated,

    /// <summary>Decision has been replaced by a newer decision.</summary>
    Superseded
}
