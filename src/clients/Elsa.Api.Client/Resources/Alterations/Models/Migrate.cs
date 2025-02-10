namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// Migrates a workflow instance to a newer version in an alteration.
/// </summary>
public class Migrate : AlterationBase
{
    /// <summary>
    /// The target version to upgrade to.
    /// </summary>
    public int TargetVersion { get; set; }
}