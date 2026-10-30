using System.Text.Json;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;
using JetBrains.Annotations;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSwitch activity based on its cases.
/// </summary>
[UsedImplicitly]
/// <summary>
/// Provides flow switch port services.
/// </summary>
public class FlowSwitchPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName is "Elsa.FlowSwitch";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var cases = context.Activity.GetProperty<List<SwitchCase>>(options, "cases") ?? new List<SwitchCase>();

        foreach (var @case in cases)
        {
            yield return new Port
            {
                Name = @case.Label,
                DisplayName = @case.Label,
                Type = PortType.Flow,
            };
        }

        if (cases.Any(x => x.Label == "Default"))
            yield break;

        yield return new Port
        {
            Name = "Default",
            DisplayName = "Default",
            Type = PortType.Flow
        };
    }
}