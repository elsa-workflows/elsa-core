using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sinks.Features;

/// <summary>
/// The workflow sinks feature enables applications to capture workflow state asynchronously.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
public class WorkflowSinksFeature : FeatureBase
{
    /// <inheritdoc />
    public WorkflowSinksFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Resolves an instance of a concrete implementation of <see cref="IWorkflowSinkDispatcher"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowSinkDispatcher> WorkflowSinkDispatcher { get; set; } = sp => ActivatorUtilities.CreateInstance<BackgroundWorkflowSinkDispatcher>(sp);

    /// <inheritdoc />
    public override void Configure()
    {
        
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<IWorkflowSinkInvoker, DefaultWorkflowSinkInvoker>()
            .AddSingleton(WorkflowSinkDispatcher)
            .AddHandlersFrom<WorkflowSinksFeature>();
    }
}