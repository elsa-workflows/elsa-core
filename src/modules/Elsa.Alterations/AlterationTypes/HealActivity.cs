using Elsa.Alterations.Core.Abstractions;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Heals an activity from the Faulted state.
/// </summary>
[UsedImplicitly]
public class HealActivity : AlterationBase
{
    /// <summary>
    /// The handle to the to be healed.
    /// </summary>
    public ActivityHandle ActivityHandle { get; set; } = null!;
}