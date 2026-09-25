using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Providers;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the outcome ports an <c>Elsa.BpmnProcess</c> node offers on a flowchart.
/// </summary>
/// <remarks>
/// elsa-core's <c>BpmnProcess</c> now declares its <c>Done</c> and <c>Cancelled</c> outcomes with
/// <c>[FlowNode(BpmnInterpreter.DoneOutcomeName, BpmnInterpreter.CancelledOutcomeName)]</c>, so the descriptor the
/// server returns for <c>Elsa.BpmnProcess</c> already carries both flow ports. That made
/// <c>BpmnProcessPortProvider</c> -- the provider that used to synthesize the same two ports when the descriptor
/// carried none -- a no-op superset of the default provider, so it was deleted. This test pins that the default
/// port path alone, given a descriptor shaped like the one the server now sends, surfaces both ports and
/// synthesizes nothing in addition.
/// </remarks>
public class BpmnProcessPortsTests
{
    private readonly DefaultActivityPortService _portService = new([new DefaultActivityPortProvider()]);

    [Fact]
    public void GetPorts_YieldsBothOutcomesTheServerDescriptorNowDeclares()
    {
        var ports = _portService.GetPorts(CreateContext()).ToList();

        Assert.Equal(["Done", "Cancelled"], ports.Select(x => x.Name));
        Assert.Equal(["Done", "Cancelled"], ports.Select(x => x.DisplayName));
        Assert.All(ports, port => Assert.Equal(PortType.Flow, port.Type));
    }

    private static PortProviderContext CreateContext()
    {
        var declaredPorts = new[]
        {
            new Port { Name = BpmnProcessConstants.DoneOutcomeName, DisplayName = BpmnProcessConstants.DoneOutcomeName, Type = PortType.Flow },
            new Port { Name = "Cancelled", DisplayName = "Cancelled", Type = PortType.Flow }
        };

        var descriptor = new ActivityDescriptor
        {
            TypeName = BpmnProcessConstants.ActivityTypeName,
            Name = BpmnProcessConstants.ActivityTypeName,
            Version = 1,
            Ports = declaredPorts
        };

        var activity = new JsonObject
        {
            ["id"] = "activity-1",
            ["type"] = BpmnProcessConstants.ActivityTypeName,
            ["version"] = 1
        };

        return new(descriptor, activity);
    }
}
