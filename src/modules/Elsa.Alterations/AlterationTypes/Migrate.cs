using Elsa.Alterations.Core.Abstractions;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Migrates a workflow instance to a newer version.
/// </summary>
public class Migrate : AlterationBase
{
    /// <summary>
    /// The target version to upgrade to.
    /// </summary>
    public int TargetVersion { get; set; }
}