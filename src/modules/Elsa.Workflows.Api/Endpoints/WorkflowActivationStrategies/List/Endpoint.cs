using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Extensions;
using Elsa.Models;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.WorkflowActivationStrategies.List;

/// <summary>
/// Returns list of available <see cref="IWorkflowActivationStrategy" /> implementations.
/// </summary>
internal class List(IEnumerable<IWorkflowActivationStrategy> strategies) : ElsaEndpointWithoutRequest<ListResponse<WorkflowActivationStrategyDescriptor>>
{
    public override void Configure()
    {
        Get("/descriptors/workflow-activation-strategies");
        ConfigurePermissions("read:workflow-activation-strategies");
    }

    public override Task<ListResponse<WorkflowActivationStrategyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = strategies.Select(WorkflowActivationStrategyDescriptor.FromStrategy).OrderBy(x => x.DisplayName).ToList();
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