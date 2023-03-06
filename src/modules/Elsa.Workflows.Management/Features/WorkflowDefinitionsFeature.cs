using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Configures workflow definition storage.
/// </summary>
public class WorkflowDefinitionsFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowDefinitionsFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowDefinitionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(WorkflowDefinitionStore);
    }
}