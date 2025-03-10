using Elsa.Alterations.Core.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Cancels the workflow instances in an alteration plan.
/// </summary>
[UsedImplicitly]
public class Cancel : AlterationBase;