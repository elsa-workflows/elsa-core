using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseEntityFrameworkCore(this WorkflowRuntimeFeature feature, Action<EFCoreWorkflowRuntimePersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}