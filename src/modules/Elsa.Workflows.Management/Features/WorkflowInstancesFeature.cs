using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Enables storage of workflow instances.
/// </summary>
public class WorkflowInstancesFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowInstancesFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowInstanceStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(WorkflowInstanceStore);
    }
}