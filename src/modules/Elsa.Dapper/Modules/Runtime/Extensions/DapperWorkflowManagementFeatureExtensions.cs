using Elsa.Dapper.Modules.Runtime.Features;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
[PublicAPI]
public static class DapperWorkflowRuntimeExtensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="DapperWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseDapper(this WorkflowRuntimeFeature feature, Action<DapperWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}