using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows;
using Humanizer;

namespace Elsa.Workflows.Api.Endpoints.IncidentStrategies.List;

/// <summary>
/// Returns list of available <see cref="IIncidentStrategy" /> implementations.
/// </summary>
internal class List(IEnumerable<IIncidentStrategy> strategies, IWorkflowJsonTypeRegistry workflowJsonTypeRegistry) : ElsaEndpointWithoutRequest<ListResponse<IncidentStrategyDescriptor>>
{
    public override void Configure()
    {
        Get("/descriptors/incident-strategies");
        ConfigurePermissions("read:incident-strategies");
    }

    public override Task<ListResponse<IncidentStrategyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = strategies.Select(x => IncidentStrategyDescriptor.FromStrategy(x, workflowJsonTypeRegistry)).OrderBy(x => x.DisplayName).ToList();
        var response = new ListResponse<IncidentStrategyDescriptor>(descriptors);
        return Task.FromResult(response);
    }
}

internal record IncidentStrategyDescriptor(string DisplayName, string Description, string TypeName)
{
    public static IncidentStrategyDescriptor FromStrategy(IIncidentStrategy strategy, IWorkflowJsonTypeRegistry workflowJsonTypeRegistry)
    {
        var type = strategy.GetType();
        var displayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
        var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>();
        var displayAttribute = type.GetCustomAttribute<DisplayAttribute>();
        var displayName = displayNameAttribute?.DisplayName ?? displayAttribute?.Name ?? type.Name.Replace("Strategy", "").Humanize();
        var description = descriptionAttribute?.Description ?? displayAttribute?.Description ?? "";

        var typeName = workflowJsonTypeRegistry.TryGetAlias(type, out var alias) ? alias : type.FullName!;
        return new IncidentStrategyDescriptor(displayName, description, typeName);
    }
}
