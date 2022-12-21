using System.ComponentModel;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstantiationStrategies.List;

/// <summary>
/// Returns list of available <see cref="IWorkflowInstantiationStrategy" /> implementations.
/// </summary>
internal class List : ElsaEndpointWithoutRequest<ListResponse<WorkflowInstantiationStrategyDescriptor>>
{
    private readonly IEnumerable<IWorkflowInstantiationStrategy> _strategies;

    public List(IEnumerable<IWorkflowInstantiationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public override void Configure()
    {
        Get("/descriptors/workflow-instantiation-strategies");
        ConfigurePermissions("read:workflow-instantiation-strategies");
    }

    public override Task<ListResponse<WorkflowInstantiationStrategyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _strategies.Select(WorkflowInstantiationStrategyDescriptor.FromStrategy).OrderBy(x => x.DisplayName).ToList();
        var response =new ListResponse<WorkflowInstantiationStrategyDescriptor>(descriptors);
        return Task.FromResult(response);
    }
}

internal record WorkflowInstantiationStrategyDescriptor(string DisplayName, string TypeName)
{
    public static WorkflowInstantiationStrategyDescriptor FromStrategy(IWorkflowInstantiationStrategy strategy)
    {
        var type = strategy.GetType();
        var displayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttribute?.DisplayName ?? type.Name.Replace("Strategy", "").Humanize();

        return new WorkflowInstantiationStrategyDescriptor(displayName, type.GetSimpleAssemblyQualifiedName());
    }
}