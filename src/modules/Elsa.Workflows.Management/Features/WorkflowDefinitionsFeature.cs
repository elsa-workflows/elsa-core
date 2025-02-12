using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Handlers.Commands;
using Elsa.Workflows.Management.Handlers.Requests;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Configures workflow definition storage.
/// </summary>
public class WorkflowDefinitionsFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// The factory to create new instances of <see cref="IWorkflowDefinitionStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowDefinitionStore>();
    public Type FindWorkflowDefinitionHandler { get; set; } = typeof(FindWorkflowDefinitionHandler);
    public Type FindLastVersionOfWorkflowDefinitionHandler { get; set; } = typeof(FindLastVersionOfWorkflowDefinitionHandler);
    public Type FindLatestOrPublishedWorkflowDefinitionsHandler { get; set; } = typeof(FindLatestOrPublishedWorkflowDefinitionsHandler);
    public Type SaveWorkflowDefinitionHandler { get; set; } = typeof(SaveWorkflowDefinitionHandler);

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddScoped(WorkflowDefinitionStore)
            .AddScoped(typeof(IRequestHandler), FindWorkflowDefinitionHandler)
            .AddScoped(typeof(IRequestHandler), FindLastVersionOfWorkflowDefinitionHandler)
            .AddScoped(typeof(IRequestHandler), FindLatestOrPublishedWorkflowDefinitionsHandler)
            .AddScoped(typeof(ICommandHandler), SaveWorkflowDefinitionHandler)
            ;
    }
}