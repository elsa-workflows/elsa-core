using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Handlers.Request;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.Management.Stores;
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
    public Func<Type> FindWorkflowDefinitionHandler { get; set; } = () => typeof(FindWorkflowDefinitionHandler);

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddScoped(WorkflowDefinitionStore)
            .AddScoped(typeof(IRequestHandler), FindWorkflowDefinitionHandler())
            ;
    }
}