using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Workflows.Core.Contracts;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.WorkflowActivationStrategies.List;

/// <summary>
/// Returns list of available <see cref="IWorkflowActivationStrategy" /> implementations.
/// </summary>
internal class List : ElsaEndpointWithoutRequest<ListResponse<WorkflowActivationStrategyDescriptor>>
{
    private readonly IEnumerable<IWorkflowActivationStrategy> _strategies;

    public List(IEnumerable<IWorkflowActivationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public override void Configure()
    {
        Get("/descriptors/workflow-activation-strategies");
        ConfigurePermissions("read:workflow-activation-strategies");
    }

    public override Task<ListResponse<WorkflowActivationStrategyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _strategies.Select(WorkflowActivationStrategyDescriptor.FromStrategy).OrderBy(x => x.DisplayName).ToList();
        var response =new ListResponse<WorkflowActivationStrategyDescriptor>(descriptors);
        return Task.FromResult(response);
    }
}

internal record WorkflowActivationStrategyDescriptor(string DisplayName, string Description, string TypeName)
{
    public static WorkflowActivationStrategyDescriptor FromStrategy(IWorkflowActivationStrategy strategy)
    {
        var type = strategy.GetType();
        var displayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
        var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>();
        var displayAttribute = type.GetCustomAttribute<DisplayAttribute>();
        var displayName = displayNameAttribute?.DisplayName ?? displayAttribute?.Name ?? type.Name.Replace("Strategy", "").Humanize();
        var description = descriptionAttribute?.Description ?? displayAttribute?.Description ?? "";

        return new WorkflowActivationStrategyDescriptor(displayName, description, type.GetSimpleAssemblyQualifiedName());
    }
}